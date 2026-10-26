using System;
using System.Globalization;
using System.Threading;
using System.Web.UI.WebControls;
using Newtonsoft.Json;

namespace Ripple.WebForms.Sample
{
    public partial class DefaultPage
    {
        protected DropDownList DeleteSource;
        protected TextBox DeleteDestination, DeleteTag, DeleteMaxFee;
        protected Button QuoteDelete, DeleteAccount, CheckDeletes;
        protected Literal DeleteQuoteResult;

        private string DeleteInputKey() => JsonConvert.SerializeObject(new[] {
            DeleteSource.SelectedValue, DeleteDestination.Text.Trim(), DeleteTag.Text.Trim(), DeleteMaxFee.Text.Trim() });

        private decimal ReadDeleteMaxFee()
        {
            decimal fee;
            if (!decimal.TryParse(DeleteMaxFee.Text.Trim(), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out fee) || fee <= 0 || fee > 100000000000m ||
                decimal.Truncate(fee * 1000000m) != fee * 1000000m)
                throw new FieldInputException(DeleteMaxFee, "Enter a positive XRP fee limit with at most six decimal places.");
            return fee;
        }

        private void ValidateDeleteFields()
        {
            if (DeleteSource.SelectedValue.Length == 0)
                throw new FieldInputException(DeleteSource, "Choose the saved wallet to delete.");
            if (string.IsNullOrWhiteSpace(DeleteDestination.Text))
                throw new FieldInputException(DeleteDestination, "Enter a different, funded destination address.");
        }

        protected void QuoteDelete_Click(object sender, EventArgs e)
        {
            ViewState["DeleteQuoteKey"] = null; DeleteQuoteResult.Text = string.Empty;
            RegisterTool(async () => {
                ValidateDeleteFields();
                var quote = await WebApplication.Runtime.QuoteAccountDeleteAsync(DeleteSource.SelectedValue,
                    DeleteDestination.Text.Trim(), ReadOptionalUInt(DeleteTag, "Destination tag"),
                    ReadDeleteMaxFee(), CancellationToken.None);
                DeleteQuoteResult.Text = JsonConvert.SerializeObject(quote, Formatting.Indented);
                if (quote.CanDelete) ViewState["DeleteQuoteKey"] = DeleteInputKey();
            }, "Deletion conditions checked. Nothing was submitted.", errorField: DeleteErrorField);
        }

        protected void DeleteAccount_Click(object sender, EventArgs e)
        {
            bool quoted = string.Equals(ViewState["DeleteQuoteKey"] as string, DeleteInputKey(), StringComparison.Ordinal);
            ViewState["DeleteQuoteKey"] = null;
            string operationId = ViewState["DeleteId"] as string;
            RegisterTool(async () => {
                ValidateDeleteFields();
                if (!quoted) throw new InvalidOperationException("Check deletion conditions for these exact inputs first.");
                await WebApplication.Runtime.DeleteAccountAsync(operationId, DeleteSource.SelectedValue,
                    DeleteDestination.Text.Trim(), ReadOptionalUInt(DeleteTag, "Destination tag"),
                    ReadDeleteMaxFee(), CancellationToken.None);
            }, null, "DeleteId", DeleteErrorField);
        }

        protected void CheckDeletes_Click(object sender, EventArgs e)
        {
            RegisterTool(() => WebApplication.Runtime.CheckAccountDeletesAsync(CancellationToken.None),
                "Saved AccountDelete receipts checked. Nothing was resubmitted.");
        }

        private WebControl DeleteErrorField(Exception ex)
        {
            if (ex is InvalidOperationException && ex.Message == "The account to delete is not funded.") return DeleteSource;
            if (!InputException(ex)) return null;
            string name = (ex as ArgumentException)?.ParamName;
            if (name == "source") return DeleteSource;
            if (name == "MaxFeeXrp") return DeleteMaxFee;
            if (name == "DestinationTag" || ex.Message.StartsWith("DestinationTag conflicts", StringComparison.Ordinal))
                return DeleteTag;
            if (name == "Destination" || name == "request" || name == "xAddress") return DeleteDestination;
            return null;
        }

        private void BindDeleteAvailability()
        {
            string key = ViewState["DeleteQuoteKey"] as string;
            bool available = key != null && key == DeleteInputKey();
            DeleteAccount.Enabled = available;
            DeleteAccount.Attributes["data-available"] = available ? "true" : "false";
            DeleteAccount.Attributes["data-quote"] = key ?? string.Empty;
        }
    }
}
