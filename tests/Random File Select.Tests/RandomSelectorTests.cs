using System.Collections.Immutable;

namespace Random_File_Select.Tests;

public class RandomSelectorTests
{
    [Theory]
    [InlineData(5, 0, 10, 1, 0)]
    [InlineData(0, 0, 10, RandomSelector.MaxTournamentK, -1)]
    [InlineData(10, 0, 10, RandomSelector.MaxTournamentK, 1)]
    public void GetTournament_mapsValueToKAndDirection(int value, int min, int max, int expectedK, int expectedDirection)
    {
        (int k, int direction) = RandomSelector.GetTournament(value, min, max);
        Assert.Equal(expectedK, k);
        Assert.Equal(expectedDirection, direction);
    }

    [Fact]
    public void GetTournament_intermediateValues_respectBoundsAndMonotonicK()
    {
        const int min = 0, max = 10;
        int midpoint = (min + max) / 2;
        int previousKAtDistance = -1;

        for (int distance = 0; distance <= midpoint - min; distance++)
        {
            int valueBelow = midpoint - distance;
            (int kBelow, int dirBelow) = RandomSelector.GetTournament(valueBelow, min, max);
            Assert.InRange(kBelow, 1, RandomSelector.MaxTournamentK);
            if (distance == 0)
            {
                Assert.Equal(0, dirBelow);
                Assert.Equal(1, kBelow);
            }
            else
            {
                Assert.Equal(-1, dirBelow);
                Assert.True(kBelow >= previousKAtDistance, $"k should not decrease as distance from midpoint grows (distance={distance}, k={kBelow}, previous={previousKAtDistance})");
            }

            previousKAtDistance = kBelow;

            int valueAbove = midpoint + distance;
            (int kAbove, int dirAbove) = RandomSelector.GetTournament(valueAbove, min, max);
            Assert.InRange(kAbove, 1, RandomSelector.MaxTournamentK);
            if (distance == 0)
                continue;

            Assert.Equal(1, dirAbove);
            Assert.Equal(kBelow, kAbove);
        }
    }

    [Fact]
    public void PickTournamentIndex_returnsMinusOneWhenEmpty()
    {
        int index = RandomSelector.PickTournamentIndex(0, _ => DateTime.MinValue, 5, 0, 10, new Random(0));
        Assert.Equal(-1, index);
    }

    [Fact]
    public void PickTournamentIndex_returnsZeroWhenOneItem()
    {
        int index = RandomSelector.PickTournamentIndex(1, _ => DateTime.MinValue, 5, 0, 10, new Random(0));
        Assert.Equal(0, index);
    }

    [Fact]
    public void PickTournamentIndex_biasesTowardOlderWhenValueIsZero()
    {
        const int count = 20;
        var timestamps = Enumerable.Range(0, count)
            .Select(i => new DateTime(2000, 1, 1).AddDays(i))
            .ToArray();
        var random = new Random(42);
        double sum = 0;
        const int trials = 5000;
        for (int t = 0; t < trials; t++)
        {
            int index = RandomSelector.PickTournamentIndex(
                count,
                i => timestamps[i],
                value: 0,
                min: 0,
                max: 10,
                random);
            sum += index;
        }

        double mean = sum / trials;
        double uniformMean = (count - 1) / 2.0;
        Assert.True(mean < uniformMean - 2, $"Expected mean rank below {uniformMean - 2}, got {mean}");
    }

    [Fact]
    public void PickTournamentIndex_biasesTowardNewerWhenValueIsTen()
    {
        const int count = 20;
        var timestamps = Enumerable.Range(0, count)
            .Select(i => new DateTime(2000, 1, 1).AddDays(i))
            .ToArray();
        var random = new Random(42);
        double sum = 0;
        const int trials = 5000;
        for (int t = 0; t < trials; t++)
        {
            int index = RandomSelector.PickTournamentIndex(
                count,
                i => timestamps[i],
                value: 10,
                min: 0,
                max: 10,
                random);
            sum += index;
        }

        double mean = sum / trials;
        double uniformMean = (count - 1) / 2.0;
        Assert.True(mean > uniformMean + 2, $"Expected mean rank above {uniformMean + 2}, got {mean}");
    }

    [Fact]
    public void PickTournamentIndexEligible_returnsMinusOneWhenEmpty()
    {
        Assert.Equal(-1, RandomSelector.PickTournamentIndex(0, _ => DateTime.MinValue, _ => true, 5, 0, 10, new Random(0)));
    }

    [Theory]
    [InlineData(true, 0)]
    [InlineData(false, -1)]
    public void PickTournamentIndexEligible_singleItemHonorsEligibility(bool eligible, int expected)
    {
        Assert.Equal(expected, RandomSelector.PickTournamentIndex(1, _ => DateTime.MinValue, _ => eligible, 5, 0, 10, new Random(0)));
    }

    [Fact]
    public void PickTournamentIndexEligible_allIneligible_returnsMinusOne()
    {
        Assert.Equal(-1, RandomSelector.PickTournamentIndex(30, _ => DateTime.MinValue, _ => false, 0, 0, 10, new Random(0)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(5)]
    [InlineData(10)]
    public void PickTournamentIndexEligible_neverReturnsIneligible(int value)
    {
        var random = new Random(7);
        for (int t = 0; t < 2000; t++)
        {
            int index = RandomSelector.PickTournamentIndex(
                40, i => new DateTime(2000, 1, 1).AddDays(i), i => i % 2 == 1, value, 0, 10, random);
            Assert.True(index % 2 == 1, $"Got ineligible index {index}");
        }
    }

    [Fact]
    public void PickTournamentIndexEligible_singleEligibleAmongMany_alwaysReturnsIt()
    {
        var random = new Random(3);
        for (int t = 0; t < 500; t++)
        {
            int index = RandomSelector.PickTournamentIndex(
                100, i => new DateTime(2000, 1, 1).AddDays(i), i => i == 63, 0, 0, 10, random);
            Assert.Equal(63, index);
        }
    }

    [Fact]
    public void PickTournamentIndexEligible_multiEligibleFallback_keepsBiasAndOnlyTimestampsEligible()
    {
        int[] eligible = [10, 50, 90, 130, 170];
        var eligibleSet = eligible.ToHashSet();
        bool touchedIneligible = false;
        DateTime TimestampAt(int i)
        {
            if (!eligibleSet.Contains(i))
                touchedIneligible = true;
            return new DateTime(2000, 1, 1).AddDays(i);
        }

        var random = new Random(11);
        const int trials = 300;
        int oldest = 0, newest = 0;
        for (int t = 0; t < trials; t++)
        {
            int low = RandomSelector.PickTournamentIndex(200, TimestampAt, eligibleSet.Contains, 0, 0, 10, random);
            int high = RandomSelector.PickTournamentIndex(200, TimestampAt, eligibleSet.Contains, 10, 0, 10, random);
            Assert.Contains(low, eligibleSet);
            Assert.Contains(high, eligibleSet);
            if (low == 10) oldest++;
            if (high == 170) newest++;
        }

        Assert.False(touchedIneligible);
        double expected = 1 - Math.Pow((eligible.Length - 1.0) / eligible.Length, RandomSelector.MaxTournamentK);
        double threshold = (expected - 0.1) * trials;
        Assert.True(oldest > threshold, $"Expected mostly oldest, got {oldest}/{trials} (threshold {threshold})");
        Assert.True(newest > threshold, $"Expected mostly newest, got {newest}/{trials} (threshold {threshold})");
    }
}