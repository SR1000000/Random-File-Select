namespace Random_File_Select;

/// <summary>Random selection helpers, including tournament selection biased by a timestamp.</summary>
public static class RandomSelector
{
    /// <summary>Largest tournament size when the trackbar is at its minimum or maximum.</summary>
    public const int MaxTournamentK = 25;

    /// <summary>Rejections allowed per tournament draw (scaled by k, capped by count) before falling back to a scan of eligible indices.</summary>
    private const int RejectionBudgetPerDraw = 4;

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
        => PickTournamentIndex(count, timestampAt, static _ => true, value, min, max, random);

    /// <summary>Tournament pick restricted to indices where isEligible is true; ineligible draws are redrawn (timestampAt is only called for eligible indices), which keeps the bias identical to a tournament over only the eligible items.
    /// When the rejection budget (RejectionBudgetPerDraw * k, capped at count) runs out, it scans for eligible indices and runs the plain tournament over them.
    /// Returns -1 when count is 0 or no index is eligible.</summary>
    public static int PickTournamentIndex(
        int count,
        Func<int, DateTime> timestampAt,
        Func<int, bool> isEligible,
        int value,
        int min,
        int max,
        Random random)
    {
        if (count == 0)
            return -1;
        if (count == 1)
            return isEligible(0) ? 0 : -1;

        (int k, int direction) = GetTournament(value, min, max);
        int rejectionsLeft = Math.Min(RejectionBudgetPerDraw * k, count);

        int best = -1;
        DateTime bestTime = default;

        for (int draw = 0; draw < k; draw++)
        {
            int candidate = random.Next(count);
            while (!isEligible(candidate))
            {
                if (rejectionsLeft-- == 0)
                    return PickFromEligibleScan(count, timestampAt, isEligible, value, min, max, random);
                candidate = random.Next(count);
            }

            DateTime candidateTime = timestampAt(candidate);
            if (best < 0
                || (direction < 0 && candidateTime < bestTime)
                || (direction > 0 && candidateTime > bestTime))
            {
                best = candidate;
                bestTime = candidateTime;
            }
        }

        return best;
    }

    /// <summary>Collects eligible indices and runs the plain tournament over them, mapping the result back; returns -1 when none are eligible.</summary>
    private static int PickFromEligibleScan(
        int count,
        Func<int, DateTime> timestampAt,
        Func<int, bool> isEligible,
        int value,
        int min,
        int max,
        Random random)
    {
        var eligible = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (isEligible(i))
                eligible.Add(i);
        }

        int picked = PickTournamentIndex(eligible.Count, i => timestampAt(eligible[i]), value, min, max, random);
        return picked < 0 ? -1 : eligible[picked];
    }
}