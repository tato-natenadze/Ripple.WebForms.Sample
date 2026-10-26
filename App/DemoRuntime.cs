using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Hosting;
using Newtonsoft.Json.Linq;
using Ripple;

namespace Ripple.WebForms.Sample
{
    public sealed class WalletView
    {
        public string Address { get; set; }
        public string Seed { get; set; }
        public string Balance { get; set; }
        public string Reserve { get; set; } = "—";
        public string Available { get; set; } = "—";
        public string Status { get; set; }
        public uint? Ledger { get; set; }
    }

    public sealed class DemoView
    {
        public WalletView Sender { get; set; }
        public WalletView Receiver { get; set; }
        public string Message { get; set; }
        public string Monitoring { get; set; }
        public string EstimatedFee { get; set; }
        public bool Busy { get; set; }
        public bool Ready { get; set; }
        public TestnetServer Server { get; set; }
        public object[] Transfers { get; set; }
        public object[] Incoming { get; set; }
        public object[] AccountDeletes { get; set; }
    }

    public sealed partial class DemoRuntime
    {
        private static readonly Uri FaucetEndpoint = new Uri("https://faucet.altnet.rippletest.net/accounts");
        private readonly StateStore _store;
        private readonly HttpClient _http = new HttpClient();
        private XrplClient _client;
        private readonly CancellationTokenSource _shutdown = new CancellationTokenSource();
        private readonly SemaphoreSlim _actions = new SemaphoreSlim(1, 1);
        private readonly SemaphoreSlim _recovery = new SemaphoreSlim(1, 1);
        private readonly object _viewGate = new object();
        private readonly TaskCompletionSource<bool> _initialized = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly Dictionary<string, WalletView> _balances = new Dictionary<string, WalletView>(StringComparer.Ordinal);
        private string _message = "Create your sender and receiver wallets.";
        private string _monitoring = "Waiting for a receiver wallet.";
        private int _busy;
        private int _refreshBalances = 1;
        private string _estimatedFee = "—";
        private DateTime _nextFeeRefreshUtc = DateTime.MinValue;

