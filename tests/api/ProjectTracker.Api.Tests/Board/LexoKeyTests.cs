using ProjectTracker.Api.Modules.Board.Ordering;
using Xunit;

namespace ProjectTracker.Api.Tests.Board;

public class LexoKeyTests
{
    // Given a null left neighbour and a null right neighbour (first item in an empty scope),
    // When a midpoint key is generated,
    // Then a valid non-empty key is returned.
    [Fact]
    public void Midpoint_returns_non_empty_key_for_empty_scope()
    {
        var result = LexoKey.Midpoint(left: null, right: null);

        Assert.False(result.NeedsRebalance);
        Assert.False(string.IsNullOrEmpty(result.Key));
    }

    // Given left key "a" and right key "b",
    // When a midpoint key is generated,
    // Then the result sorts strictly between "a" and "b" under ordinal comparison.
    [Fact]
    public void Midpoint_sorts_strictly_between_two_neighbours()
    {
        var result = LexoKey.Midpoint(left: "a", right: "b");

        Assert.False(result.NeedsRebalance);
        Assert.True(string.CompareOrdinal("a", result.Key) < 0);
        Assert.True(string.CompareOrdinal(result.Key, "b") < 0);
    }

    // Given left key "a" and null right neighbour (inserting at the end),
    // When a midpoint key is generated,
    // Then the result sorts strictly after "a".
    [Fact]
    public void Midpoint_sorts_after_left_when_appending_at_end()
    {
        var result = LexoKey.Midpoint(left: "a", right: null);

        Assert.False(result.NeedsRebalance);
        Assert.True(string.CompareOrdinal("a", result.Key) < 0);
    }

    // Given repeated insertion at the same position such that the generated key would exceed
    // 48 characters, When the next key is generated, Then the rebalance-needed signal is raised
    // instead of returning an ever-longer key.
    [Fact]
    public void Midpoint_signals_rebalance_when_key_would_exceed_48_characters()
    {
        // Simulate worst-case adversarial repeated insertion at the same position: each
        // insertion between the same left bound and a shrinking right bound grows the key by
        // roughly one character. Drive it past the 48-character threshold.
        string? left = "a";
        string right = "b";
        string? lastKey = null;
        var rebalanceSignalled = false;

        for (var i = 0; i < 60; i++)
        {
            var result = LexoKey.Midpoint(left, right);
            if (result.NeedsRebalance)
            {
                rebalanceSignalled = true;
                break;
            }
            lastKey = result.Key;
            right = lastKey; // insert immediately before the previous insertion, every time
        }

        Assert.True(rebalanceSignalled, "expected rebalance to be signalled before 60 adversarial insertions");
        if (lastKey is not null)
        {
            Assert.True(lastKey.Length <= 48, "key length must never exceed the 48-character rebalance threshold");
        }
    }

    // Given two adjacent keys with no representable midpoint,
    // When a midpoint key is generated,
    // Then the rebalance-needed signal is raised.
    [Fact]
    public void Midpoint_signals_rebalance_when_no_representable_value_between_neighbours()
    {
        // "a" and its minimal successor in the base-62 alphabet have no representable midpoint
        // at that length; the implementation must detect this rather than produce a duplicate
        // or malformed key.
        var minimalSuccessor = "a" + LexoKey.AlphabetMinChar;

        var result = LexoKey.Midpoint(left: "a", right: minimalSuccessor);

        Assert.True(result.NeedsRebalance);
    }

    // Given a scope of 5 items with arbitrary existing keys,
    // When the scope is rebalanced,
    // Then the 5 items receive new keys that preserve their original relative order and no two
    // items share a key.
    [Fact]
    public void Rebalance_preserves_relative_order_and_produces_unique_keys()
    {
        var originalOrder = new[] { "zzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzzz", "a", "m", "mzz", "n" };
        // Sort by current key to establish the scope's existing logical order before rebalance.
        var sorted = originalOrder.OrderBy(k => k, StringComparer.Ordinal).ToArray();

        var rebalanced = LexoKey.Rebalance(sorted.Length);

        Assert.Equal(sorted.Length, rebalanced.Count);
        Assert.Equal(rebalanced.Distinct(StringComparer.Ordinal).Count(), rebalanced.Count);

        for (var i = 1; i < rebalanced.Count; i++)
        {
            Assert.True(string.CompareOrdinal(rebalanced[i - 1], rebalanced[i]) < 0,
                "rebalanced keys must be in strictly ascending order, preserving original relative positions");
        }
    }
}
