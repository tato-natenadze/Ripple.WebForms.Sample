using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using Newtonsoft.Json;
using Ripple;

namespace Ripple.WebForms.Sample
{
    public partial class DefaultPage
    {
        protected Button Quote;
        protected Literal QuoteResult;
        protected TextBox PaymentDestination, PaymentTag, PaymentMemo;
        protected TextBox ValidationAddress;
        protected Button ValidateClassic;
        protected Literal ValidationResult;
        protected TextBox XClassicAddress, XTag, XAddressInput;
        protected CheckBox XTestNetwork;
        protected Button EncodeX, DecodeX, ValidateX;
        protected Literal XResult;
        protected TextBox HistoryAddress, HistoryFrom, HistoryTo, HistoryLimit;
        protected Button SearchHistory, NextHistory;
        protected Literal HistorySummary;
        protected GridView HistoryResults;
        protected TextBox TransactionHash;
        protected Button QueryTransaction;
        protected Literal TransactionResult;
        protected TextBox SigningAccount, SigningSeed, SigningAmount, SigningDestination, SigningTag, SigningMemo;
        protected System.Web.UI.WebControls.HiddenField SigningDefaultsReady;
        protected TextBox SigningSequence, SigningFee, SigningLastLedger;
        protected Button SignOffline;
        protected Literal OfflineResult;
        protected TextBox AccountAddress;
        protected Button QueryAccount, QueryNetwork;
        protected Literal AccountResult, NetworkResult;

        protected void QueryAccount_Click(object sender, EventArgs e)
        {
            AccountResult.Text = string.Empty;
            RegisterTool(async () => {
                string address = AccountAddress.Text.Trim();
                if (address.Length == 0) address = WebApplication.Runtime.SavedSenderAddress;
                if (string.IsNullOrEmpty(address)) throw new FieldInputException(AccountAddress, "Enter an address or create the sender wallet first.");
                var account = await WebApplication.Runtime.ReadAccountDetailsAsync(address, CancellationToken.None);
                AccountResult.Text = JsonConvert.SerializeObject(account, Formatting.Indented);
            }, "Account details loaded from Testnet.", errorField: ex => InputException(ex) ? AccountAddress : null);
        }

        protected void QueryNetwork_Click(object sender, EventArgs e)
        {
            NetworkResult.Text = string.Empty;
            RegisterTool(async () => {
                var network = await WebApplication.Runtime.ReadNetworkDetailsAsync(CancellationToken.None);
                NetworkResult.Text = JsonConvert.SerializeObject(network, Formatting.Indented);
            }, "Network details loaded from Testnet.");
        }

        protected void SignOffline_Click(object sender, EventArgs e)
        {
            OfflineResult.Text = string.Empty;
            RegisterTool(async () => {
                uint sequence = ReadOptionalUInt(SigningSequence, "Sequence") ?? throw new FieldInputException(SigningSequence, "Sequence is required.");
                OfflineResult.Text = await WebApplication.Runtime.SignOfflineAsync(SigningAccount.Text.Trim(), SigningSeed.Text.Trim(),
                    SigningAmount.Text, SigningDestination.Text.Trim(), ReadOptionalUInt(SigningTag, "Destination tag"),
                    SigningMemo.Text.Length == 0 ? null : SigningMemo.Text, sequence, SigningFee.Text,
                    ReadOptionalUInt(SigningLastLedger, "Last ledger sequence"), CancellationToken.None);
            }, "Payment signed locally. Nothing was submitted.", errorField: SigningErrorField);
        }

        protected void QueryTransaction_Click(object sender, EventArgs e)
        {
            TransactionResult.Text = string.Empty;
            RegisterTool(async () => {
                var transaction = await WebApplication.Runtime.ReadTransactionAsync(TransactionHash.Text.Trim(), CancellationToken.None);
                TransactionResult.Text = JsonConvert.SerializeObject(transaction, Formatting.Indented,
                    new Newtonsoft.Json.Converters.StringEnumConverter());
            }, "Transaction lookup completed.", errorField: ex => InputException(ex) ? TransactionHash : null);
        }

