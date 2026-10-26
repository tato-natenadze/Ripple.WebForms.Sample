using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace Ripple.WebForms.Sample
{
    public partial class DefaultPage : Page
    {
        protected global::System.Web.UI.HtmlControls.HtmlForm MainForm;
        protected Literal ActionMessage;
        protected Button CreateSender, CreateReceiver, FundSender, Send;
        protected Label AmountLabel;
        protected TextBox Amount;

        protected override void OnInit(EventArgs e)
        {
            Response.Cache.SetCacheability(System.Web.HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            if (Session["ViewStateUserKey"] == null)
                Session["ViewStateUserKey"] = Guid.NewGuid().ToString("N");
            ViewStateUserKey = (string)Session["ViewStateUserKey"];
            InitializeInputErrors();
            base.OnInit(e);
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            ClearInputErrors();
            ActionMessage.Text = IsPostBack ? string.Empty : "Loading saved wallets…";
            if (!IsPostBack) { ViewState["SendId"] = Guid.NewGuid().ToString("N"); ViewState["FaucetId"] = Guid.NewGuid().ToString("N"); ViewState["DeleteId"] = Guid.NewGuid().ToString("N"); }
        }

        protected void CreateSender_Click(object sender, EventArgs e)
        { Register(() => WebApplication.Runtime.CreateWalletAsync(true, CancellationToken.None)); }
        protected void CreateReceiver_Click(object sender, EventArgs e)
        { Register(() => WebApplication.Runtime.CreateWalletAsync(false, CancellationToken.None)); }
        protected void FundSender_Click(object sender, EventArgs e)
        { string id = ViewState["FaucetId"] as string; Register(() => WebApplication.Runtime.FundSenderAsync(id, CancellationToken.None), "FaucetId"); }
        protected void Send_Click(object sender, EventArgs e)
        { QuoteResult.Text = string.Empty; string id = ViewState["SendId"] as string;
            RegisterTool(() => WebApplication.Runtime.SendAsync(id, Amount.Text, PaymentDestination.Text,
                ReadPaymentTag(), ReadPaymentMemo(), CancellationToken.None), null, "SendId", PaymentErrorField); }

        private void Register(Func<Task> action, string nextId = null,
            Func<Exception, WebControl> errorField = null, bool reportError = false)
        {
            RegisterAsyncTask(new PageAsyncTask(async () => {
                try
                {
                    if (WebApplication.Runtime == null) throw new InvalidOperationException(WebApplication.StartupError ?? "The sample is not running.");
                    await action();
                }
                catch (Exception ex)
                {
                    var field = (ex as FieldInputException)?.Field ?? errorField?.Invoke(ex);
                    if (field != null)
                    {
                        ShowInputError(field, DemoRuntime.Describe(ex));
                        // The status poll must not repeat a field error at the top.
                        WebApplication.Runtime?.ReportToolMessage(string.Empty);
                    }
                    else
                    {
                        ActionMessage.Text = DemoRuntime.Describe(ex);
                        if (reportError) WebApplication.Runtime?.ReportToolMessage(ActionMessage.Text);
                    }
                }
                finally { if (nextId != null) ViewState[nextId] = Guid.NewGuid().ToString("N"); }
            }));
        }
    }
}