        public DemoRuntime(string dataDirectory)
        {
            _store = new StateStore(dataDirectory);
            if (_store.Snapshot().Sender != null || _store.Snapshot().Receiver != null)
                _message = "Saved wallets and payment history loaded.";
            _client = CurrentServer.CreateClient();
            HostingEnvironment.QueueBackgroundWorkItem(async stopping =>
            {
                using (var linked = CancellationTokenSource.CreateLinkedTokenSource(stopping, _shutdown.Token))
                {
                    try
                    {
                        _store.Update(state => {
                            foreach (var transfer in state.Transfers.Where(x => x.Receipt == null && !x.FinishedWithoutSubmission))
                            {
                                transfer.FinishedWithoutSubmission = true;
                                transfer.Detail = "The application stopped before a signed receipt was saved. Nothing was submitted.";
                            }
                            foreach (var deletion in state.AccountDeletes.Where(x => x.Receipt == null && !x.FinishedWithoutSubmission))
                            {
                                deletion.FinishedWithoutSubmission = true;
                                deletion.Detail = "The application stopped before a signed receipt was saved. Nothing was submitted.";
                            }
                        });
                        // Register restored unresolved receipts before allowing another send.
                        for (;;)
                        {
                            try { await RecoverAsync(linked.Token).ConfigureAwait(false); break; }
                            catch (Exception ex) when (IsTemporary(ex, linked.Token))
                            {
                                SetMessage("Waiting for Testnet to check saved payment receipts.");
                                await Task.Delay(TimeSpan.FromSeconds(2), linked.Token).ConfigureAwait(false);
                            }
                        }
                        StartConnection();
                        _initialized.TrySetResult(true);
                        await Task.Delay(Timeout.Infinite, linked.Token).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (linked.IsCancellationRequested) { }
                    catch (Exception ex) { SetMessage(Describe(ex)); }
                    finally
                    {
                        _initialized.TrySetCanceled();
                        _shutdown.Cancel();
                        await _actions.WaitAsync().ConfigureAwait(false);
                        try
                        {
                            await StopConnectionAsync().ConfigureAwait(false);
                        }
                        finally
                        {
                            _client.Dispose();
                            _http.Dispose();
                            _store.Dispose();
                            _actions.Release();
                        }
                    }
                }
            });
        }

        public void Stop() { _shutdown.Cancel(); }

        public DemoView Snapshot()
        {
            var state = _store.Snapshot();
            lock (_viewGate)
            {
                return new DemoView
                {
                    Sender = View(state.Sender), Receiver = View(state.Receiver), Message = _message,
                    Monitoring = _monitoring, Busy = Volatile.Read(ref _busy) != 0,
                    EstimatedFee = _estimatedFee,
                    Ready = _initialized.Task.Status == TaskStatus.RanToCompletion,
                    Server = TestnetServer.Get(state.ServerId),
                    Transfers = state.Transfers.AsEnumerable().Reverse().Select(x => (object)new {
                        Amount = Xrp(x.AmountXrp), Status = x.FinishedWithoutSubmission ? "Not submitted" : x.Status.ToString(),
                        Hash = x.Receipt?.Hash, Fee = x.FeeXrp.HasValue ? Xrp(x.FeeXrp.Value) : null, Detail = x.Detail,
                        DestinationTag = x.DestinationTag, Memo = x.Memo, Destination = x.Destination, Error = x.Error }).ToArray(),
                    AccountDeletes = state.AccountDeletes.AsEnumerable().Reverse().Select(x => (object)new {
                        Sender = x.Sender, Destination = x.Receipt?.Destination, DestinationTag = x.Receipt?.DestinationTag,
                        Status = x.FinishedWithoutSubmission ? "Not submitted" : x.Status.ToString(),
                        Returned = x.ReturnedAmountXrp.HasValue ? Xrp(x.ReturnedAmountXrp.Value) : null,
                        Fee = x.FeeXrp.HasValue ? Xrp(x.FeeXrp.Value) : null,
                        Hash = x.Receipt?.Hash, Detail = x.Detail, Error = x.Error }).ToArray(),
                    Incoming = state.Incoming.AsEnumerable().Reverse().Select(x => (object)new {
                        Hash = x.Hash, Amount = Xrp(x.AmountXrp), Ledger = x.LedgerIndex }).ToArray()
                };
            }
        }

        private WalletView View(WalletState wallet)
        {
            if (wallet == null) return new WalletView { Status = "Not created", Balance = "—" };
            string seed = StateStore.ReadSeed(wallet);
            WalletView balance;
            if (_balances.TryGetValue(wallet.Address, out balance))
                return new WalletView { Address = wallet.Address, Seed = seed, Balance = balance.Balance, Reserve = balance.Reserve,
                    Available = balance.Available, Status = balance.Status, Ledger = balance.Ledger };
            return new WalletView { Address = wallet.Address, Seed = seed, Status = "Checking Testnet…", Balance = "—" };
        }

        public Task CreateWalletAsync(bool sender, CancellationToken token)
        {
            return RunActionAsync(async ct =>
            {
                var saved = _store.Snapshot();
                if ((sender ? saved.Sender : saved.Receiver) != null) return;
                uint ledger = await VerifyTestnetAsync(ct).ConfigureAwait(false);
                var wallet = StateStore.NewWallet();
                _store.Update(state => {
                    if (sender) state.Sender = wallet;
                    else { state.Receiver = wallet; state.FirstIncomingLedger = checked(ledger + 1); }
                });
                Interlocked.Exchange(ref _refreshBalances, 1);
                SetMessage(sender ? "Sender wallet created and saved." : "Receiver wallet created and saved.");
            }, token);
        }

        public Task FundSenderAsync(string operationId, CancellationToken token)
        {
            return RunActionAsync(async ct =>
            {
                RequireOperationId(operationId);
                var state = _store.Snapshot();
                if (state.Sender == null) throw new InvalidOperationException("Create the sender wallet first.");
                if (state.FaucetOperations.Contains(operationId)) return;
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                var before = await _client.GetAccountAsync(state.Sender.Address, ct).ConfigureAwait(false);
                // Persist the request identity first: resubmitting this HTTP form must not call the faucet twice.
                _store.Update(next => next.FaucetOperations.Add(operationId));
                SetMessage("Requesting Testnet XRP from the faucet…");
                using (var response = await _http.PostAsync(FaucetEndpoint,
                    new StringContent(new JObject { ["destination"] = state.Sender.Address }.ToString(), Encoding.UTF8, "application/json"), ct).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode)
                        throw new InvalidOperationException("The faucet rejected the request (HTTP " + (int)response.StatusCode + "). It may be rate limited.");
                }
                Interlocked.Exchange(ref _refreshBalances, 1);
                SetMessage("Faucet request accepted. Waiting for the validated balance.");
                using (var confirmation = CancellationTokenSource.CreateLinkedTokenSource(ct))
                {
                    confirmation.CancelAfter(TimeSpan.FromSeconds(60));
                    for (;;)
                    {
                        var account = await _client.GetAccountAsync(state.Sender.Address, confirmation.Token).ConfigureAwait(false);
                        StoreBalance(account);
                        if (account.Exists && (!before.Exists || account.BalanceXrp > before.BalanceXrp)) break;
                        await Task.Delay(TimeSpan.FromSeconds(2), confirmation.Token).ConfigureAwait(false);
                    }
                }
                SetMessage("Sender funding is validated on Testnet.");
            }, token);
        }

