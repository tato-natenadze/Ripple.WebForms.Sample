# Ripple Web Forms Sample

A standalone English-language Web Forms sample for **XRPL Testnet**:
create sender/receiver wallets, request test XRP, send payments and observe
confirmed incoming payments automatically.

## Run

1. Use Windows, Visual Studio 2022 with the ASP.NET/web development workload,
   and the .NET Framework 4.7.2 targeting pack.
2. Open `Ripple.WebForms.Sample.sln` and restore NuGet packages.
3. Run with IIS Express (F5).
4. Create both wallets, select **Get Testnet XRP**, enter an XRP amount, then
   select **Send XRP**. Confirmed incoming payments appear automatically.

The library is restored from [NuGet.org](https://www.nuget.org/packages/Ripple.MultiSupport/1.1.0)
as `Ripple.MultiSupport` version `1.1.0` through `PackageReference`.
There is no project reference to the Ripple source.

## Library tools

Tools are grouped into eight tabs, including a separate **Delete account** tab.

- Estimate a payment: amount, fee, total debit, remaining funds and activation reserve.
- Send to the saved receiver or another classic/X-address, with an optional tag and UTF-8 memo.
- Validate classic addresses; encode, decode and validate X-addresses.
- Query payment history by ledger range, continue its pages, or look up a transaction hash.
- Read full account and network details. Submission/confirmation errors are saved with their SDK details.
- Sign a payment locally using the saved sender and explicit sequence, fee in drops and optional expiry ledger. This action does not submit it.
- Check account deletion conditions, its fee and remaining XRP transfer; delete a saved wallet and recover its saved receipt. The destination must already be funded. Keys and history remain in the local JSON.

The connection stays on Testnet. The X-address codec's network checkbox only
controls address encoding. Incoming monitoring watches the saved receiver.

## Local data

Each tester creates their own two wallets. `App_Data/testnet-state.json` saves
wallets, payment receipts and the incoming checkpoint across restarts.
Seeds are protected with Windows DPAPI for the Windows account running the app.
Keep your local data on that account and exclude `App_Data` when sharing the sample.
Balances are read from validated Testnet ledgers.