        protected override void OnPreRenderComplete(EventArgs e)
        {
            bool hasNext = !string.IsNullOrEmpty(ViewState["HistoryCursor"] as string);
            NextHistory.Enabled = hasNext;
            NextHistory.Attributes["data-available"] = hasNext ? "true" : "false";
            InitializeSigningDefaults();
            BindDeleteAvailability();
            BindTestnetServer();
            BindImplementationExamples();
            base.OnPreRenderComplete(e);
        }

        protected void SearchHistory_Click(object sender, EventArgs e)
        {
            ViewState["HistoryCursor"] = null;
            HistorySummary.Text = string.Empty;
            HistoryResults.DataSource = null;
            HistoryResults.DataBind();
            RegisterTool(async () => {
                string address = ResolveReceiverAddress(HistoryAddress.Text);
                var request = new PaymentHistoryRequest { FromLedger = ReadOptionalUInt(HistoryFrom, "First ledger"),
                    ToLedger = ReadOptionalUInt(HistoryTo, "Last ledger"), Limit = ParseHistoryLimit() };
                var page = await WebApplication.Runtime.ReadHistoryAsync(address, request, CancellationToken.None);
                ViewState["HistoryAddress"] = address;
                ViewState["HistoryFrom"] = request.FromLedger;
                ViewState["HistoryTo"] = request.ToLedger;
                ViewState["HistoryLimit"] = request.Limit;
                ShowHistory(address, page);
            }, "Payment history loaded from Testnet.", errorField: HistoryErrorField);
        }

        protected void NextHistory_Click(object sender, EventArgs e)
        {
            RegisterTool(async () => {
                string cursor = ViewState["HistoryCursor"] as string;
                if (string.IsNullOrEmpty(cursor)) throw new InvalidOperationException("Run a history search first.");
                string address = (string)ViewState["HistoryAddress"];
                var page = await WebApplication.Runtime.ReadHistoryAsync(address, new PaymentHistoryRequest {
                    FromLedger = ViewState["HistoryFrom"] as uint?, ToLedger = ViewState["HistoryTo"] as uint?,
                    Limit = (int)ViewState["HistoryLimit"], ContinuationToken = cursor }, CancellationToken.None);
                ShowHistory(address, page);
            }, "Next payment-history page loaded.");
        }

        private void ShowHistory(string address, PaymentHistoryPage page)
        {
            HistorySummary.Text = JsonConvert.SerializeObject(new { Account = address, page.FromLedger,
                page.ToLedger, page.RequestedRangeAvailable }, Formatting.Indented);
            HistoryResults.DataSource = page.Payments;
            HistoryResults.DataBind();
            ViewState["HistoryCursor"] = page.ContinuationToken;
        }

        private string ResolveReceiverAddress(string value)
        {
            if (!string.IsNullOrWhiteSpace(value)) return value.Trim();
            return WebApplication.Runtime.SavedReceiverAddress ?? throw new FieldInputException(HistoryAddress, "Enter an address or create the receiver wallet first.");
        }

        private int ParseHistoryLimit()
        {
            int limit;
            if (!int.TryParse(HistoryLimit.Text.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out limit))
                throw new FieldInputException(HistoryLimit, "Page size must be a whole number from 1 to 400.");
            return limit;
        }

        protected void EncodeX_Click(object sender, EventArgs e)
        {
            XResult.Text = string.Empty;
            RegisterTool(() => {
                XResult.Text = JsonConvert.SerializeObject(new {
                    XAddress = XAddressCodec.Encode(XClassicAddress.Text.Trim(), ReadOptionalUInt(XTag, "Destination tag"), XTestNetwork.Checked)
                }, Formatting.Indented);
                return Task.CompletedTask;
            }, "X-address encoded.", errorField: ex => InputException(ex) ? XClassicAddress : null);
        }

