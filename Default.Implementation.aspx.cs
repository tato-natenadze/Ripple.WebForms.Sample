using System.IO;
using System.Text;
using System.Web.UI.WebControls;

namespace Ripple.WebForms.Sample
{
    public partial class DefaultPage
    {
        protected HiddenField ActiveExamples;
        protected Literal ImplementationWallets, ImplementationPayments, ImplementationAddresses, ImplementationHistory, ImplementationTransactions, ImplementationAccount, ImplementationSigning, ImplementationDelete, ImplementationTestnet;

        private void BindImplementationExamples()
        {
            ImplementationWallets.Text = ReadExample("WalletExamples.cs");
            ImplementationPayments.Text = ReadExample("PaymentExamples.cs");
            ImplementationAddresses.Text = ReadExample("AddressExamples.cs");
            ImplementationHistory.Text = ReadExample("HistoryExamples.cs");
            ImplementationTransactions.Text = ReadExample("TransactionExamples.cs");
            ImplementationAccount.Text = ReadExample("AccountExamples.cs");
            ImplementationSigning.Text = ReadExample("SigningExamples.cs");
            ImplementationDelete.Text = ReadExample("DeleteExamples.cs");
            ImplementationTestnet.Text = ReadExample("TestnetExamples.cs");
        }

        private string ReadExample(string fileName)
        {
            // Filenames are fixed above, never supplied by form/query input.
            // Encode at the Literal; display the same source files compiled below.
            return File.ReadAllText(Server.MapPath("~/Examples/" + fileName), Encoding.UTF8);
        }
    }
}
