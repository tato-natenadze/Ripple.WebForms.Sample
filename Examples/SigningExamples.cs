using System;
using System.Globalization;
using System.Text;
using Newtonsoft.Json.Linq;
using Ripple;

namespace RippleExamples
{
    public static class SigningExamples
    {
        public static string Sign(string account, string seed, string destination,
            decimal amountXrp, uint sequence, ulong feeDrops, uint? lastLedger,
            uint? tag, string memo)
        {
            if (!RippleClient.ValidateAddress(account)) throw new ArgumentException("Invalid account.", nameof(account));
            if (!RippleClient.ValidateAddress(destination)) throw new ArgumentException("Invalid destination.", nameof(destination));
            if (amountXrp <= 0 || amountXrp > 100000000000m ||
                decimal.Truncate(amountXrp * 1000000m) != amountXrp * 1000000m)
                throw new ArgumentException("Use a positive XRP amount with at most six decimals.", nameof(amountXrp));
            if (feeDrops == 0) throw new ArgumentOutOfRangeException(nameof(feeDrops));
            var tx = new JObject {
                ["TransactionType"] = "Payment", ["Account"] = account,
                ["Destination"] = destination,
                ["Amount"] = (amountXrp * 1000000m).ToString("0", CultureInfo.InvariantCulture),
                ["Sequence"] = sequence,
                ["Fee"] = feeDrops.ToString(CultureInfo.InvariantCulture)
            };
            if (lastLedger.HasValue) tx["LastLedgerSequence"] = lastLedger.Value;
            if (tag.HasValue) tx["DestinationTag"] = tag.Value;
            if (memo != null)
            {
                byte[] bytes = new UTF8Encoding(false, true).GetBytes(memo);
                if (bytes.Length > 1000) throw new ArgumentException("Memo exceeds 1000 UTF-8 bytes.", nameof(memo));
                string hex = BitConverter.ToString(bytes).Replace("-", string.Empty);
                tx["Memos"] = new JArray(new JObject { ["Memo"] = new JObject { ["MemoData"] = hex } });
            }
            tx["Seed"] = seed;
            try { return RippleClient.SignTransaction(tx); } // Hex tx_blob; no submission or network calls.
            finally { tx.Remove("Seed"); }
        }
    }
}