        protected void DecodeX_Click(object sender, EventArgs e)
        {
            XResult.Text = string.Empty;
            RegisterTool(() => {
                XResult.Text = JsonConvert.SerializeObject(XAddressCodec.Decode(XAddressInput.Text.Trim()), Formatting.Indented);
                return Task.CompletedTask;
            }, "X-address decoded.", errorField: ex => InputException(ex) ? XAddressInput : null);
        }

        protected void ValidateX_Click(object sender, EventArgs e)
        {
            XResult.Text = string.Empty;
            RegisterTool(() => {
                bool valid = XAddressCodec.IsValid(XAddressInput.Text.Trim());
                XResult.Text = JsonConvert.SerializeObject(new { IsValid = valid }, Formatting.Indented);
                if (!valid) ShowInputError(XAddressInput, "A valid X-address is required.");
                return Task.CompletedTask;
            }, "X-address checked.");
        }

        private static uint? ReadOptionalUInt(TextBox field, string name)
        {
            string value = field.Text.Trim();
            if (string.IsNullOrEmpty(value)) return null;
            uint parsed;
            if (!uint.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out parsed))
                throw new FieldInputException(field, name + " must be a whole number from 0 to 4294967295.");
            return parsed;
        }

        protected void ValidateClassic_Click(object sender, EventArgs e)
        {
            ValidationResult.Text = string.Empty;
            RegisterTool(() => {
                bool valid = RippleClient.ValidateAddress(ValidationAddress.Text.Trim());
                ValidationResult.Text = JsonConvert.SerializeObject(new { IsValid = valid }, Formatting.Indented);
                if (!valid) ShowInputError(ValidationAddress, "A valid classic XRPL address is required.");
                return Task.CompletedTask;
            }, "Classic address checked.");
        }

        protected void Quote_Click(object sender, EventArgs e)
        {
            QuoteResult.Text = string.Empty;
            RegisterTool(async () => {
                var quote = await WebApplication.Runtime.QuoteAsync(Amount.Text, PaymentDestination.Text,
                    ReadPaymentTag(), ReadPaymentMemo(), CancellationToken.None);
                QuoteResult.Text = JsonConvert.SerializeObject(quote, Formatting.Indented);
            }, "Payment estimate calculated. Nothing was submitted.", errorField: PaymentErrorField);
        }

        private uint? ReadPaymentTag() { return ReadOptionalUInt(PaymentTag, "Destination tag"); }
        private string ReadPaymentMemo() { return PaymentMemo.Text.Length == 0 ? null : PaymentMemo.Text; }

        private void RegisterTool(Func<Task> action, string successMessage, string nextId = null,
            Func<Exception, WebControl> errorField = null)
        {
            Register(async () => {
                await action();
                if (successMessage != null)
                {
                    WebApplication.Runtime.ReportToolMessage(successMessage);
                    ActionMessage.Text = successMessage;
                }
            }, nextId, errorField, true);
        }

        private sealed class FieldInputException : ArgumentException
        {
            internal WebControl Field { get; }
            internal FieldInputException(WebControl field, string message) : base(message) { Field = field; }
        }

        private static bool InputException(Exception ex) => ex is ArgumentException || ex is FormatException;

        private readonly Dictionary<WebControl, HtmlGenericControl> _inputErrors = new Dictionary<WebControl, HtmlGenericControl>();

        private void InitializeInputErrors()
        {
            foreach (var field in new WebControl[] { DeleteSource, DeleteDestination, DeleteTag, DeleteMaxFee, Amount, PaymentDestination, PaymentTag, PaymentMemo, ValidationAddress,
                XClassicAddress, XTag, XAddressInput, HistoryAddress, HistoryFrom, HistoryTo, HistoryLimit,
                TransactionHash, SigningAccount, SigningSeed, SigningAmount, SigningDestination, SigningTag, SigningMemo,
                SigningSequence, SigningFee, SigningLastLedger, AccountAddress, SelectedTestnetServer })
            {
                // Keep the control tree stable across postbacks, including when
                // a previously invalid field becomes valid.
                var error = new HtmlGenericControl("div") { ID = field.ID + "Error", EnableViewState = false, Visible = false };
                error.Attributes["class"] = "field-error";
                error.Attributes["role"] = "alert";
                field.Parent.Controls.AddAt(field.Parent.Controls.IndexOf(field), error);
                _inputErrors.Add(field, error);
            }
        }

