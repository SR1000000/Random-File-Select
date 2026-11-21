namespace Random_File_Select;

/// <summary>Random selection helpers, including tournament selection biased by a timestamp.</summary>
public static class RandomSelector
{
    /// <summary>Largest tournament size when the trackbar is at its minimum or maximum.</summary>
    public const int MaxTournamentK = 50;

    /// <summary>Maps a trackbar value to tournament size k (1 at midpoint, up to <see cref="MaxTournamentK"/> at min/max) and a direction (-1 = prefer older, +1 = prefer newer, 0 = uniform).</summary>
    public static (int k, int Direction) GetTournament(int value, int min, int max)
    {
        int midpoint = (min + max) / 2;
        int delta = value - midpoint;
        int halfSpan = Math.Max(midpoint - min, max - midpoint);
        int k = halfSpan == 0
            ? 1
            : 1 + (MaxTournamentK - 1) * Math.Abs(delta) / halfSpan;
        int direction = delta switch
        {
            < 0 => -1,
            > 0 => 1,
            _ => 0
        };
        return (k, direction);
    }

    /// <summary>Draws k indices uniformly with replacement and returns the one whose timestamp is oldest or newest according to the direction; returns -1 when count is 0.
    /// Has mostly equivalent behavior to Rank-based Exponential Sampling, but without requiring any sorting of lists.</summary>
    public static int PickTournamentIndex(
        int count,
        Func<int, DateTime> timestampAt,
        int value,
        int min,
        int max,
        Random random)
    {
        if (count == 0)
            return -1;
        if (count == 1)
            return 0;

        (int k, int direction) = GetTournament(value, min, max);

        int best = random.Next(count);
        DateTime bestTime = timestampAt(best);

        for (int draw = 1; draw < k; draw++)
        {
            int candidate = random.Next(count);
            DateTime candidateTime = timestampAt(candidate);

            if (direction < 0 && candidateTime < bestTime)
            {
                best = candidate;
                bestTime = candidateTime;
            }
            else if (direction > 0 && candidateTime > bestTime)
            {
                best = candidate;
                bestTime = candidateTime;
            }
        }

        return best;
    }
}