using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ripple;
using Ripple.LibraryClasses.CreateAddress;

namespace Ripple.WebForms.Sample
{
    public sealed class WalletState
    {
        public string Address { get; set; }
        public string ProtectedSeed { get; set; }
    }

    public sealed class TransferState
    {
        public string OperationId { get; set; }
        public decimal AmountXrp { get; set; }
        public PaymentReceipt Receipt { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Unknown;
        public bool FinishedWithoutSubmission { get; set; }
        public string Detail { get; set; }
        public decimal? FeeXrp { get; set; }
        public uint? DestinationTag { get; set; }
        public string Memo { get; set; }
        public string Destination { get; set; }
        public PaymentErrorState Error { get; set; }
    }

    public sealed class AccountDeleteState
    {
        public string OperationId { get; set; }
        public string Sender { get; set; }
        public AccountDeleteReceipt Receipt { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Unknown;
        public bool FinishedWithoutSubmission { get; set; }
        public string Detail { get; set; }
        public decimal? FeeXrp { get; set; }
        public decimal? ReturnedAmountXrp { get; set; }
        public PaymentErrorState Error { get; set; }
    }

    public sealed class PaymentErrorState
    {
        public string Kind { get; set; }
        public string Method { get; set; }
        public string RpcCode { get; set; }
        public int? HttpStatusCode { get; set; }
        public string Message { get; set; }

        public static PaymentErrorState From(PaymentError error)
        {
            if (error == null) return null;
            return new PaymentErrorState { Kind = error.Kind.ToString(), Method = error.Method,
                RpcCode = error.RpcCode, HttpStatusCode = error.HttpStatusCode, Message = error.Message };
        }
    }

    public sealed class IncomingState
    {
        public string Hash { get; set; }
        public decimal AmountXrp { get; set; }
        public uint LedgerIndex { get; set; }
    }

    public sealed class SampleState
    {
        public int Version { get; set; } = 1;
        public int NetworkId { get; set; } = 1;
        public string ServerId { get; set; } = "ripple";
        public Dictionary<string, string> IncomingCheckpoints { get; set; } = new Dictionary<string, string>();
        public WalletState Sender { get; set; }
        public WalletState Receiver { get; set; }
        public uint? FirstIncomingLedger { get; set; }
        public string Checkpoint { get; set; }
        public List<string> FaucetOperations { get; set; } = new List<string>();
        public List<TransferState> Transfers { get; set; } = new List<TransferState>();
        public List<AccountDeleteState> AccountDeletes { get; set; } = new List<AccountDeleteState>();
        public List<IncomingState> Incoming { get; set; } = new List<IncomingState>();
    }

    // One writer per data folder. Updates become visible only after the JSON file
    // has been flushed and atomically replaced; failed writes retain old state.
    public sealed class StateStore : IDisposable
    {
        private readonly object _gate = new object();
        private readonly string _path;
        private readonly FileStream _ownership;
        private SampleState _state;
        private bool _disposed;
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("Ripple.WebForms.Sample/Testnet");
        private static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            Culture = CultureInfo.InvariantCulture,
            FloatParseHandling = FloatParseHandling.Decimal,
            DateParseHandling = DateParseHandling.None,
            TypeNameHandling = TypeNameHandling.None,
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore
        };

        public StateStore(string directory)
        {
            Directory.CreateDirectory(directory);
            _path = Path.Combine(directory, "testnet-state.json");
            _ownership = new FileStream(Path.Combine(directory, "state.lock"), FileMode.OpenOrCreate,
                FileAccess.ReadWrite, FileShare.None);
            try
            {
                _state = File.Exists(_path) ? Read(File.ReadAllText(_path, Encoding.UTF8)) : new SampleState();
                Validate(_state);
            }
            catch { _ownership.Dispose(); throw; }
        }

        public SampleState Snapshot()
        {
            lock (_gate) { ThrowIfDisposed(); return Clone(_state); }
        }

