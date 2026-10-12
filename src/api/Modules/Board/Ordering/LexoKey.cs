using System.Numerics;

namespace ProjectTracker.Api.Modules.Board.Ordering;

public static class LexoKey
{
    public const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
    public const char AlphabetMinChar = '0';
    public const int RebalanceThreshold = 48;

    private static readonly IReadOnlyDictionary<char, int> AlphabetIndex = Alphabet
        .Select((ch, index) => (ch, index))
        .ToDictionary(x => x.ch, x => x.index);

    public sealed record Result(string Key, bool NeedsRebalance);

    public static Result Midpoint(string? left, string? right)
    {
        if (left is null && right is null)
        {
            return new Result(MidChar().ToString(), false);
        }

        if (left is null)
        {
            return TryBefore(right!) ?? new Result(string.Empty, true);
        }

        if (right is null)
        {
            var key = left + MidChar();
            return NeedsRebalance(key) ? new Result(string.Empty, true) : new Result(key, false);
        }

        if (string.CompareOrdinal(left, right) >= 0)
        {
            return new Result(string.Empty, true);
        }

        var prefix = string.Empty;
        var index = 0;

        while (true)
        {
            var leftEnded = index >= left.Length;
            var rightEnded = index >= right.Length;

            if (leftEnded && rightEnded)
            {
                var key = prefix + MidChar();
                return NeedsRebalance(key) ? new Result(string.Empty, true) : new Result(key, false);
            }

            if (leftEnded)
            {
                var currentRightRank = AlphabetIndex[right[index]];
                if (currentRightRank == 0)
                {
                    return new Result(string.Empty, true);
                }

                var key = prefix + Alphabet[currentRightRank - 1];
                return NeedsRebalance(key) ? new Result(string.Empty, true) : new Result(key, false);
            }

            if (rightEnded)
            {
                var key = prefix + left[index] + MidChar();
                return NeedsRebalance(key) ? new Result(string.Empty, true) : new Result(key, false);
            }

            var leftRank = AlphabetIndex[left[index]];
            var rightRank = AlphabetIndex[right[index]];

            if (leftRank == rightRank)
            {
                prefix += left[index];
                index++;
                continue;
            }

            var gap = rightRank - leftRank;
            if (gap > 1)
            {
                var key = prefix + Alphabet[leftRank + (gap / 2)];
                return NeedsRebalance(key) ? new Result(string.Empty, true) : new Result(key, false);
            }

            // No representable midpoint at this depth; extend the prefix and try again.
            prefix += left[index];
            index++;
        }
    }

    public static IReadOnlyList<string> Rebalance(int count)
    {
        if (count <= 0)
        {
            return Array.Empty<string>();
        }

        var width = 1;
        while (BigInteger.Pow(Alphabet.Length, width) <= count)
        {
            width++;
        }

        return Enumerable.Range(1, count)
            .Select(value => EncodeBase62(value, width))
            .ToArray();
    }

    public static bool NeedsRebalance(string key) => key.Length > RebalanceThreshold;

    private static Result? TryBefore(string right)
    {
        if (right.Length == 0)
        {
            return null;
        }

        var rank = AlphabetIndex[right[0]];
        if (rank == 0)
        {
            return null;
        }

        return new Result(Alphabet[rank - 1].ToString(), false);
    }

    private static char MidChar() => Alphabet[Alphabet.Length / 2];

    private static string EncodeBase62(int value, int width)
    {
        var buffer = new char[width];
        var current = value;

        for (var i = width - 1; i >= 0; i--)
        {
            buffer[i] = Alphabet[current % Alphabet.Length];
            current /= Alphabet.Length;
        }

        return new string(buffer);
    }
}
