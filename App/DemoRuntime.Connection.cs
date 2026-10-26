using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace Ripple.WebForms.Sample
{
    public sealed partial class DemoRuntime
    {
        private CancellationTokenSource _connectionStop;
        private Task _connectionTask = Task.CompletedTask;

        public TestnetServer CurrentServer => TestnetServer.Get(_store.Snapshot().ServerId);

        private void StartConnection()
        {
            _connectionStop = CancellationTokenSource.CreateLinkedTokenSource(_shutdown.Token);
            _connectionTask = Task.WhenAll(MaintainAsync(_connectionStop.Token), ReceiveAsync(_connectionStop.Token));
        }

        private async Task StopConnectionAsync()
        {
            var stop = _connectionStop;
            if (stop == null) return;
            try
            {
                stop.Cancel();
                try { await _connectionTask.ConfigureAwait(false); }
                catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
            }
            finally { stop.Dispose(); _connectionStop = null; }
        }

        public Task<bool> ChangeServerAsync(string serverId, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                var server = TestnetServer.Get(serverId);
                var state = _store.Snapshot();
                if (state.ServerId == server.Id) return false;
                // Receipts are bound to their original RPC endpoint. Finish their
                // outcome checks before replacing the client; never rebind or resubmit.
                if (state.Transfers.Any(IsUnresolved) || state.AccountDeletes.Any(x =>
                    x.Receipt != null && !x.FinishedWithoutSubmission && !IsFinal(x.Status)))
                    throw new InvalidOperationException("Wait until saved payment and deletion receipts have a final result before changing servers.");

                XrplClient nextClient = server.CreateClient();
                try
                {
                    var network = await nextClient.GetNetworkInfoAsync(ct).ConfigureAwait(false);
                    if (network.NetworkId != 1 || network.ValidatedLedgerIndex == 0 || network.ValidatedLedgerIndex == uint.MaxValue)
                        throw new InvalidDataException("The selected server did not return a validated XRPL Testnet ledger. The current server was retained.");
                    try
                    {
                        await StopConnectionAsync().ConfigureAwait(false);
                        ct.ThrowIfCancellationRequested();
                        _store.Update(next => next.ServerId = server.Id);
                        var previous = _client;
                        _client = nextClient;
                        nextClient = null;
                        previous.Dispose();
                        Interlocked.Exchange(ref _refreshBalances, 1);
                        _nextFeeRefreshUtc = DateTime.MinValue;
                        lock (_viewGate) _estimatedFee = "—";
                        SetMonitoring("Connecting to the selected Testnet server.");
                    }
                    finally { if (!_shutdown.IsCancellationRequested) StartConnection(); }
                }
                finally { nextClient?.Dispose(); }
                return true;
            }, token);
        }
    }
}