        private void ClearInputErrors()
        {
            foreach (var field in _inputErrors.Keys)
            {
                field.CssClass = string.Join(" ", field.CssClass.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Where(value => value != "input-error"));
                field.Attributes.Remove("aria-invalid");
                field.Attributes.Remove("aria-describedby");
            }
        }

        private void ShowInputError(WebControl field, string message)
        {
            var error = _inputErrors[field];
            error.InnerText = message;
            error.Visible = true;
            field.CssClass = (field.CssClass + " input-error").Trim();
            field.Attributes["aria-invalid"] = "true";
            field.Attributes["aria-describedby"] = error.ClientID;
        }

        private TextBox PaymentErrorField(Exception ex)
        {
            if (!(InputException(ex) || ex is InvalidOperationException)) return null;
            string name = (ex as ArgumentException)?.ParamName;
            if (name == "AmountXrp") return Amount;
            if (name == "Destination" || name == "destination" || name == "xAddress") return PaymentDestination;
            if (name == "DestinationTag") return PaymentTag;
            // These existing SDK/sample messages have no unambiguous field parameter.
            // Unrecognized errors retain their existing page-level presentation.
            string message = ex.Message.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            switch (message)
            {
                case "Enter a positive XRP amount with at most six decimal places, using a decimal point.":
                case "Payment is below the destination account activation reserve.":
                case "Insufficient spendable XRP after reserve and fee.": return Amount;
                case "Memo exceeds 1000 UTF-8 bytes.": return PaymentMemo;
                case "Destination requires a DestinationTag.":
                case "DestinationTag conflicts with the X-address.": return PaymentTag;
                case "A direct XRP payment requires different sender and destination.":
                case "The X-address network does not match the connected network.":
                case "Destination has not authorized this sender.":
                case "Create the receiver wallet or enter a destination address.": return PaymentDestination;
                default: return null;
            }
        }

        private void InitializeSigningDefaults()
        {
            if (SigningDefaultsReady.Value == "true" || WebApplication.Runtime == null) return;
            SigningAccount.Text = WebApplication.Runtime.SavedSenderAddress ?? string.Empty;
            SigningSeed.Text = WebApplication.Runtime.SavedSenderSeed ?? string.Empty;
            SigningAmount.Text = Amount.Text;
            SigningDestination.Text = string.IsNullOrWhiteSpace(PaymentDestination.Text)
                ? WebApplication.Runtime.SavedReceiverAddress ?? string.Empty : PaymentDestination.Text.Trim();
            SigningTag.Text = PaymentTag.Text;
            SigningMemo.Text = PaymentMemo.Text;
        }

        private TextBox SigningErrorField(Exception ex)
        {
            string name = (ex as ArgumentException)?.ParamName;
            if (name == "account") return SigningAccount;
            if (name == "seed") return SigningSeed;
            if (name == "destination") return SigningDestination;
            if (name == "feeDrops") return SigningFee;
            if (ex.Message.StartsWith("Enter a positive XRP amount", StringComparison.Ordinal)) return SigningAmount;
            if (ex.Message.StartsWith("Memo exceeds 1000 UTF-8 bytes.", StringComparison.Ordinal)) return SigningMemo;
            return null;
        }

        private TextBox HistoryErrorField(Exception ex)
        {
            if (!InputException(ex)) return null;
            string name = (ex as ArgumentException)?.ParamName;
            if (name == "address") return HistoryAddress;
            if (name == "Limit") return HistoryLimit;
            if (ex.Message.StartsWith("FromLedger must not exceed ToLedger.", StringComparison.Ordinal)) return HistoryFrom;
            return null;
        }
    }
}
