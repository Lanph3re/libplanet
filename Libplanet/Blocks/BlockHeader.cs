using System;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using Bencodex;
using Bencodex.Types;

namespace Libplanet.Blocks
{
    /// <summary>
    /// Block header containing information about <see cref="Block{T}"/>s except transactions.
    /// </summary>
    public readonly struct BlockHeader
    {
        internal const string TimestampFormat = "yyyy-MM-ddTHH:mm:ss.ffffffZ";

        private static readonly byte[] IndexKey = { 0x69 }; // 'i'

        private static readonly byte[] TimestampKey = { 0x74 }; // 't'

        private static readonly byte[] DifficultyKey = { 0x64 }; // 'd'

        private static readonly byte[] TotalDifficultyKey = { 0x54 }; // 'T'

        private static readonly byte[] NonceKey = { 0x6e }; // 'n'

        private static readonly byte[] MinerKey = { 0x6d }; // 'm'

        private static readonly byte[] PreviousHashKey = { 0x70 }; // 'p'

        private static readonly byte[] TxHashKey = { 0x78 }; // 'x'

        private static readonly byte[] HashKey = { 0x68 }; // 'h'

        private static readonly TimeSpan TimestampThreshold =
            TimeSpan.FromSeconds(15);

        /// <summary>
        /// Creates a <see cref="BlockHeader"/> instance.
        /// </summary>
        /// <param name="index">The height of the block.  Goes to the <see cref="Index"/>.
        /// </param>
        /// <param name="timestamp">The time this block is created.
        /// Goes to the <see cref="Timestamp"/>.</param>
        /// <param name="nonce">The nonce which satisfy the given <paramref name="difficulty"/>
        /// with any other field values.  Goes to the <see cref="Nonce"/>.</param>
        /// <param name="miner">An optional address refers to who mines this block.
        /// Goes to the <see cref="Miner"/>.</param>
        /// <param name="difficulty">The mining difficulty that <paramref name="nonce"/>
        /// has to satisfy.  Goes to the <see cref="Difficulty"/>.</param>
        /// <param name="totalDifficulty">The total mining difficulty until this block.
        /// See also <see cref="Difficulty"/>.</param>
        /// <param name="previousHash">The previous block's <see cref="Hash"/>.  If it's a genesis
        /// block (i.e., <paramref name="index"/> is 0) this should be <c>null</c>.
        /// Goes to the <see cref="PreviousHash"/>.</param>
        /// <param name="txHash">The result of hashing the transactions the block has.
        /// Goes to the <see cref="TxHash"/>.</param>
        /// <param name="hash">The hash of the <see cref="Block{T}"/>.
        /// Goes to the <see cref="Hash"/>.</param>
        public BlockHeader(
            long index,
            string timestamp,
            ImmutableArray<byte> nonce,
            ImmutableArray<byte> miner,
            long difficulty,
            BigInteger totalDifficulty,
            ImmutableArray<byte> previousHash,
            ImmutableArray<byte> txHash,
            ImmutableArray<byte> hash)
        {
            Index = index;
            Timestamp = timestamp;
            Nonce = nonce;
            Miner = miner;
            Difficulty = difficulty;
            TotalDifficulty = totalDifficulty;
            PreviousHash = previousHash;
            TxHash = txHash;
            Hash = hash;
        }

        public BlockHeader(Bencodex.Types.Dictionary dict)
        {
            Index = dict.GetValue<Integer>(IndexKey);
            Timestamp = dict.GetValue<Text>(TimestampKey);
            Difficulty = dict.GetValue<Integer>(DifficultyKey);
            TotalDifficulty = dict.GetValue<Integer>(TotalDifficultyKey);
            Nonce = dict.GetValue<Binary>(NonceKey).ToImmutableArray();

            Miner = dict.ContainsKey((IKey)(Binary)MinerKey)
                ? dict.GetValue<Binary>(MinerKey).ToImmutableArray()
                : ImmutableArray<byte>.Empty;

            PreviousHash = dict.ContainsKey((IKey)(Binary)PreviousHashKey)
                ? dict.GetValue<Binary>(PreviousHashKey).ToImmutableArray()
                : ImmutableArray<byte>.Empty;

            TxHash = dict.ContainsKey((IKey)(Binary)TxHashKey)
                ? dict.GetValue<Binary>(TxHashKey).ToImmutableArray()
                : ImmutableArray<byte>.Empty;

            Hash = dict.ContainsKey((IKey)(Binary)HashKey)
                ? dict.GetValue<Binary>(HashKey).ToImmutableArray()
                : ImmutableArray<byte>.Empty;
        }

        public long Index { get; }

        public string Timestamp { get; }

        public ImmutableArray<byte> Nonce { get; }

        public ImmutableArray<byte> Miner { get; }

        public long Difficulty { get; }

        public BigInteger TotalDifficulty { get; }

        public ImmutableArray<byte> PreviousHash { get; }

        public ImmutableArray<byte> TxHash { get; }

        public ImmutableArray<byte> Hash { get; }

        /// <summary>
        /// Gets <see cref="BlockHeader"/> instance from serialized <paramref name="bytes"/>.
        /// </summary>
        /// <param name="bytes">Serialized <see cref="BlockHeader"/>.</param>
        /// <returns>Deserialized <see cref="BlockHeader"/>.</returns>
        /// <exception cref="DecodingException">Thrown when decoded value is not
        /// <see cref="Bencodex.Types.Dictionary"/> type.</exception>
        public static BlockHeader Deserialize(byte[] bytes)
        {
            IValue value = new Codec().Decode(bytes);
            if (!(value is Bencodex.Types.Dictionary dict))
            {
                throw new DecodingException(
                    $"Expected {typeof(Bencodex.Types.Dictionary)} but " +
                    $"{value.GetType()}");
            }

            return new BlockHeader(dict);
        }

        /// <summary>
        /// Gets serialized byte array of the <see cref="BlockHeader"/>.
        /// </summary>
        /// <returns>Serialized byte array of <see cref="BlockHeader"/>.</returns>
        public byte[] Serialize()
        {
            return new Codec().Encode(ToBencodex());
        }

        /// <summary>
        /// Gets <see cref="Bencodex.Types.Dictionary"/> representation of
        /// <see cref="BlockHeader"/>.
        /// </summary>
        /// <returns><see cref="Bencodex.Types.Dictionary"/> representation of
        /// <see cref="BlockHeader"/>.</returns>
        public Bencodex.Types.Dictionary ToBencodex()
        {
            var dict = Bencodex.Types.Dictionary.Empty
                .Add(IndexKey, Index)
                .Add(TimestampKey, Timestamp)
                .Add(DifficultyKey, Difficulty)
                .Add(TotalDifficultyKey, (IValue)(Bencodex.Types.Integer)TotalDifficulty)
                .Add(NonceKey, Nonce.ToArray())
                .Add(HashKey, Hash.ToArray());

            if (Miner.Any())
            {
                dict = dict.Add(MinerKey, Miner.ToArray());
            }

            if (PreviousHash.Any())
            {
                dict = dict.Add(PreviousHashKey, PreviousHash.ToArray());
            }

            if (TxHash.Any())
            {
                dict = dict.Add(TxHashKey, TxHash.ToArray());
            }

            return dict;
        }

        internal void Validate(DateTimeOffset currentTime)
        {
            DateTimeOffset ts = DateTimeOffset.ParseExact(
                Timestamp,
                TimestampFormat,
                CultureInfo.InvariantCulture
            );

            if (currentTime + TimestampThreshold < ts)
            {
                throw new InvalidBlockTimestampException(
                    $"The block #{Index}'s timestamp ({Timestamp}) is " +
                    $"later than now ({currentTime}, " +
                    $"threshold: {TimestampThreshold})."
                );
            }

            if (Index < 0)
            {
                throw new InvalidBlockIndexException(
                    $"index must be 0 or more, but its index is {Index}."
                );
            }

            if (Difficulty > TotalDifficulty)
            {
                var msg = $"A Block.Difficulty ({Difficulty}) must be less than" +
                          $"its TotalDifficulty ({TotalDifficulty}).";
                throw new InvalidBlockTotalDifficultyException(
                    Difficulty,
                    TotalDifficulty,
                    msg
                );
            }

            if (Index == 0)
            {
                if (Difficulty != 0)
                {
                    throw new InvalidBlockDifficultyException(
                        "difficulty must be 0 for the genesis block, " +
                        $"but its difficulty is {Difficulty}."
                    );
                }

                if (TotalDifficulty != 0)
                {
                    var msg = "Total difficulty must be 0 for the genesis block, " +
                              $"but its total difficulty is {TotalDifficulty}.";
                    throw new InvalidBlockTotalDifficultyException(
                        Difficulty,
                        TotalDifficulty,
                        msg
                    );
                }

                if (!PreviousHash.IsEmpty)
                {
                    throw new InvalidBlockPreviousHashException(
                        "previous hash must be empty for the genesis block."
                    );
                }
            }
            else
            {
                if (Difficulty < 1)
                {
                    throw new InvalidBlockDifficultyException(
                        "difficulty must be more than 0 (except of " +
                        "the genesis block), but its difficulty is " +
                        $"{Difficulty}."
                    );
                }

                if (PreviousHash.IsEmpty)
                {
                    throw new InvalidBlockPreviousHashException(
                        "previous hash must be present except of " +
                        "the genesis block."
                    );
                }
            }

            if (!new HashDigest<SHA256>(Hash.ToArray()).Satisfies(Difficulty))
            {
                throw new InvalidBlockNonceException(
                    $"hash ({Hash}) with the nonce ({Nonce}) does not " +
                    $"satisfy its difficulty level {Difficulty}."
                );
            }
        }
    }
}
