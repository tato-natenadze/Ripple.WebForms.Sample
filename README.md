# Ripple Web Forms Sample

A standalone English-language Web Forms sample for **XRPL Testnet**:
create sender/receiver wallets, request test XRP, send payments and observe
confirmed incoming payments automatically.

**Testnet demo only.** The Wallets tab displays each test wallet's address and seed.
Use test-wallet seeds and test XRP only.

## Run

1. Use Windows, Visual Studio 2022 with the ASP.NET/web development workload,
   and the .NET Framework 4.7.2 targeting pack.
2. Open `Ripple.WebForms.Sample.sln` and restore NuGet packages.
3. Run with IIS Express (F5).
4. Create both wallets, select **Get Testnet XRP**, enter an XRP amount, then
   select **Send XRP**. Confirmed incoming payments appear automatically.
5. Open **Implementation** inside any section to view its C# example.

The library is restored from [NuGet.org](https://www.nuget.org/packages/Ripple.MultiSupport/1.1.0)
as `Ripple.MultiSupport` version `1.1.0` through `PackageReference`.
There is no project reference to the Ripple source.

## Library tools

Tools are grouped into nine tabs, including **Delete account** and **Testnet servers**.
Each has **Test** for the working sample and **Implementation** for reusable C#.
The displayed files in `Examples/` are compiled with the sample and support both
.NET Framework 4.7.2 and .NET 8.

- Estimate a payment: amount, fee, total debit, remaining funds and activation reserve.
- Send to the saved receiver or another classic/X-address, with an optional tag and UTF-8 memo.
- Validate classic addresses; encode, decode and validate X-addresses.
- Query payment history by ledger range, continue its pages, or look up a transaction hash.
- Read full account and network details. Submission/confirmation errors are saved with their SDK details.
- Sign a payment locally with editable account, seed and payment fields, plus an explicit sequence, fee in drops and optional expiry ledger. Defaults use the saved sender and current payment inputs. This action does not submit it.
- Check account deletion conditions, its fee and remaining XRP transfer; delete a saved wallet and recover its saved receipt. The destination must already be funded. Keys and history remain in the local JSON.

The **Testnet servers** tab shows the current RPC and WebSocket endpoints and lets
you choose Ripple, XRPL Labs or Honeycluster from the [official server list](https://xrpl.org/docs/tutorials/public-servers).
The selection is saved locally; the default is Ripple Testnet. Unresolved receipts
must finish their outcome checks before changing servers. Monitoring keeps a
separate checkpoint for each server and deduplicates incoming payments by hash.
The connection stays on Testnet. The X-address codec's network checkbox only
controls address encoding. Incoming monitoring watches the saved receiver.

## Local data

Each tester creates their own two wallets. `App_Data/testnet-state.json` saves
wallets, payment receipts, the selected Testnet server and per-server incoming
checkpoints across restarts.
Seeds are protected with Windows DPAPI for the Windows account running the app.
Keep your local data on that account and exclude `App_Data` when sharing the sample.
Balances are read from validated Testnet ledgers.

## License

Licensed under the [Apache License, Version 2.0](LICENSE).
See [NOTICE](NOTICE) for attribution. Dependencies retain their own licenses and notices.

## Disclaimer

Provided AS IS, without warranties of any kind. Use at your own risk. To the extent permitted by applicable law, the authors and contributors accept no liability for any loss, damage or claim arising from use of this sample.
