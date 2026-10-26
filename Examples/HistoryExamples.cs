using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class HistoryExamples
    {
        public static async Task<PaymentHistoryPage> ReadPageAsync(XrplClient client,
            string address, uint? firstLedger, uint? lastLedger, int pageSize,
            string continuationToken, CancellationToken token)
        {
            var page = await client.GetPaymentsAsync(address, new PaymentHistoryRequest {
                FromLedger = firstLedger, ToLedger = lastLedger, Limit = pageSize,
                ContinuationToken = continuationToken
            }, token);
            // Check RequestedRangeAvailable before treating this as complete history.
            // For the next page, pass page.ContinuationToken unchanged with the same
            // client, account and search parameters. null means no next page.
            // Each payment exposes status, DeliveredAmountXrp, tag and FeeXrp.
            return page;
        }
    }
}
