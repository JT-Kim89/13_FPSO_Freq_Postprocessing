namespace FpsoFrequencyDomain;

public enum OperabilityMetric
{
    Rms,
    SignificantSingleAmplitude,
    SignificantDoubleAmplitude,
    ShortTermMpm,
    ShortTermExpectedMaximum,
    ShortTermMaximumExceedanceProbability
}

public sealed record OperabilityCriterion(
    string Name,
    string ResponseName,
    double Limit,
    OperabilityMetric Metric,
    double AllowedExceedanceProbability = 0.0);

public sealed record OperabilitySeaState(
    string Name,
    double Probability,
    IReadOnlyDictionary<string, ResponseStatistics> Responses);

public sealed record OperabilityCriterionResult(
    string CriterionName,
    double Value,
    double Limit,
    bool IsPassing);

public sealed record OperabilitySeaStateResult(
    string SeaStateName,
    double Probability,
    bool IsOperable,
    IReadOnlyList<OperabilityCriterionResult> Criteria);

public sealed record OperabilitySummary(
    double OperableProbability,
    double DowntimeProbability,
    IReadOnlyList<OperabilitySeaStateResult> SeaStates);

public static class OperabilityAnalyzer
{
    public static OperabilitySummary Evaluate(
        IReadOnlyList<OperabilitySeaState> seaStates,
        IReadOnlyList<OperabilityCriterion> criteria,
        TimeSpan shortTermDuration)
    {
        ArgumentNullException.ThrowIfNull(seaStates);
        ArgumentNullException.ThrowIfNull(criteria);
        if (shortTermDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(shortTermDuration), "Short-term duration must be positive.");
        }

        var results = new List<OperabilitySeaStateResult>();
        foreach (var seaState in seaStates)
        {
            var criterionResults = new List<OperabilityCriterionResult>();
            foreach (var criterion in criteria)
            {
                if (!seaState.Responses.TryGetValue(criterion.ResponseName, out var statistics))
                {
                    throw new KeyNotFoundException(
                        $"Sea state '{seaState.Name}' does not contain response '{criterion.ResponseName}'.");
                }

                var value = ValueForCriterion(statistics, criterion, shortTermDuration);
                var isPassing = criterion.Metric == OperabilityMetric.ShortTermMaximumExceedanceProbability
                    ? value <= criterion.AllowedExceedanceProbability
                    : value <= criterion.Limit;
                criterionResults.Add(new OperabilityCriterionResult(
                    criterion.Name,
                    value,
                    criterion.Limit,
                    isPassing));
            }

            results.Add(new OperabilitySeaStateResult(
                seaState.Name,
                seaState.Probability,
                criterionResults.All(result => result.IsPassing),
                criterionResults));
        }

        var probabilitySum = results.Sum(result => result.Probability);
        if (probabilitySum <= 0.0)
        {
            throw new ArgumentException("At least one sea-state probability must be positive.", nameof(seaStates));
        }

        var downtime = results
            .Where(result => !result.IsOperable)
            .Sum(result => result.Probability)
            / probabilitySum;

        return new OperabilitySummary(
            OperableProbability: 1.0 - downtime,
            DowntimeProbability: downtime,
            SeaStates: results);
    }

    private static double ValueForCriterion(
        ResponseStatistics statistics,
        OperabilityCriterion criterion,
        TimeSpan shortTermDuration)
    {
        return criterion.Metric switch
        {
            OperabilityMetric.Rms => statistics.Rms,
            OperabilityMetric.SignificantSingleAmplitude => statistics.SignificantSingleAmplitude,
            OperabilityMetric.SignificantDoubleAmplitude => statistics.SignificantDoubleAmplitude,
            OperabilityMetric.ShortTermMpm => ExtremeStatistics.ShortTerm(statistics, shortTermDuration).MostProbableMaximum,
            OperabilityMetric.ShortTermExpectedMaximum => ExtremeStatistics.ShortTerm(statistics, shortTermDuration).ExpectedMaximum,
            OperabilityMetric.ShortTermMaximumExceedanceProbability =>
                ExtremeStatistics.ShortTermMaximumExceedanceProbability(statistics, shortTermDuration, criterion.Limit),
            _ => throw new ArgumentOutOfRangeException(nameof(criterion), criterion.Metric, "Unsupported operability metric.")
        };
    }
}
