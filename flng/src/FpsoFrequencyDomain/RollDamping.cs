namespace FpsoFrequencyDomain;

public sealed record NonlinearRollDamping(
    double Linear,
    double Quadratic,
    double Cubic = 0.0);

public sealed record RollDampingIterationResult(
    double EquivalentLinearDamping,
    double RollAngleStdDev,
    double RollVelocityStdDev,
    int Iterations,
    IReadOnlyList<double> DampingHistory);

public static class RollDampingLinearizer
{
    /// <summary>
    /// Equivalent linear damping for M = B1*xDot + B2*|xDot|xDot + B3*xDot^3
    /// under a zero-mean Gaussian roll velocity assumption.
    /// </summary>
    public static double EquivalentLinearDamping(
        NonlinearRollDamping damping,
        double rollVelocityStdDev)
    {
        if (rollVelocityStdDev < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(rollVelocityStdDev), "Velocity standard deviation cannot be negative.");
        }

        return damping.Linear
            + (damping.Quadratic * Math.Sqrt(8.0 / Math.PI) * rollVelocityStdDev)
            + (3.0 * damping.Cubic * rollVelocityStdDev * rollVelocityStdDev);
    }

    public static double EquivalentLinearDamping(
        NonlinearRollDamping damping,
        ResponseSpectrum rollAngleSpectrum)
    {
        ArgumentNullException.ThrowIfNull(rollAngleSpectrum);
        var moments = rollAngleSpectrum.Moments();
        var rollVelocityStdDev = Math.Sqrt(Math.Max(moments.M2, 0.0));
        return EquivalentLinearDamping(damping, rollVelocityStdDev);
    }

    /// <summary>
    /// Iterates equivalent damping when the caller can regenerate roll response spectrum from damping.
    /// The factory receives the current equivalent linear damping and returns the roll angle spectrum.
    /// </summary>
    public static RollDampingIterationResult Iterate(
        NonlinearRollDamping damping,
        Func<double, ResponseSpectrum> rollSpectrumFactory,
        double initialEquivalentDamping,
        int maxIterations = 30,
        double tolerance = 1.0e-4,
        double relaxation = 0.6)
    {
        ArgumentNullException.ThrowIfNull(rollSpectrumFactory);
        if (maxIterations <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxIterations), "At least one iteration is required.");
        }

        if (tolerance <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be positive.");
        }

        if (relaxation <= 0.0 || relaxation > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(relaxation), "Relaxation must be in (0, 1].");
        }

        var current = initialEquivalentDamping;
        var history = new List<double> { current };
        ResponseStatistics? lastStatistics = null;

        for (var iteration = 1; iteration <= maxIterations; iteration++)
        {
            var spectrum = rollSpectrumFactory(current);
            lastStatistics = spectrum.Statistics();
            var rollVelocityStdDev = Math.Sqrt(Math.Max(lastStatistics.M2, 0.0));
            var target = EquivalentLinearDamping(damping, rollVelocityStdDev);
            var next = ((1.0 - relaxation) * current) + (relaxation * target);
            history.Add(next);

            if (Math.Abs(next - current) <= tolerance * Math.Max(1.0, Math.Abs(current)))
            {
                return new RollDampingIterationResult(
                    next,
                    lastStatistics.Rms,
                    rollVelocityStdDev,
                    iteration,
                    history);
            }

            current = next;
        }

        var finalSpectrum = rollSpectrumFactory(current);
        lastStatistics = finalSpectrum.Statistics();
        return new RollDampingIterationResult(
            current,
            lastStatistics.Rms,
            Math.Sqrt(Math.Max(lastStatistics.M2, 0.0)),
            maxIterations,
            history);
    }
}