        public void Update(Action<SampleState> update)
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                var next = Clone(_state);
                update(next);
                if (JsonConvert.SerializeObject(next, Settings) == JsonConvert.SerializeObject(_state, Settings)) return;
                Validate(next);
                string temporary = _path + "." + Guid.NewGuid().ToString("N") + ".tmp";
                try
                {
                    byte[] bytes = new UTF8Encoding(false, true).GetBytes(JsonConvert.SerializeObject(next, Formatting.Indented, Settings));
                    using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { stream.Write(bytes, 0, bytes.Length); stream.Flush(true); }
                    if (File.Exists(_path)) File.Replace(temporary, _path, null);
                    else File.Move(temporary, _path);
                    _state = next;
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
        }

        public static WalletState NewWallet()
        {
            var wallet = RippleClient.GenerateAddress();
            byte[] bytes = Encoding.UTF8.GetBytes(wallet.Seed);
            try
            {
                return new WalletState { Address = wallet.Address,
                    ProtectedSeed = Convert.ToBase64String(ProtectedData.Protect(bytes, Entropy, DataProtectionScope.CurrentUser)) };
            }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }

        public static string ReadSeed(WalletState wallet)
        {
            byte[] bytes = ProtectedData.Unprotect(Convert.FromBase64String(wallet.ProtectedSeed), Entropy, DataProtectionScope.CurrentUser);
            try { return Encoding.UTF8.GetString(bytes); }
            finally { Array.Clear(bytes, 0, bytes.Length); }
        }

        private static SampleState Read(string text)
        {
            var document = JObject.Parse(text, new JsonLoadSettings { DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error });
            if (document["Version"]?.Type != JTokenType.Integer || document["NetworkId"]?.Type != JTokenType.Integer ||
                document.Property("Sender") == null || document.Property("Receiver") == null ||
                !(document["Transfers"] is JArray) || !(document["Incoming"] is JArray))
                throw new InvalidDataException("Saved sample data is incomplete. The original file was retained.");
            return document.ToObject<SampleState>(JsonSerializer.Create(Settings));
        }

        private static SampleState Clone(SampleState state)
        { return JsonConvert.DeserializeObject<SampleState>(JsonConvert.SerializeObject(state, Settings), Settings); }

