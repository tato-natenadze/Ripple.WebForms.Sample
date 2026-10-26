using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Ripple;
using Ripple.LibraryClasses.CreateAddress;

namespace RippleExamples
{
    public static class WalletExamples
    {
        public static AddressData Create()
        {
            // Generation is local; funding activates the account on the ledger.
            return RippleClient.GenerateAddress(); // Address and Seed
        }

        public static Task<AccountInfo> ReadAsync(
            XrplClient client, string address, CancellationToken token)
        {
            // BalanceXrp includes reserve. EstimatedSpendableXrp is before fee.
            return client.GetAccountAsync(address, token);
        }

        // The faucet is a Testnet HTTP service, not a Ripple.MultiSupport method.
        // Call once per user action. After a timeout, check the balance first.
        public static async Task<AccountInfo> FundAsync(
            XrplClient client, HttpClient http, string address, CancellationToken token)
        {
            var network = await client.GetNetworkInfoAsync(token);
            if (network.NetworkId != 1)
                throw new InvalidOperationException("Use an XRPL Testnet client.");
            var before = await client.GetAccountAsync(address, token);
            var body = new JObject { ["destination"] = address };
            using (var content = new StringContent(body.ToString(), Encoding.UTF8, "application/json"))
            using (var response = await http.PostAsync(
                "https://faucet.altnet.rippletest.net/accounts", content, token))
            {
                response.EnsureSuccessStatusCode();
            }
            using (var deadline = CancellationTokenSource.CreateLinkedTokenSource(token))
            {
                deadline.CancelAfter(TimeSpan.FromSeconds(60));
                while (true)
                {
                    var account = await client.GetAccountAsync(address, deadline.Token);
                    if (account.Exists && (!before.Exists || account.BalanceXrp > before.BalanceXrp))
                        return account;
                    await Task.Delay(TimeSpan.FromSeconds(2), deadline.Token);
                }
            }
        }
    }
}
