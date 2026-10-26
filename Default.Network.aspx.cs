using System;
using System.Threading;
using System.Web.UI.WebControls;

namespace Ripple.WebForms.Sample
{
    public partial class DefaultPage
    {
        protected DropDownList SelectedTestnetServer;
        protected Button ApplyTestnetServer;
        protected Literal CurrentServerName, CurrentRpcEndpoint, CurrentSocketEndpoint;

        private void BindTestnetServer()
        {
            if (WebApplication.Runtime == null) return;
            var server = WebApplication.Runtime.CurrentServer;
            CurrentServerName.Text = server.Name;
            CurrentRpcEndpoint.Text = server.RpcEndpoint.AbsoluteUri;
            CurrentSocketEndpoint.Text = server.WebSocketEndpoint.AbsoluteUri;
            if (!IsPostBack) SelectedTestnetServer.SelectedValue = server.Id;
        }

        protected void ApplyTestnetServer_Click(object sender, EventArgs e)
        {
            RegisterTool(async () => {
                bool changed = await WebApplication.Runtime.ChangeServerAsync(SelectedTestnetServer.SelectedValue, CancellationToken.None);
                // The library's history cursor is also bound to its original endpoint.
                if (changed) ViewState["HistoryCursor"] = null;
            }, "Testnet server settings applied.", errorField: ex => ex is ArgumentException ? SelectedTestnetServer : null);
        }
    }
}
