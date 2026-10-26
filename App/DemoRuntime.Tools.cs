using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Ripple;
using Newtonsoft.Json.Linq;
using Ripple.LibraryClasses.Signer;

namespace Ripple.WebForms.Sample
{
    public sealed partial class DemoRuntime
    {
        internal void ReportToolMessage(string message) { SetMessage(message); }
        internal string SavedReceiverAddress { get { return _store.Snapshot().Receiver?.Address; } }
        internal string SavedSenderAddress { get { return _store.Snapshot().Sender?.Address; } }

        public Task<AccountInfo> ReadAccountDetailsAsync(string address, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                return await _client.GetAccountAsync(address, ct).ConfigureAwait(false);
            }, token);
        }

        public Task<NetworkInfo> ReadNetworkDetailsAsync(CancellationToken token)
        { return RunQueryAsync(ReadTestnetNetworkInfoAsync, token); }

        private async Task<NetworkInfo> ReadTestnetNetworkInfoAsync(CancellationToken token)
        {
            var network = await _client.GetNetworkInfoAsync(token).ConfigureAwait(false);
            if (network.NetworkId != 1 || network.ValidatedLedgerIndex == 0 || network.ValidatedLedgerIndex == uint.MaxValue)
                throw new InvalidDataException("The fixed server did not confirm XRPL Testnet (network_id 1). Nothing was submitted.");
            return network;
        }

        public Task<string> SignOfflineAsync(string amountText, string destination, uint? tag, string memo,
            uint sequence, string feeDrops, uint? lastLedger, CancellationToken token)
        {
            return RunQueryAsync(ct => {
                ct.ThrowIfCancellationRequested();
                var state = _store.Snapshot();
                if (state.Sender == null) throw new InvalidOperationException("Create the sender wallet first.");
                ulong fee;
                if (!ulong.TryParse(feeDrops?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out fee) || fee == 0)
                    throw new ArgumentException("Fee must be a positive integer number of drops.");
                var tx = new JObject { ["TransactionType"] = "Payment", ["Account"] = state.Sender.Address,
                    ["Destination"] = ResolveDestination(state, destination),
                    ["Amount"] = (ParseAmount(amountText) * 1000000m).ToString("0", CultureInfo.InvariantCulture),
                    ["Fee"] = fee.ToString(CultureInfo.InvariantCulture), ["Sequence"] = sequence };
                if (tag.HasValue) tx["DestinationTag"] = tag.Value;
                if (lastLedger.HasValue) tx["LastLedgerSequence"] = lastLedger.Value;
                if (memo != null)
                {
                    byte[] bytes = new UTF8Encoding(false, true).GetBytes(memo);
                    if (bytes.Length > 1000) throw new ArgumentException("Memo exceeds 1000 UTF-8 bytes.");
                    tx["Memos"] = new JArray(new JObject { ["Memo"] = new JObject { ["MemoData"] = B16.Encode(bytes) } });
                }
                tx["Seed"] = StateStore.ReadSeed(state.Sender);
                try { return Task.FromResult(RippleClient.SignTransaction(tx)); }
                finally { tx.Remove("Seed"); }
            }, token);
        }

        public Task<TransactionInfo> ReadTransactionAsync(string hash, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                return await _client.GetTransactionAsync(hash, ct).ConfigureAwait(false);
            }, token);
        }

        public Task<PaymentHistoryPage> ReadHistoryAsync(string address, PaymentHistoryRequest request, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                return await _client.GetPaymentsAsync(address, request, ct).ConfigureAwait(false);
            }, token);
        }

        public Task<PaymentQuote> QuoteAsync(string amountText, CancellationToken token)
        { return QuoteAsync(amountText, null, null, token); }

        public Task<PaymentQuote> QuoteAsync(string amountText, uint? tag, string memo, CancellationToken token)
        { return QuoteAsync(amountText, null, tag, memo, token); }

        public Task<PaymentQuote> QuoteAsync(string amountText, string destination, uint? tag, string memo, CancellationToken token)
        {
            return RunQueryAsync(async ct => {
                var state = _store.Snapshot();
                if (state.Sender == null) throw new InvalidOperationException("Create the sender wallet first.");
                string recipient = ResolveDestination(state, destination);
                decimal amount = ParseAmount(amountText);
                await VerifyTestnetAsync(ct).ConfigureAwait(false);
                return await _client.GetPaymentQuoteAsync(state.Sender.Address,
                    new XrpPaymentRequest { Destination = recipient, AmountXrp = amount,
                        DestinationTag = tag, Memo = memo }, ct).ConfigureAwait(false);
            }, token);
        }

        private static string ResolveDestination(SampleState state, string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            if (state.Receiver == null) throw new InvalidOperationException("Create the receiver wallet or enter a destination address.");
            return state.Receiver.Address;
        }

        private async Task<T> RunQueryAsync<T>(Func<CancellationToken, Task<T>> query, CancellationToken token)
        {
            T result = default(T);
            await RunActionAsync(async ct => { result = await query(ct).ConfigureAwait(false); }, token).ConfigureAwait(false);
            return result;
        }

        private static decimal ParseAmount(string value)
        {
            decimal amount;
            if (!decimal.TryParse(value?.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out amount) ||
                amount <= 0 || amount > 100000000000m || decimal.Truncate(amount * 1000000m) != amount * 1000000m)
                throw new ArgumentException("Enter a positive XRP amount with at most six decimal places, using a decimal point.");
            return amount;
        }
    }
}
