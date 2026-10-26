// Copyright 2026 tato-natenadze
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class DeleteExamples
    {
        public static Task<AccountDeleteQuote> CheckAsync(XrplClient client,
            string sender, string destination, uint? tag, decimal maxFeeXrp,
            CancellationToken token)
        {
            // Show CanDelete, BlockingReasons, EstimatedFeeXrp and EstimatedReturnedXrp.
            return client.GetAccountDeleteQuoteAsync(sender, new AccountDeleteRequest {
                Destination = destination, DestinationTag = tag, MaxFeeXrp = maxFeeXrp
            }, token);
        }

        public static async Task<AccountDeleteResult> DeleteAsync(XrplClient client,
            string seed, string destination, uint? tag, decimal maxFeeXrp,
            Func<AccountDeleteReceipt, CancellationToken, Task> saveReceiptAsync,
            CancellationToken token)
        {
            if (saveReceiptAsync == null) throw new ArgumentNullException(nameof(saveReceiptAsync));
            var result = await client.DeleteAccountAsync(seed, new AccountDeleteRequest {
                Destination = destination, DestinationTag = tag, MaxFeeXrp = maxFeeXrp,
                OnPreparedAsync = saveReceiptAsync // Save durably BEFORE broadcast.
            }, token); // The library checks conditions again before signing.
            // Only Succeeded confirms deletion and ReturnedAmountXrp.
            // Inspect Transaction.FeeXrp, Diagnostic and Error; failures may charge a fee.
            // Pending/Unknown: recover the saved receipt before any replacement.
            return result;
        }

        public static Task<AccountDeleteResult> RecoverAsync(XrplClient client,
            AccountDeleteReceipt receipt, CancellationToken token)
        {
            return client.GetAccountDeleteAsync(receipt, token); // Same RPC endpoint; no resubmission.
        }
    }
}