        public Task SendAsync(string operationId, string amountText, CancellationToken token)
        { return SendAsync(operationId, amountText, null, null, token); }

        public Task SendAsync(string operationId, string amountText, uint? tag, string memo, CancellationToken token)
        { return SendAsync(operationId, amountText, null, tag, memo, token); }

        public Task SendAsync(string operationId, string amountText, string destination, uint? tag, string memo, CancellationToken token)
        {
            return RunActionAsync(async ct =>
            {
                RequireOperationId(operationId);
                var state = _store.Snapshot();
                if (state.Transfers.Any(x => x.OperationId == operationId)) return;
                if (state.Sender == null) throw new InvalidOperationException("Create the sender wallet first.");
                string recipient = ResolveDestination(state, destination);
                decimal amount = ParseAmount(amountText);
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                await RecoverAsync(ct).ConfigureAwait(false);
                state = _store.Snapshot();
                if (state.Transfers.Any(IsUnresolved)) throw new InvalidOperationException("A previous payment is unresolved. Its saved receipt is being checked; wait before sending another.");
                _store.Update(next => next.Transfers.Add(new TransferState { OperationId = operationId, AmountXrp = amount,
                    Destination = recipient, DestinationTag = tag, Memo = memo, Detail = "Preparing payment." }));
                SetMessage("Preparing and submitting the payment once…");
                try
                {
                    var result = await _client.SendXrpAsync(StateStore.ReadSeed(state.Sender), new XrpPaymentRequest
                    {
                        Destination = recipient, AmountXrp = amount, DestinationTag = tag, Memo = memo,
                        OnPreparedAsync = (receipt, callbackToken) => {
                            callbackToken.ThrowIfCancellationRequested();
                            if (receipt.Sender != state.Sender.Address) throw new InvalidDataException("Receipt sender does not match the saved wallet.");
                            _store.Update(next => { var transfer = Find(next, operationId); transfer.Receipt = receipt; transfer.Detail = "Receipt saved before submission."; });
                            return Task.CompletedTask;
                        }
                    }, ct).ConfigureAwait(false);
                    ApplyResult(operationId, result.Transaction, result.Diagnostic, result.Error);
                    Interlocked.Exchange(ref _refreshBalances, 1);
                    SetMessage("Payment status: " + result.Status + ".");
                }
                catch (Exception ex)
                {
                    _store.Update(next => {
                        var transfer = Find(next, operationId);
                        transfer.FinishedWithoutSubmission = transfer.Receipt == null;
                        transfer.Detail = Describe(ex);
                    });
                    throw;
                }
            }, token);
        }

