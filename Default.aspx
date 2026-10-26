<%@ Page Language="C#" AutoEventWireup="true" Async="true" AsyncTimeout="120" CodeBehind="Default.aspx.cs" Inherits="Ripple.WebForms.Sample.DefaultPage" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Ripple Web Forms Sample — XRPL Testnet</title>
    <link rel="stylesheet" href="sample.css?v=a09c1c790b99" />
</head>
<body>
<form id="MainForm" runat="server">
    <asp:ScriptManager ID="AjaxManager" runat="server" AsyncPostBackTimeout="120" />
    <main>
        <header><h1>Ripple Web Forms Sample</h1><p>XRPL Testnet · .NET Framework 4.7.2</p><p class="testnet-banner"><strong>TESTNET DEMO</strong> — All network operations use XRPL Testnet and test XRP. Wallet seeds are visible for testing. Use test-wallet seeds only.</p><p class="sample-disclaimer">Provided AS IS, without warranties of any kind. Use at your own risk. To the extent permitted by applicable law, the authors and contributors accept no liability for any loss, damage or claim arising from use of this sample.</p></header>
        <asp:UpdatePanel ID="MessageUpdate" runat="server"><ContentTemplate>
            <p id="connection" class="message" role="status" aria-live="polite"><asp:Literal ID="ActionMessage" runat="server" Mode="Encode" /></p>
        </ContentTemplate><Triggers>
            <asp:AsyncPostBackTrigger ControlID="CreateSender" EventName="Click" />
            <asp:AsyncPostBackTrigger ControlID="CreateReceiver" EventName="Click" />
            <asp:AsyncPostBackTrigger ControlID="FundSender" EventName="Click" />
        </Triggers></asp:UpdatePanel>
        <asp:HiddenField ID="ActiveTab" runat="server" Value="wallets" ClientIDMode="Static" />
        <asp:HiddenField ID="ActiveExamples" runat="server" Value="{}" ClientIDMode="Static" />
        <div class="tabs" role="tablist" aria-label="Sample functions">
            <button type="button" id="tab-wallets" class="tab-button" role="tab" aria-controls="panel-wallets" aria-selected="true" tabindex="0" data-tab="wallets">Wallets</button>
            <button type="button" id="tab-payments" class="tab-button" role="tab" aria-controls="panel-payments" aria-selected="false" tabindex="-1" data-tab="payments">Payments</button>
            <button type="button" id="tab-addresses" class="tab-button" role="tab" aria-controls="panel-addresses" aria-selected="false" tabindex="-1" data-tab="addresses">Addresses</button>
            <button type="button" id="tab-history" class="tab-button" role="tab" aria-controls="panel-history" aria-selected="false" tabindex="-1" data-tab="history">History</button>
            <button type="button" id="tab-transactions" class="tab-button" role="tab" aria-controls="panel-transactions" aria-selected="false" tabindex="-1" data-tab="transactions">Transactions</button>
            <button type="button" id="tab-account" class="tab-button" role="tab" aria-controls="panel-account" aria-selected="false" tabindex="-1" data-tab="account">Account &amp; network</button>
            <button type="button" id="tab-signing" class="tab-button" role="tab" aria-controls="panel-signing" aria-selected="false" tabindex="-1" data-tab="signing">Offline signing</button>
            <button type="button" id="tab-delete" class="tab-button" role="tab" aria-controls="panel-delete" aria-selected="false" tabindex="-1" data-tab="delete">Delete account</button>
            <button type="button" id="tab-testnet" class="tab-button" role="tab" aria-controls="panel-testnet" aria-selected="false" tabindex="-1" data-tab="testnet">Testnet servers</button>
        </div>
        <div id="panel-wallets" class="tab-panel" role="tabpanel" aria-labelledby="tab-wallets">
        <h2 class="section-title">Wallets</h2>
        <div class="example-tabs" role="tablist" aria-label="Wallets view" data-topic="wallets">
            <button type="button" id="example-wallets-test" class="tab-button" role="tab" aria-controls="test-wallets" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-wallets-implementation" class="tab-button" role="tab" aria-controls="implementation-wallets" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-wallets" class="example-panel" role="tabpanel" aria-labelledby="example-wallets-test">
        <section class="wallets" aria-label="Testnet wallets">
            <article class="card">
                <h2>Sender</h2>
                <p class="wallet-detail-label">Testnet address</p>
                <p class="address" id="senderAddress">Not created</p>
                <p class="wallet-detail-label">Testnet seed</p>
                <p class="wallet-seed" id="senderSeed">Not created</p>
                <p class="balance">Total balance: <span id="senderBalance">—</span> XRP</p>
                <p class="muted">Required reserve: <span id="senderReserve">—</span> XRP</p>
                <p><strong>Available (before fee): <span id="senderAvailable">—</span> XRP</strong></p>
                <p class="muted" id="senderStatus">Not created</p>
                <asp:Button ID="CreateSender" runat="server" Text="Create sender wallet" OnClick="CreateSender_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
                <asp:Button ID="FundSender" runat="server" Text="Get Testnet XRP" OnClick="FundSender_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            </article>
            <article class="card">
                <h2>Receiver</h2>
                <p class="wallet-detail-label">Testnet address</p>
                <p class="address" id="receiverAddress">Not created</p>
                <p class="wallet-detail-label">Testnet seed</p>
                <p class="wallet-seed" id="receiverSeed">Not created</p>
                <p class="balance">Total balance: <span id="receiverBalance">—</span> XRP</p>
                <p class="muted">Required reserve: <span id="receiverReserve">—</span> XRP</p>
                <p><strong>Available (before fee): <span id="receiverAvailable">—</span> XRP</strong></p>
                <p class="muted" id="receiverStatus">Not created</p>
                <asp:Button ID="CreateReceiver" runat="server" Text="Create receiver wallet" OnClick="CreateReceiver_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            </article>
        </section>
        </div>
        <div id="implementation-wallets" class="example-panel" role="tabpanel" aria-labelledby="example-wallets-implementation" hidden>
        <section class="card">
            <h2>Wallets implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationWallets" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Funding uses the Testnet HTTP faucet. Handle input, RPC, HTTP and cancellation exceptions in your async handler. After a funding timeout, read the validated balance before retrying.</p>
        </section>
        </div>
        </div>
        <div id="panel-payments" class="tab-panel" role="tabpanel" aria-labelledby="tab-payments" hidden>
        <h2 class="section-title">Payments</h2>
        <div class="example-tabs" role="tablist" aria-label="Payments view" data-topic="payments">
            <button type="button" id="example-payments-test" class="tab-button" role="tab" aria-controls="test-payments" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-payments-implementation" class="tab-button" role="tab" aria-controls="implementation-payments" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-payments" class="example-panel" role="tabpanel" aria-labelledby="example-payments-test">
        <section class="card">
            <h2>Send XRP to the receiver</h2>
            <p class="muted">Estimated transaction fee: <strong><span id="feeEstimate">—</span> XRP</strong></p>
            <asp:UpdatePanel ID="PaymentUpdate" runat="server"><ContentTemplate>
            <asp:Label ID="AmountLabel" runat="server" AssociatedControlID="Amount" Text="Amount (XRP)" />
            <asp:TextBox ID="Amount" runat="server" inputmode="decimal" placeholder="1.000001" MaxLength="30" />
            <div class="tool-fields">
                <asp:Label runat="server" AssociatedControlID="PaymentDestination" Text="Destination (classic or X-address; blank uses the saved receiver)" />
                <asp:TextBox ID="PaymentDestination" runat="server" MaxLength="100" />
                <asp:Label runat="server" AssociatedControlID="PaymentTag" Text="Destination tag (optional; 0 is a tag)" />
                <asp:TextBox ID="PaymentTag" runat="server" inputmode="numeric" MaxLength="10" />
                <asp:Label runat="server" AssociatedControlID="PaymentMemo" Text="Memo (optional; up to 1000 UTF-8 bytes)" />
                <asp:TextBox ID="PaymentMemo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
            </div>
            <asp:Button ID="Quote" runat="server" CssClass="tool-action" Text="Estimate payment" OnClick="Quote_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <asp:Button ID="Send" runat="server" Text="Send XRP" OnClick="Send_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre class="result"><asp:Literal ID="QuoteResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
            <div class="table-wrap"><table><caption>Outgoing payments</caption><thead><tr><th>Amount (XRP)</th><th>Status</th><th>Fee (XRP)</th><th>Transaction hash</th><th>Detail</th><th>Destination tag</th><th>Memo</th><th>Destination</th><th>Submission / confirmation error</th></tr></thead><tbody id="transfers"></tbody></table></div>
        </section>
        <section class="card">
            <h2>Incoming payments</h2>
            <p id="monitoring" class="muted">Waiting for a receiver wallet.</p>
            <div class="table-wrap"><table><thead><tr><th>Received (XRP)</th><th>Transaction hash</th><th>Validated ledger</th></tr></thead><tbody id="incoming"></tbody></table></div>
        </section>
        </div>
        <div id="implementation-payments" class="example-panel" role="tabpanel" aria-labelledby="example-payments-implementation" hidden>
        <section class="card">
            <h2>Payments implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation. Persistence callbacks are supplied by your application.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationPayments" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Persist receipts before submission. Only Succeeded confirms a payment. Inspect Diagnostic and Error; recover Pending/Unknown from the saved receipt before sending again. Input and preflight errors throw exceptions.</p>
        </section>
        </div>
        </div>
        <div id="panel-addresses" class="tab-panel" role="tabpanel" aria-labelledby="tab-addresses" hidden>
        <h2 class="section-title">Addresses</h2>
        <div class="example-tabs" role="tablist" aria-label="Addresses view" data-topic="addresses">
            <button type="button" id="example-addresses-test" class="tab-button" role="tab" aria-controls="test-addresses" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-addresses-implementation" class="tab-button" role="tab" aria-controls="implementation-addresses" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-addresses" class="example-panel" role="tabpanel" aria-labelledby="example-addresses-test">
        <section class="card tool-fields">
            <h2>Validate a classic address</h2>
            <asp:UpdatePanel ID="ValidationUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="ValidationAddress" Text="Classic address" />
            <asp:TextBox ID="ValidationAddress" runat="server" MaxLength="100" />
            <asp:Button ID="ValidateClassic" runat="server" CssClass="tool-action" Text="Validate address" OnClick="ValidateClassic_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre class="result"><asp:Literal ID="ValidationResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        <section class="card tool-fields">
            <h2>X-address tools</h2>
            <asp:UpdatePanel ID="XAddressUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="XClassicAddress" Text="Classic address to encode" />
            <asp:TextBox ID="XClassicAddress" runat="server" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="XTag" Text="Destination tag to encode (optional; 0 is a tag)" />
            <asp:TextBox ID="XTag" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:CheckBox ID="XTestNetwork" runat="server" Checked="true" Text="Test network (Testnet / Devnet)" />
            <asp:Button ID="EncodeX" runat="server" CssClass="tool-action" Text="Encode X-address" OnClick="EncodeX_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <asp:Label runat="server" AssociatedControlID="XAddressInput" Text="X-address to decode or validate" />
            <asp:TextBox ID="XAddressInput" runat="server" MaxLength="100" />
            <asp:Button ID="DecodeX" runat="server" CssClass="tool-action" Text="Decode X-address" OnClick="DecodeX_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <asp:Button ID="ValidateX" runat="server" CssClass="tool-action" Text="Validate X-address" OnClick="ValidateX_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="xAddressResult" class="result"><asp:Literal ID="XResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-addresses" class="example-panel" role="tabpanel" aria-labelledby="example-addresses-implementation" hidden>
        <section class="card">
            <h2>Addresses implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0.</p>
                        <pre class="implementation-code"><code><asp:Literal ID="ImplementationAddresses" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Validation returns true/false. Encoding or decoding invalid input can throw an input exception.</p>
        </section>
        </div>
        </div>
        <div id="panel-history" class="tab-panel" role="tabpanel" aria-labelledby="tab-history" hidden>
        <h2 class="section-title">History</h2>
        <div class="example-tabs" role="tablist" aria-label="History view" data-topic="history">
            <button type="button" id="example-history-test" class="tab-button" role="tab" aria-controls="test-history" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-history-implementation" class="tab-button" role="tab" aria-controls="implementation-history" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-history" class="example-panel" role="tabpanel" aria-labelledby="example-history-test">
        <section class="card tool-fields">
            <h2>Query payment history</h2>
            <asp:UpdatePanel ID="HistoryUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="HistoryAddress" Text="History account (classic address; blank uses the saved receiver)" />
            <asp:TextBox ID="HistoryAddress" runat="server" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="HistoryFrom" Text="First ledger (optional)" />
            <asp:TextBox ID="HistoryFrom" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Label runat="server" AssociatedControlID="HistoryTo" Text="Last ledger (optional)" />
            <asp:TextBox ID="HistoryTo" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Label runat="server" AssociatedControlID="HistoryLimit" Text="Page size (1–400)" />
            <asp:TextBox ID="HistoryLimit" runat="server" inputmode="numeric" Text="20" MaxLength="3" />
            <asp:Button ID="SearchHistory" runat="server" CssClass="tool-action" Text="Search payment history" OnClick="SearchHistory_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <asp:Button ID="NextHistory" runat="server" CssClass="tool-action" Text="Next history page" OnClick="NextHistory_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <p class="muted">Next page continues the same search. Start a new search to change its account or ledger range. The server may adjust the requested page size. RequestedRangeAvailable reports whether the complete requested ledger range was available.</p>
            <pre id="historySummary" class="result"><asp:Literal ID="HistorySummary" runat="server" Mode="Encode" /></pre>
            <div class="table-wrap">
                <asp:GridView ID="HistoryResults" runat="server" AutoGenerateColumns="false" GridLines="None" EmptyDataText="No XRP payments in this page.">
                    <Columns>
                        <asp:BoundField DataField="LedgerIndex" HeaderText="Ledger" />
                        <asp:BoundField DataField="Hash" HeaderText="Transaction hash" ItemStyle-CssClass="hash" />
                        <asp:BoundField DataField="Sender" HeaderText="Sender" ItemStyle-CssClass="hash" />
                        <asp:BoundField DataField="Destination" HeaderText="Destination" ItemStyle-CssClass="hash" />
                        <asp:BoundField DataField="DestinationTag" HeaderText="Destination tag" NullDisplayText="—" />
                        <asp:BoundField DataField="AmountXrp" HeaderText="Requested (XRP)" DataFormatString="{0:0.######}" NullDisplayText="—" />
                        <asp:BoundField DataField="DeliveredAmountXrp" HeaderText="Delivered (XRP)" DataFormatString="{0:0.######}" NullDisplayText="—" />
                        <asp:BoundField DataField="FeeXrp" HeaderText="Fee (XRP)" DataFormatString="{0:0.######}" NullDisplayText="—" />
                        <asp:BoundField DataField="Status" HeaderText="Status" />
                        <asp:BoundField DataField="Validated" HeaderText="Validated" />
                    </Columns>
                </asp:GridView>
            </div>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-history" class="example-panel" role="tabpanel" aria-labelledby="example-history-implementation" hidden>
        <section class="card">
            <h2>History implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationHistory" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Handle input, RPC, connection and cancellation exceptions in the caller. Check RequestedRangeAvailable and keep the continuation token with its original account and search parameters.</p>
        </section>
        </div>
        </div>
        <div id="panel-transactions" class="tab-panel" role="tabpanel" aria-labelledby="tab-transactions" hidden>
        <h2 class="section-title">Transactions</h2>
        <div class="example-tabs" role="tablist" aria-label="Transactions view" data-topic="transactions">
            <button type="button" id="example-transactions-test" class="tab-button" role="tab" aria-controls="test-transactions" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-transactions-implementation" class="tab-button" role="tab" aria-controls="implementation-transactions" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-transactions" class="example-panel" role="tabpanel" aria-labelledby="example-transactions-test">
        <section class="card tool-fields">
            <h2>Look up a transaction</h2>
            <asp:UpdatePanel ID="TransactionUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="TransactionHash" Text="Transaction hash (64 hexadecimal characters)" />
            <asp:TextBox ID="TransactionHash" runat="server" MaxLength="64" />
            <asp:Button ID="QueryTransaction" runat="server" CssClass="tool-action" Text="Look up transaction" OnClick="QueryTransaction_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="transactionResult" class="result"><asp:Literal ID="TransactionResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-transactions" class="example-panel" role="tabpanel" aria-labelledby="example-transactions-implementation" hidden>
        <section class="card">
            <h2>Transactions implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationTransactions" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Inspect Found, Validated, Status and ResultCode. A connection error cannot establish transaction failure. Recover with a saved receipt when available.</p>
        </section>
        </div>
        </div>
        <div id="panel-account" class="tab-panel" role="tabpanel" aria-labelledby="tab-account" hidden>
        <h2 class="section-title">Account &amp; network</h2>
        <div class="example-tabs" role="tablist" aria-label="Account &amp; network view" data-topic="account">
            <button type="button" id="example-account-test" class="tab-button" role="tab" aria-controls="test-account" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-account-implementation" class="tab-button" role="tab" aria-controls="implementation-account" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-account" class="example-panel" role="tabpanel" aria-labelledby="example-account-test">
        <section class="card tool-fields">
            <h2>Account and network details</h2>
            <asp:UpdatePanel ID="AccountUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="AccountAddress" Text="Account details (classic address; blank uses the saved sender)" />
            <asp:TextBox ID="AccountAddress" runat="server" MaxLength="100" />
            <asp:Button ID="QueryAccount" runat="server" CssClass="tool-action" Text="Read account details" OnClick="QueryAccount_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="accountResult" class="result"><asp:Literal ID="AccountResult" runat="server" Mode="Encode" /></pre>
            <asp:Button ID="QueryNetwork" runat="server" CssClass="tool-action" Text="Read network details" OnClick="QueryNetwork_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="networkResult" class="result"><asp:Literal ID="NetworkResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-account" class="example-panel" role="tabpanel" aria-labelledby="example-account-implementation" hidden>
        <section class="card">
            <h2>Account &amp; network implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationAccount" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Handle input, RPC, connection and cancellation exceptions in your async handler. Sequence belongs to the selected account.</p>
        </section>
        </div>
        </div>
        <div id="panel-signing" class="tab-panel" role="tabpanel" aria-labelledby="tab-signing" hidden>
        <h2 class="section-title">Offline signing</h2>
        <div class="example-tabs" role="tablist" aria-label="Offline signing view" data-topic="signing">
            <button type="button" id="example-signing-test" class="tab-button" role="tab" aria-controls="test-signing" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-signing-implementation" class="tab-button" role="tab" aria-controls="implementation-signing" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-signing" class="example-panel" role="tabpanel" aria-labelledby="example-signing-test">
        <section class="card tool-fields">
            <h2>Sign a payment offline</h2>
            <p class="muted">Starts with the saved sender and the current payment values. Edit the account and seed to use another wallet. Signing uses only the values below and does not submit a transaction or make a network request.</p>
            <asp:UpdatePanel ID="SigningUpdate" runat="server"><ContentTemplate>
            <asp:HiddenField ID="SigningDefaultsReady" runat="server" Value="false" ClientIDMode="Static" />
            <asp:Label runat="server" AssociatedControlID="SigningAccount" Text="Signing account (classic address)" />
            <asp:TextBox ID="SigningAccount" runat="server" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="SigningSeed" Text="Signing seed" />
            <asp:TextBox ID="SigningSeed" runat="server" EnableViewState="false" autocomplete="off" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="SigningAmount" Text="Amount (XRP)" />
            <asp:TextBox ID="SigningAmount" runat="server" inputmode="decimal" MaxLength="30" />
            <asp:Label runat="server" AssociatedControlID="SigningDestination" Text="Destination (classic address)" />
            <asp:TextBox ID="SigningDestination" runat="server" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="SigningTag" Text="Destination tag (optional; 0 is a tag)" />
            <asp:TextBox ID="SigningTag" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Label runat="server" AssociatedControlID="SigningMemo" Text="Memo (optional; up to 1000 UTF-8 bytes)" />
            <asp:TextBox ID="SigningMemo" runat="server" TextMode="MultiLine" Rows="2" MaxLength="1000" />
            <asp:Label runat="server" AssociatedControlID="SigningSequence" Text="Signing sequence" />
            <p class="muted">Open the <strong>Account &amp; network</strong> tab, enter the signing account in Account details, select <strong>Read account details</strong>, then copy its <code>Sequence</code> value here.</p>
            <asp:TextBox ID="SigningSequence" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Label runat="server" AssociatedControlID="SigningFee" Text="Signing fee (integer drops; 1 XRP = 1000000 drops)" />
            <asp:TextBox ID="SigningFee" runat="server" inputmode="numeric" MaxLength="18" />
            <asp:Label runat="server" AssociatedControlID="SigningLastLedger" Text="Last ledger sequence (optional)" />
            <asp:TextBox ID="SigningLastLedger" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Button ID="SignOffline" runat="server" CssClass="tool-action" Text="Sign without submitting" OnClick="SignOffline_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="offlineResult" class="result"><asp:Literal ID="OfflineResult" runat="server" Mode="Encode" /></pre>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-signing" class="example-panel" role="tabpanel" aria-labelledby="example-signing-implementation" hidden>
        <section class="card">
            <h2>Offline signing implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0.</p>
                        <pre class="implementation-code"><code><asp:Literal ID="ImplementationSigning" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Signing returns a transaction blob. It does not submit or check the current sequence, balance or network fee. Handle invalid-input and signing exceptions in the caller.</p>
        </section>
        </div>
        </div>
        <div id="panel-delete" class="tab-panel" role="tabpanel" aria-labelledby="tab-delete" hidden>
        <h2 class="section-title">Delete account</h2>
        <div class="example-tabs" role="tablist" aria-label="Delete account view" data-topic="delete">
            <button type="button" id="example-delete-test" class="tab-button" role="tab" aria-controls="test-delete" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-delete-implementation" class="tab-button" role="tab" aria-controls="implementation-delete" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-delete" class="example-panel" role="tabpanel" aria-labelledby="example-delete-test">
        <section class="card tool-fields">
            <h2>Delete a Testnet account</h2>
            <p class="muted">Transfers the wallet's entire remaining balance, including releasable reserve, minus the deletion fee to a different, funded account. Check the conditions first. A validated failure can also charge the fee. Saved keys and history stay in your local JSON; the address can be funded again.</p>
            <asp:UpdatePanel ID="DeleteUpdate" runat="server"><ContentTemplate>
            <asp:Label runat="server" AssociatedControlID="DeleteSource" Text="Saved wallet to delete" />
            <asp:DropDownList ID="DeleteSource" runat="server">
                <asp:ListItem Value="" Text="Choose a wallet" /><asp:ListItem Value="sender" Text="Sender wallet" /><asp:ListItem Value="receiver" Text="Receiver wallet" />
            </asp:DropDownList>
            <asp:Label runat="server" AssociatedControlID="DeleteDestination" Text="Funded destination (classic or X-address)" />
            <asp:TextBox ID="DeleteDestination" runat="server" MaxLength="100" />
            <asp:Label runat="server" AssociatedControlID="DeleteTag" Text="Destination tag (optional; 0 is a tag)" />
            <asp:TextBox ID="DeleteTag" runat="server" inputmode="numeric" MaxLength="10" />
            <asp:Label runat="server" AssociatedControlID="DeleteMaxFee" Text="Maximum deletion fee (XRP)" />
            <asp:TextBox ID="DeleteMaxFee" runat="server" Text="0.2" inputmode="decimal" MaxLength="20" />
            <asp:Button ID="QuoteDelete" runat="server" CssClass="tool-action" Text="Check deletion conditions" OnClick="QuoteDelete_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <asp:Button ID="DeleteAccount" runat="server" CssClass="tool-action" Text="Delete account and transfer remaining XRP" OnClick="DeleteAccount_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <pre id="deleteQuoteResult" class="result"><asp:Literal ID="DeleteQuoteResult" runat="server" Mode="Encode" /></pre>
            <asp:Button ID="CheckDeletes" runat="server" CssClass="tool-action" Text="Check saved deletion receipts" OnClick="CheckDeletes_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            </ContentTemplate></asp:UpdatePanel>
            <div class="table-wrap"><table><thead><tr><th>Status</th><th>Returned (XRP)</th><th>Fee (XRP)</th><th>Deleted account</th><th>Destination</th><th>Destination tag</th><th>Transaction hash</th><th>Detail</th><th>Error</th></tr></thead><tbody id="accountDeletes"></tbody></table></div>
        </section>
        </div>
        <div id="implementation-delete" class="example-panel" role="tabpanel" aria-labelledby="example-delete-implementation" hidden>
        <section class="card">
            <h2>Delete account implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
            <p class="muted">Pass a shared XrplClient configured as shown in Testnet servers → Implementation. Persistence callbacks are supplied by your application.</p>
            <pre class="implementation-code"><code><asp:Literal ID="ImplementationDelete" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Only Succeeded confirms deletion and ReturnedAmountXrp. Persist and recover its receipt; inspect Diagnostic, Error and Transaction.FeeXrp. Failed transactions can still charge a fee.</p>
        </section>
        </div>
        </div>
        <div id="panel-testnet" class="tab-panel" role="tabpanel" aria-labelledby="tab-testnet" hidden>
        <h2 class="section-title">Testnet servers</h2>
        <div class="example-tabs" role="tablist" aria-label="Testnet servers view" data-topic="testnet">
            <button type="button" id="example-testnet-test" class="tab-button" role="tab" aria-controls="test-testnet" aria-selected="true" tabindex="0" data-view="test">Test</button>
            <button type="button" id="example-testnet-implementation" class="tab-button" role="tab" aria-controls="implementation-testnet" aria-selected="false" tabindex="-1" data-view="implementation">Implementation</button>
        </div>
        <div id="test-testnet" class="example-panel" role="tabpanel" aria-labelledby="example-testnet-test">
        <section class="card tool-fields">
            <h2>Testnet servers</h2>
            <p>Network: <strong>XRPL Testnet (network ID 1)</strong></p>
            <asp:UpdatePanel ID="TestnetUpdate" runat="server"><ContentTemplate>
            <p>Current server: <strong id="currentServerName"><asp:Literal ID="CurrentServerName" runat="server" Mode="Encode" /></strong></p>
            <p>JSON-RPC: <code id="currentRpcEndpoint"><asp:Literal ID="CurrentRpcEndpoint" runat="server" Mode="Encode" /></code></p>
            <p>WebSocket: <code id="currentSocketEndpoint"><asp:Literal ID="CurrentSocketEndpoint" runat="server" Mode="Encode" /></code></p>
            <p>Test XRP faucet: <code>https://faucet.altnet.rippletest.net/accounts</code></p>
            <asp:Label runat="server" AssociatedControlID="SelectedTestnetServer" Text="Testnet server" />
            <asp:DropDownList ID="SelectedTestnetServer" runat="server">
                <asp:ListItem Value="ripple" Text="Ripple Testnet" />
                <asp:ListItem Value="xrpl-labs" Text="XRPL Labs Testnet" />
                <asp:ListItem Value="honeycluster" Text="Honeycluster Testnet" />
            </asp:DropDownList>
            <asp:Button ID="ApplyTestnetServer" runat="server" CssClass="tool-action" Text="Apply server" OnClick="ApplyTestnetServer_Click" UseSubmitBehavior="false" OnClientClick="if (!beginAction(this)) return false;" />
            <p class="muted">The choice is saved locally. Wallets and history are kept. Finish checking any unresolved payment or deletion receipt before switching servers.</p>
            </ContentTemplate></asp:UpdatePanel>
        </section>
        </div>
        <div id="implementation-testnet" class="example-panel" role="tabpanel" aria-labelledby="example-testnet-implementation" hidden>
        <section class="card">
            <h2>Testnet servers implementation</h2>
            <p class="muted">Compatible C# for .NET Framework 4.7.2 and .NET 8 with Ripple.MultiSupport 1.1.0. Await these methods; Web Forms uses Async=&quot;true&quot; and RegisterAsyncTask, while .NET 8 calls them from an async handler.</p>
                        <pre class="implementation-code"><code><asp:Literal ID="ImplementationTestnet" runat="server" Mode="Encode" EnableViewState="false" /></code></pre>
            <p class="muted">Server names are ripple, xrpl-labs and honeycluster. Keep one client for the host lifetime and cancel monitoring before disposal. Receipts and history cursors belong to their original RPC endpoint.</p>
        </section>
        </div>
        </div>
    </main>
</form>
<script src="sample.js?v=bdcb875bd5a4"></script>
</body>
</html>
