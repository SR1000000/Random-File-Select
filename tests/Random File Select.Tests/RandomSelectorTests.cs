using System.Collections.Immutable;

namespace Random_File_Select.Tests;

public class RandomSelectorTests
{
    [Theory]
    [InlineData(5, 0, 10, 1, 0)]
    [InlineData(0, 0, 10, 50, -1)]
    [InlineData(10, 0, 10, 50, 1)]
    [InlineData(2, 0, 10, 30, -1)]
    public void GetTournament_mapsValueToKAndDirection(int value, int min, int max, int expectedK, int expectedDirection)
    {
        (int k, int direction) = RandomSelector.GetTournament(value, min, max);
        Assert.Equal(expectedK, k);
        Assert.Equal(expectedDirection, direction);
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
}