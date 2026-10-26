using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class AccountExamples
    {
        public static Task<AccountInfo> ReadAccountAsync(
            XrplClient client, string address, CancellationToken token)
        {
            // Sequence is the value used by the Offline signing example.
            // Also returns Exists, balance, reserve, spendable XRP and account flags.
            return client.GetAccountAsync(address, token);
        }

        public static Task<NetworkInfo> ReadNetworkAsync(
            XrplClient client, CancellationToken token)
        {
            // NetworkId, ValidatedLedgerIndex, BaseReserveXrp, OwnerReserveXrp.
            return client.GetNetworkInfoAsync(token);
        }
    }
}
