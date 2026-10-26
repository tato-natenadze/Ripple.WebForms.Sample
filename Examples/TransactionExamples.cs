// Copyright 2026 tato-natenadze
// SPDX-License-Identifier: Apache-2.0

using System.Threading;
using System.Threading.Tasks;
using Ripple;

namespace RippleExamples
{
    public static class TransactionExamples
    {
        public static Task<TransactionInfo> FindAsync(
            XrplClient client, string hash, CancellationToken token)
        {
            // Inspect Found, Validated, Status and ResultCode.
            // A missing hash alone cannot prove that a payment expired.
            return client.GetTransactionAsync(hash, token);
        }

        public static Task<TransactionInfo> RecoverAsync(
            XrplClient client, PaymentReceipt receipt, CancellationToken token)
        {
            // A persisted receipt allows expiry checks using ledger-history proof.
            // Use its original RPC endpoint; never create a replacement while unknown.
            return client.GetTransactionAsync(receipt, token);
        }
    }
}