        private static void Validate(SampleState state)
        {
            if (state == null || state.Version != 1 || state.NetworkId != 1 || state.Transfers == null || state.Incoming == null || state.FaucetOperations == null || state.AccountDeletes == null)
                throw new InvalidDataException("Saved sample data is not valid Testnet data.");
            TestnetServer.Get(state.ServerId);
            if (state.IncomingCheckpoints == null) throw new InvalidDataException("Saved monitoring checkpoints are invalid.");
            foreach (var checkpoint in state.IncomingCheckpoints)
            {
                TestnetServer.Get(checkpoint.Key);
                if (checkpoint.Key == "ripple" || string.IsNullOrEmpty(checkpoint.Value))
                    throw new InvalidDataException("Saved monitoring checkpoints are invalid.");
            }
            var faucetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in state.FaucetOperations)
            {
                Guid operation;
                if (!Guid.TryParse(id, out operation) || !faucetIds.Add(id))
                    throw new InvalidDataException("Saved faucet request data is invalid.");
            }
            foreach (var wallet in new[] { state.Sender, state.Receiver })
            {
                if (wallet == null) continue;
                if (!RippleClient.ValidateAddress(wallet.Address) || string.IsNullOrEmpty(wallet.ProtectedSeed) ||
                    Seed.FromBase58(ReadSeed(wallet)).KeyPair().Id() != wallet.Address)
                    throw new InvalidDataException("Saved wallet does not match its protected seed.");
            }
            if (state.Receiver != null && (!state.FirstIncomingLedger.HasValue || state.FirstIncomingLedger == 0))
                throw new InvalidDataException("The receiver's first ledger is missing.");
            if (state.Sender != null && state.Receiver != null && state.Sender.Address == state.Receiver.Address)
                throw new InvalidDataException("Sender and receiver must be different wallets.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var transfer in state.Transfers)
            {
                Guid operation;
                if (transfer == null || !Guid.TryParse(transfer.OperationId, out operation) || !ids.Add(transfer.OperationId) ||
                    transfer.AmountXrp <= 0 || transfer.AmountXrp > 100000000000m || decimal.Truncate(transfer.AmountXrp * 1000000m) != transfer.AmountXrp * 1000000m)
                    throw new InvalidDataException("Saved transfer data is invalid.");
                if (transfer.Receipt != null && (state.Sender == null || transfer.Receipt.Sender != state.Sender.Address ||
                    !TestnetServer.Contains(transfer.Receipt.RpcEndpoint)))
                    throw new InvalidDataException("A saved receipt belongs to a different sender or network.");
                if (!Enum.IsDefined(typeof(TransactionStatus), transfer.Status)) throw new InvalidDataException("Invalid saved transaction status.");
                if ((transfer.Receipt == null && transfer.Status != TransactionStatus.Unknown) ||
                    (transfer.Receipt != null && transfer.FinishedWithoutSubmission) ||
                    (transfer.FeeXrp.HasValue && (transfer.FeeXrp < 0 || transfer.FeeXrp > 100000000000m ||
                        decimal.Truncate(transfer.FeeXrp.Value * 1000000m) != transfer.FeeXrp.Value * 1000000m)))
                    throw new InvalidDataException("Saved transfer status or fee is invalid.");
            }
            var deleteIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in state.AccountDeletes)
            {
                Guid operation;
                if (item == null || !Guid.TryParse(item.OperationId, out operation) || !deleteIds.Add(item.OperationId) ||
                    (item.Sender != state.Sender?.Address && item.Sender != state.Receiver?.Address) ||
                    !RippleClient.ValidateAddress(item.Sender) || !Enum.IsDefined(typeof(TransactionStatus), item.Status) ||
                    (item.Receipt == null && item.Status != TransactionStatus.Unknown) ||
                    (item.Receipt != null && (item.FinishedWithoutSubmission || item.Receipt.Sender != item.Sender ||
                        !TestnetServer.Contains(item.Receipt.RpcEndpoint))) ||
                    (item.ReturnedAmountXrp.HasValue && item.Status != TransactionStatus.Succeeded))
                    throw new InvalidDataException("Saved AccountDelete data is invalid.");
                foreach (decimal? amount in new[] { item.FeeXrp, item.ReturnedAmountXrp })
                    if (amount.HasValue && (amount < 0 || amount > 100000000000m ||
                        decimal.Truncate(amount.Value * 1000000m) != amount.Value * 1000000m))
                        throw new InvalidDataException("Saved AccountDelete amount is invalid.");
            }
            var hashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var incoming in state.Incoming)
            {
                if (incoming == null || incoming.Hash == null || incoming.Hash.Length != 64 || !hashes.Add(incoming.Hash) ||
                    !IsHex(incoming.Hash) || incoming.AmountXrp <= 0 || incoming.AmountXrp > 100000000000m ||
                    decimal.Truncate(incoming.AmountXrp * 1000000m) != incoming.AmountXrp * 1000000m || incoming.LedgerIndex == 0)
                    throw new InvalidDataException("Saved incoming payment data is invalid.");
            }
        }

        private static bool IsHex(string value)
        {
            foreach (char c in value)
                if (!((c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F'))) return false;
            return true;
        }

        private void ThrowIfDisposed() { if (_disposed) throw new ObjectDisposedException(nameof(StateStore)); }
        public void Dispose()
        {
            lock (_gate) { if (_disposed) return; _disposed = true; _ownership.Dispose(); }
        }
    }
}
