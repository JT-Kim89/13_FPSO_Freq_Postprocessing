namespace FpsoFrequencyDomain;

public static class ExtremeStatistics
{
    public static ShortTermExtremeResult ShortTerm(ResponseStatistics statistics, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be positive.");
        }

        if (statistics.M0 <= 0.0 || double.IsInfinity(statistics.MeanZeroUpcrossingPeriod))
        {
            return new ShortTermExtremeResult(
                statistics.Name,
                duration,
                0.0,
                0.0,
                0.0,
                0.0);
        }

        var cycles = Math.Max(duration.TotalSeconds / statistics.MeanZeroUpcrossingPeriod, 1.0);
        var sigma = Math.Sqrt(statistics.M0);
        var mpm = cycles > 1.0 ? sigma * Math.Sqrt(2.0 * Math.Log(cycles)) : sigma;
        var expected = ExpectedMaximumRayleighApproximation(sigma, cycles);

        return new ShortTermExtremeResult(
            statistics.Name,
            duration,
            cycles,
            sigma,
            mpm,
            expected);
    }

    public static double PeakExceedanceProbability(ResponseStatistics statistics, double level)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        if (level < 0.0)
        {
            return 1.0;
        }

        return statistics.M0 > 0.0
            ? Math.Exp(-(level * level) / (2.0 * statistics.M0))
            : 0.0;
    }

    public static double ShortTermMaximumExceedanceProbability(
        ResponseStatistics statistics,
        TimeSpan duration,
        double level)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        if (level < 0.0)
        {
            return 1.0;
        }

        if (statistics.M0 <= 0.0 || double.IsInfinity(statistics.MeanZeroUpcrossingPeriod))
        {
            return 0.0;
        }

        var cycles = Math.Max(duration.TotalSeconds / statistics.MeanZeroUpcrossingPeriod, 1.0);
        var peakExceedance = PeakExceedanceProbability(statistics, level);
        var nonExceedance = Math.Pow(Math.Max(0.0, 1.0 - peakExceedance), cycles);
        return Math.Clamp(1.0 - nonExceedance, 0.0, 1.0);
    }

    public static double LevelForShortTermMaximumExceedanceProbability(
        ResponseStatistics statistics,
        TimeSpan duration,
        double exceedanceProbability)
    {
        ArgumentNullException.ThrowIfNull(statistics);
        if (exceedanceProbability <= 0.0)
        {
            return double.PositiveInfinity;
        }

        if (exceedanceProbability >= 1.0)
        {
            return 0.0;
        }

        if (statistics.M0 <= 0.0 || double.IsInfinity(statistics.MeanZeroUpcrossingPeriod))
        {
            return 0.0;
        }

        var cycles = Math.Max(duration.TotalSeconds / statistics.MeanZeroUpcrossingPeriod, 1.0);
        var peakNonExceedance = Math.Pow(1.0 - exceedanceProbability, 1.0 / cycles);
        var peakExceedance = Math.Max(1.0e-300, 1.0 - peakNonExceedance);
        return Math.Sqrt(-2.0 * statistics.M0 * Math.Log(peakExceedance));
    }

    public static LongTermExtremeResult AnnualReturnValue(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        double returnPeriodYears)
    {
        if (returnPeriodYears <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(returnPeriodYears), "Return period must be positive.");
        }

        return SolveLongTermValue(
            seaStates,
            seaStateDuration,
            targetProbability: 1.0 / returnPeriodYears,
            probabilityFunction: level => AnnualExceedanceProbability(seaStates, seaStateDuration, level),
            description: $"{returnPeriodYears:g}-year annual return value");
    }

    public static LongTermExtremeResult ExposureMaximumValue(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        TimeSpan exposureDuration,
        double targetExceedanceProbability = 0.6321205588285577)
    {
        if (exposureDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(exposureDuration), "Exposure duration must be positive.");
        }

        return SolveLongTermValue(
            seaStates,
            seaStateDuration,
            targetExceedanceProbability,
            level => ExposureExceedanceProbability(seaStates, seaStateDuration, exposureDuration, level),
            $"Exposure maximum value over {exposureDuration.TotalDays:g} days");
    }

    public static double AnnualExceedanceProbability(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        double level)
    {
        var q = WeightedShortTermMaximumExceedanceProbability(seaStates, seaStateDuration, level);
        var seaStatesPerYear = Numerics.JulianYearSeconds / seaStateDuration.TotalSeconds;
        return AtLeastOneExceedance(q, seaStatesPerYear);
    }

    public static double ExposureExceedanceProbability(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        TimeSpan exposureDuration,
        double level)
    {
        var q = WeightedShortTermMaximumExceedanceProbability(seaStates, seaStateDuration, level);
        var count = exposureDuration.TotalSeconds / seaStateDuration.TotalSeconds;
        return AtLeastOneExceedance(q, count);
    }

    private static double WeightedShortTermMaximumExceedanceProbability(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        double level)
    {
        ValidateLongTermInputs(seaStates, seaStateDuration);

        var probabilitySum = seaStates.Sum(state => state.Probability);
        var q = 0.0;
        foreach (var state in seaStates)
        {
            var weight = state.Probability / probabilitySum;
            q += weight * ShortTermMaximumExceedanceProbability(state.Statistics, seaStateDuration, level);
        }

        return Math.Clamp(q, 0.0, 1.0);
    }

    private static LongTermExtremeResult SolveLongTermValue(
        IReadOnlyList<LongTermSeaState> seaStates,
        TimeSpan seaStateDuration,
        double targetProbability,
        Func<double, double> probabilityFunction,
        string description)
    {
        ValidateLongTermInputs(seaStates, seaStateDuration);
        if (targetProbability <= 0.0 || targetProbability >= 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetProbability), "Target probability must be between 0 and 1.");
        }

        var high = Math.Max(1.0, seaStates.Max(state => state.Statistics.Rms) * 12.0);
        while (probabilityFunction(high) > targetProbability)
        {
            high *= 2.0;
            if (high > 1.0e12)
            {
                throw new InvalidOperationException("Could not bracket the long-term return value.");
            }
        }

        var low = 0.0;
        for (var i = 0; i < 100; i++)
        {
            var mid = 0.5 * (low + high);
            if (probabilityFunction(mid) > targetProbability)
            {
                low = mid;
            }
            else
            {
                high = mid;
            }
        }

        var value = 0.5 * (low + high);
        return new LongTermExtremeResult(description, value, targetProbability, probabilityFunction(value));
    }

    private static double AtLeastOneExceedance(double shortTermExceedanceProbability, double count)
    {
        if (shortTermExceedanceProbability <= 0.0 || count <= 0.0)
        {
            return 0.0;
        }

        if (shortTermExceedanceProbability >= 1.0)
        {
            return 1.0;
        }

        return Math.Clamp(1.0 - Math.Exp(count * Math.Log(1.0 - shortTermExceedanceProbability)), 0.0, 1.0);
    }

    private static double ExpectedMaximumRayleighApproximation(double sigma, double cycles)
    {
        if (cycles <= 1.0)
        {
            return sigma * Math.Sqrt(Math.PI / 2.0);
        }

        var u = Math.Sqrt(2.0 * Math.Log(cycles));
        return sigma * (u + (Numerics.EulerMascheroni / u));
    }

    private static void ValidateLongTermInputs(IReadOnlyList<LongTermSeaState> seaStates, TimeSpan seaStateDuration)
    {
        ArgumentNullException.ThrowIfNull(seaStates);
        if (seaStates.Count == 0)
        {
            throw new ArgumentException("At least one sea state is required.", nameof(seaStates));
        }

        if (seaStateDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(seaStateDuration), "Sea-state duration must be positive.");
        }

        if (seaStates.Any(state => state.Probability < 0.0))
        {
            throw new ArgumentException("Sea-state probabilities cannot be negative.", nameof(seaStates));
        }

        if (seaStates.Sum(state => state.Probability) <= 0.0)
        {
            throw new ArgumentException("At least one sea-state probability must be positive.", nameof(seaStates));
        }
    }
}

public sealed record ShortTermExtremeResult(
    string Name,
    TimeSpan Duration,
    double CycleCount,
    double Sigma,
    double MostProbableMaximum,
    double ExpectedMaximum);

public sealed record LongTermSeaState(
    string Name,
    double Probability,
    ResponseStatistics Statistics);

public sealed record LongTermExtremeResult(
    string Description,
    double Value,
    double TargetExceedanceProbability,
    double ActualExceedanceProbability);
