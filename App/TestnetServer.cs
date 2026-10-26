using System;
using Ripple;

namespace Ripple.WebForms.Sample
{
    // XRPL's official public-server list: https://xrpl.org/docs/tutorials/public-servers
    public sealed class TestnetServer
    {
        public string Id { get; }
        public string Name { get; }
        public Uri RpcEndpoint { get; }
        public Uri WebSocketEndpoint { get; }

        private TestnetServer(string id, string name, string rpc, string socket)
        { Id = id; Name = name; RpcEndpoint = new Uri(rpc); WebSocketEndpoint = new Uri(socket); }

        private static readonly TestnetServer[] Servers = {
            new TestnetServer("ripple", "Ripple Testnet", "https://s.altnet.rippletest.net:51234/", "wss://s.altnet.rippletest.net:51233/"),
            new TestnetServer("xrpl-labs", "XRPL Labs Testnet", "https://testnet.xrpl-labs.com/", "wss://testnet.xrpl-labs.com/"),
            new TestnetServer("honeycluster", "Honeycluster Testnet", "https://testnet.honeycluster.io/", "wss://testnet.honeycluster.io/")
        };

        public static TestnetServer Get(string id)
        {
            foreach (var server in Servers) if (server.Id == id) return server;
            throw new ArgumentException("Choose a listed XRPL Testnet server.", nameof(id));
        }

        internal static bool Contains(Uri endpoint)
        {
            foreach (var server in Servers) if (server.RpcEndpoint == endpoint) return true;
            return false;
        }

        internal XrplClient CreateClient()
        { return new XrplClient(new XrplClientOptions { RpcEndpoint = RpcEndpoint, WebSocketEndpoint = WebSocketEndpoint }); }
    }
}