        private async Task RunActionAsync(Func<CancellationToken, Task> action, CancellationToken token)
        {
            if (!await _actions.WaitAsync(0, token).ConfigureAwait(false))
                throw new InvalidOperationException("Another operation is in progress. Wait for it to finish.");
            Interlocked.Exchange(ref _busy, 1);
            try
            {
                if (!_initialized.Task.IsCompleted)
                    throw new InvalidOperationException("Saved payment receipts are still being checked. Wait for the Testnet connection.");
                await _initialized.Task.ConfigureAwait(false);
                using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token, _shutdown.Token))
                {
                    deadline.CancelAfter(TimeSpan.FromSeconds(110));
                    await action(deadline.Token).ConfigureAwait(false);
                }
            }
            catch (Exception ex) { SetMessage(Describe(ex)); throw; }
            finally { Interlocked.Exchange(ref _busy, 0); _actions.Release(); }
        }

        private async Task MaintainAsync(CancellationToken token)
        {
            while (true)
            {
                token.ThrowIfCancellationRequested();
                bool refreshFee = DateTime.UtcNow >= _nextFeeRefreshUtc;
                try
                {
                    await RecoverAsync(token).ConfigureAwait(false);
                    if (Interlocked.Exchange(ref _refreshBalances, 0) != 0)
                    {
                        foreach (var wallet in new[] { _store.Snapshot().Sender, _store.Snapshot().Receiver })
                            if (wallet != null) StoreBalance(await _client.GetAccountAsync(wallet.Address, token).ConfigureAwait(false));
                        refreshFee = true;
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
                catch (Exception ex) when (IsTemporary(ex, token))
                { SetMessage(Describe(ex)); Interlocked.Exchange(ref _refreshBalances, 1); }
                catch (Exception ex) { SetMessage("Stopped checking saved payments and balances: " + Describe(ex)); return; }
                if (refreshFee) await RefreshFeeEstimateAsync(token).ConfigureAwait(false);
                await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
            }
        }

        private async Task RefreshFeeEstimateAsync(CancellationToken token)
        {
            _nextFeeRefreshUtc = DateTime.UtcNow.AddSeconds(30);
            try
            {
                decimal fee = await _client.GetFeeAsync(token).ConfigureAwait(false);
                lock (_viewGate) _estimatedFee = Xrp(fee);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { throw; }
            catch (Exception ex)
            {
                lock (_viewGate) _estimatedFee = "—";
                _nextFeeRefreshUtc = IsTemporary(ex, token) ? DateTime.UtcNow.AddSeconds(5) : DateTime.MaxValue;
                SetMessage("Fee estimate unavailable: " + Describe(ex));
            }
        }

        private async Task RecoverAsync(CancellationToken token)
        {
            await _recovery.WaitAsync(token).ConfigureAwait(false);
            try
            {
                foreach (var transfer in _store.Snapshot().Transfers.Where(IsUnresolved))
                {
                    var result = await _client.GetTransactionAsync(transfer.Receipt, token).ConfigureAwait(false);
                    ApplyResult(transfer.OperationId, result, null);
                    if (IsFinal(result.Status)) Interlocked.Exchange(ref _refreshBalances, 1);
                }
                await RecoverAccountDeletesAsync(token).ConfigureAwait(false);
            }
            finally { _recovery.Release(); }
        }

        private async Task ReceiveAsync(CancellationToken token)
        {
            try
            {
                SampleState state;
                for (;;)
                {
                    token.ThrowIfCancellationRequested();
                    state = _store.Snapshot();
                    if (state.Receiver == null) { await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false); continue; }
                    try
                    {
                        var account = await _client.GetAccountAsync(state.Receiver.Address, token).ConfigureAwait(false);
                        StoreBalance(account);
                        if (account.Exists) break;
                        SetMonitoring("Waiting for the receiver's first validated payment.");
                    }
                    catch (Exception ex) when (IsTemporary(ex, token)) { SetMonitoring("Waiting for the Testnet connection."); }
                    await Task.Delay(TimeSpan.FromSeconds(2), token).ConfigureAwait(false);
                }
                string resumeCheckpoint = state.Checkpoint;
                if (state.ServerId != "ripple") state.IncomingCheckpoints.TryGetValue(state.ServerId, out resumeCheckpoint);
                SetMonitoring("Watching confirmed incoming payments.");
                await _client.WatchIncomingPaymentsAsync(state.Receiver.Address, (payment, callbackToken) =>
                {
                    callbackToken.ThrowIfCancellationRequested();
                    _store.Update(next => {
                        if (next.Incoming.Any(x => string.Equals(x.Hash, payment.Hash, StringComparison.OrdinalIgnoreCase))) return;
                        next.Incoming.Add(new IncomingState { Hash = payment.Hash, AmountXrp = payment.DeliveredAmountXrp.Value,
                            LedgerIndex = payment.LedgerIndex.Value });
                    });
                    Interlocked.Exchange(ref _refreshBalances, 1);
                    return Task.CompletedTask;
                }, new PaymentWatchOptions {
                    Checkpoint = resumeCheckpoint,
                    FromLedger = resumeCheckpoint == null ? state.FirstIncomingLedger : null,
                    SaveCheckpointAsync = (checkpoint, callbackToken) => {
                        callbackToken.ThrowIfCancellationRequested();
                        _store.Update(next => {
                            if (state.ServerId == "ripple") next.Checkpoint = checkpoint;
                            else next.IncomingCheckpoints[state.ServerId] = checkpoint;
                        });
                        return Task.CompletedTask;
                    }
                }, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested) { }
            catch (Exception ex) { SetMonitoring("Stopped: " + Describe(ex)); }
        }

        private async Task<uint> VerifyTestnetAsync(CancellationToken token)
        {
            var network = await ReadTestnetNetworkInfoAsync(token).ConfigureAwait(false);
            return network.ValidatedLedgerIndex;
        }

        private void ApplyResult(string id, TransactionInfo transaction, string detail, PaymentError error = null)
        {
            _store.Update(state => {
                var transfer = Find(state, id);
                if (error != null) transfer.Error = PaymentErrorState.From(error);
                if (IsFinal(transfer.Status)) return;
                transfer.Status = transaction.Status;
                transfer.FeeXrp = transaction.FeeXrp;
                if (transaction.Found) { transfer.Destination = transaction.Destination; transfer.DestinationTag = transaction.DestinationTag; }
                transfer.Detail = detail ?? transaction.ResultCode ?? transaction.Status.ToString();
            });
        }

        private void StoreBalance(AccountInfo account)
        {
            lock (_viewGate)
            {
                WalletView previous;
                if (_balances.TryGetValue(account.Address, out previous) && previous.Ledger > account.LedgerIndex) return;
                _balances[account.Address] = new WalletView { Address = account.Address, Balance = Xrp(account.BalanceXrp),
                    Reserve = Xrp(account.EstimatedReserveXrp), Available = Xrp(account.EstimatedSpendableXrp),
                    Ledger = account.LedgerIndex, Status = account.Exists ? "Validated balance" : "Not funded" };
            }
        }

        private static TransferState Find(SampleState state, string id) { return state.Transfers.Single(x => x.OperationId == id); }
        private static bool IsFinal(TransactionStatus status) { return status == TransactionStatus.Succeeded || status == TransactionStatus.Failed || status == TransactionStatus.Expired || status == TransactionStatus.Rejected; }
        private static bool IsUnresolved(TransferState state) { return state.Receipt != null && !state.FinishedWithoutSubmission && !IsFinal(state.Status); }
        private static void RequireOperationId(string id) { Guid parsed; if (!Guid.TryParse(id, out parsed)) throw new ArgumentException("Invalid form operation. Reload the page."); }
        private static string Xrp(decimal amount) { return amount.ToString("0.######", CultureInfo.InvariantCulture); }
        private void SetMessage(string value) { lock (_viewGate) _message = value; }
        private void SetMonitoring(string value) { lock (_viewGate) _monitoring = value; }

        private static bool IsTemporary(Exception ex, CancellationToken token)
        {
            if (token.IsCancellationRequested) return false;
            var rpc = ex as XrplRpcException;
            return ex is HttpRequestException || ex is OperationCanceledException ||
                (ex is IOException && !(ex is InvalidDataException)) ||
                (rpc != null && new[] { "tooBusy", "noNetwork", "noCurrent", "noClosed", "failedToForward" }.Contains(rpc.Code));
        }

        public static string Describe(Exception ex)
        {
            if (ex is OperationCanceledException) return "The operation was cancelled or timed out. Check any saved payment receipt before trying another payment.";
            if (ex is CryptographicException) return "The local wallet file cannot be decrypted by this Windows account.";
            if (ex is HttpRequestException) return "The Testnet connection failed. No final payment outcome can be inferred from a connection error.";
            if (ex is XrplRpcException) return "The XRPL server rejected a request (" + ((XrplRpcException)ex).Code + ").";
            if (ex is ArgumentException || ex is InvalidOperationException || ex is InvalidDataException) return ex.Message;
            return "The operation could not be completed (" + ex.GetType().Name + "). Saved data was retained.";
        }
    }
}
