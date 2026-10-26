// Copyright 2026 tato-natenadze
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class PaymentExamples
    {
        public static Task<decimal> ReadFeeAsync(XrplClient client, CancellationToken token)
        {
            return client.GetFeeAsync(token); // XRP; an estimate before submission
        }

        public static Task<PaymentQuote> EstimateAsync(XrplClient client,
            string sender, string destination, decimal amountXrp,
            uint? tag, string memo, CancellationToken token)
        {
            return client.GetPaymentQuoteAsync(sender, new XrpPaymentRequest {
                Destination = destination, AmountXrp = amountXrp,
                DestinationTag = tag, Memo = memo
            }, token); // Reserve, fee, activation requirements and remaining funds
        }

        public static async Task<PaymentResult> SendAsync(XrplClient client,
            string seed, string destination, decimal amountXrp, uint? tag, string memo,
            Func<PaymentReceipt, CancellationToken, Task> saveReceiptAsync,
            CancellationToken token)
        {
            if (saveReceiptAsync == null) throw new ArgumentNullException(nameof(saveReceiptAsync));
            var result = await client.SendXrpAsync(seed, new XrpPaymentRequest {
                Destination = destination, AmountXrp = amountXrp,
                DestinationTag = tag, Memo = memo,
                OnPreparedAsync = saveReceiptAsync // Save durably BEFORE broadcast.
            }, token);
            // Only Succeeded is a validated success. Inspect Status, Diagnostic,
            // Error and Transaction.FeeXrp. Pending/Unknown: recover; do not resend.
            return result;
        }

        public static Task<TransactionInfo> RecoverAsync(
            XrplClient client, PaymentReceipt receipt, CancellationToken token)
        {
            // Use the receipt's original RPC endpoint. This does not resubmit.
            return client.GetTransactionAsync(receipt, token);
        }

        public static Task WatchIncomingAsync(XrplClient client, string receiver,
            string checkpoint, uint? firstLedger,
            Func<TransactionInfo, CancellationToken, Task> saveIncomingAsync,
            Func<string, CancellationToken, Task> saveCheckpointAsync,
            CancellationToken token)
        {
            if (saveIncomingAsync == null) throw new ArgumentNullException(nameof(saveIncomingAsync));
            if (saveCheckpointAsync == null) throw new ArgumentNullException(nameof(saveCheckpointAsync));
            // saveIncomingAsync must atomically deduplicate by network/hash and
            // record DeliveredAmountXrp. Delivery can repeat after reconnect/restart.
            return client.WatchIncomingPaymentsAsync(receiver, saveIncomingAsync,
                new PaymentWatchOptions {
                    Checkpoint = checkpoint,
                    FromLedger = checkpoint == null ? firstLedger : null,
                    SaveCheckpointAsync = saveCheckpointAsync
                }, token); // A host-owned cancellation token stops monitoring.
        }
    }
}
