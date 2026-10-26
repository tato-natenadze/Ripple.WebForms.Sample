using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace Ripple.WebForms.Sample
{
    public sealed partial class DemoRuntime
    {
        private static WalletState DeletionWallet(SampleState state, string source)
        {
            WalletState wallet = source == "sender" ? state.Sender : source == "receiver" ? state.Receiver : null;
            return wallet ?? throw new ArgumentException("Choose a saved wallet to delete.", nameof(source));
        }

        public Task<AccountDeleteQuote> QuoteAccountDeleteAsync(string source, string destination, uint? tag,
            decimal maxFee, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                var wallet = DeletionWallet(_store.Snapshot(), source);
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                return await _client.GetAccountDeleteQuoteAsync(wallet.Address, new AccountDeleteRequest {
                    Destination = destination, DestinationTag = tag, MaxFeeXrp = maxFee }, ct).ConfigureAwait(false);
            }, token);
        }

        public Task DeleteAccountAsync(string operationId, string source, string destination, uint? tag,
            decimal maxFee, CancellationToken token)
        {
            return RunActionAsync(async ct => {
                RequireOperationId(operationId);
                var state = _store.Snapshot();
                if (state.AccountDeletes.Any(x => x.OperationId == operationId)) return;
                var wallet = DeletionWallet(state, source);
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                await RecoverAsync(ct).ConfigureAwait(false);
                _store.Update(next => next.AccountDeletes.Add(new AccountDeleteState {
                    OperationId = operationId, Sender = wallet.Address, Detail = "Preparing AccountDelete." }));
                SetMessage("Preparing and submitting AccountDelete once…");
                try
                {
                    var result = await _client.DeleteAccountAsync(StateStore.ReadSeed(wallet), new AccountDeleteRequest {
                        Destination = destination, DestinationTag = tag, MaxFeeXrp = maxFee,
                        OnPreparedAsync = (receipt, callbackToken) => {
                            callbackToken.ThrowIfCancellationRequested();
                            if (receipt.Sender != wallet.Address) throw new InvalidOperationException("Receipt does not match the saved wallet.");
                            _store.Update(next => {
                                var item = next.AccountDeletes.Single(x => x.OperationId == operationId);
                                item.Receipt = receipt; item.Detail = "Receipt saved before submission.";
                            });
                            return Task.CompletedTask;
                        }
                    }, ct).ConfigureAwait(false);
                    ApplyDeleteResult(operationId, result);
                    Interlocked.Exchange(ref _refreshBalances, 1);
                    SetMessage("AccountDelete status: " + result.Status + ".");
                }
                catch (Exception ex)
                {
                    _store.Update(next => {
                        var item = next.AccountDeletes.Single(x => x.OperationId == operationId);
                        item.FinishedWithoutSubmission = item.Receipt == null;
                        item.Detail = Describe(ex);
                    });
                    throw;
                }
            }, token);
        }

        public Task CheckAccountDeletesAsync(CancellationToken token)
        {
            return RunActionAsync(async ct => {
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                await RecoverAsync(ct).ConfigureAwait(false);
            }, token);
        }

        private async Task RecoverAccountDeletesAsync(CancellationToken token)
        {
            foreach (var item in _store.Snapshot().AccountDeletes.Where(x =>
                x.Receipt != null && !x.FinishedWithoutSubmission && !IsFinal(x.Status)))
            {
                var result = await _client.GetAccountDeleteAsync(item.Receipt, token).ConfigureAwait(false);
                ApplyDeleteResult(item.OperationId, result);
                if (IsFinal(result.Status)) Interlocked.Exchange(ref _refreshBalances, 1);
            }
        }

        private void ApplyDeleteResult(string operationId, AccountDeleteResult result)
        {
            _store.Update(next => {
                var item = next.AccountDeletes.Single(x => x.OperationId == operationId);
                if (IsFinal(item.Status)) return;
                item.Status = result.Status; item.FeeXrp = result.Transaction.FeeXrp;
                item.ReturnedAmountXrp = result.ReturnedAmountXrp;
                if (result.Error != null) item.Error = PaymentErrorState.From(result.Error);
                item.Detail = result.Diagnostic ?? result.Transaction.ResultCode ?? result.Status.ToString();
            });
        }
    }
}
