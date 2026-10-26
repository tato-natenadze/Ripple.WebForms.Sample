using System;
using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class TestnetExamples
    {
        // Keep one shared client for the application's lifetime. Pass it to the
        // examples in the other tabs, then cancel monitoring and dispose at shutdown.
        public static async Task<XrplClient> CreateAsync(string server, CancellationToken token)
        {
            string rpc, socket;
            switch (server)
            {
                case "ripple":
                    rpc = "https://s.altnet.rippletest.net:51234/";
                    socket = "wss://s.altnet.rippletest.net:51233/";
                    break;
                case "xrpl-labs":
                    rpc = "https://testnet.xrpl-labs.com/";
                    socket = "wss://testnet.xrpl-labs.com/";
                    break;
                case "honeycluster":
                    rpc = "https://testnet.honeycluster.io/";
                    socket = "wss://testnet.honeycluster.io/";
                    break;
                default: throw new ArgumentException("Choose a listed Testnet server.", nameof(server));
            }
            var client = new XrplClient(new XrplClientOptions {
                RpcEndpoint = new Uri(rpc), WebSocketEndpoint = new Uri(socket)
            });
            try
            {
                var network = await client.GetNetworkInfoAsync(token);
                if (network.NetworkId != 1 || network.ValidatedLedgerIndex == 0 || network.ValidatedLedgerIndex == uint.MaxValue)
                    throw new InvalidOperationException("The server did not confirm a validated XRPL Testnet ledger.");
                return client;
            }
            catch { client.Dispose(); throw; }
        }
    }
}
