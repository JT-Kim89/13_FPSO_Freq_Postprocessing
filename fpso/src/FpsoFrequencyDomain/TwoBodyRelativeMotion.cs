using System.Numerics;

namespace FpsoFrequencyDomain;

public enum RaoPhaseReferenceConvention
{
    /// <summary>
    /// Both body RAOs already use the same incident-wave phase reference origin.
    /// </summary>
    CommonWaveReference,

    /// <summary>
    /// Each body RAO uses its own RAO origin as the incident-wave phase reference.
    /// The secondary body is phase-shifted into the primary body's wave reference.
    /// </summary>
    EachBodyOrigin
}

public enum RelativeMotionSense
{
    SecondaryMinusPrimary,
    PrimaryMinusSecondary
}

public sealed record TwoBodyRelativePointResult(
    PointMotionRao RelativeRao,
    PointMotionAnalysis Analysis);

public static class TwoBodyRelativeMotionAnalyzer
{
    /// <summary>
    /// Builds relative local-point motion RAO between two vessels.
    /// Default result is secondary point motion minus primary point motion, expressed in primary axes.
    /// Coordinate convention: x forward, y port, z up. For a starboard-side LNGC, the secondary
    /// origin usually has a negative Y offset from the FLNG origin.
    /// </summary>
    public static PointMotionRao RelativePointRao(
        SixDofRao primaryRao,
        BodyPoint primaryPointFromPrimaryOrigin,
        SixDofRao secondaryRao,
        BodyPoint secondaryPointFromSecondaryOrigin,
        BodyPoint secondaryOriginFromPrimaryOrigin,
        double headingRadians,
        RaoPhaseReferenceConvention phaseReferenceConvention = RaoPhaseReferenceConvention.EachBodyOrigin,
        RelativeMotionSense sense = RelativeMotionSense.SecondaryMinusPrimary,
        double? waterDepth = null,
        double secondaryAxesYawFromPrimaryRadians = 0.0)
    {
        ArgumentNullException.ThrowIfNull(primaryRao);
        ArgumentNullException.ThrowIfNull(secondaryRao);

        var secondaryOnPrimaryGrid = secondaryRao.InterpolateTo(primaryRao.FrequencyHz);
        var primaryPoint = primaryRao.AtPoint(primaryPointFromPrimaryOrigin);
        var secondaryPoint = secondaryOnPrimaryGrid.AtPoint(secondaryPointFromSecondaryOrigin);
        var secondaryInPrimaryAxes = RotateHorizontalComponents(
            secondaryPoint,
            secondaryAxesYawFromPrimaryRadians);

        var x = new Complex[primaryRao.Count];
        var y = new Complex[primaryRao.Count];
        var z = new Complex[primaryRao.Count];

        for (var i = 0; i < primaryRao.Count; i++)
        {
            var phaseShift = SecondaryPhaseShift(
                primaryRao.FrequencyHz[i],
                secondaryOriginFromPrimaryOrigin,
                headingRadians,
                phaseReferenceConvention,
                waterDepth);

            var secondaryX = secondaryInPrimaryAxes.X[i] * phaseShift;
            var secondaryY = secondaryInPrimaryAxes.Y[i] * phaseShift;
            var secondaryZ = secondaryInPrimaryAxes.Z[i] * phaseShift;

            if (sense == RelativeMotionSense.SecondaryMinusPrimary)
            {
                x[i] = secondaryX - primaryPoint.X[i];
                y[i] = secondaryY - primaryPoint.Y[i];
                z[i] = secondaryZ - primaryPoint.Z[i];
            }
            else
            {
                x[i] = primaryPoint.X[i] - secondaryX;
                y[i] = primaryPoint.Y[i] - secondaryY;
                z[i] = primaryPoint.Z[i] - secondaryZ;
            }
        }

        return new PointMotionRao(primaryRao.FrequencyHz, x, y, z);
    }

    public static TwoBodyRelativePointResult AnalyzeRelativePoint(
        SixDofRao primaryRao,
        BodyPoint primaryPointFromPrimaryOrigin,
        SixDofRao secondaryRao,
        BodyPoint secondaryPointFromSecondaryOrigin,
        BodyPoint secondaryOriginFromPrimaryOrigin,
        WaveSpectrum waveSpectrum,
        TimeSpan shortTermDuration,
        double headingRadians,
        RaoPhaseReferenceConvention phaseReferenceConvention = RaoPhaseReferenceConvention.EachBodyOrigin,
        RelativeMotionSense sense = RelativeMotionSense.SecondaryMinusPrimary,
        double? waterDepth = null,
        double secondaryAxesYawFromPrimaryRadians = 0.0,
        string name = "Two-body relative")
    {
        ArgumentNullException.ThrowIfNull(waveSpectrum);

        var primaryOnWaveGrid = primaryRao.InterpolateTo(waveSpectrum.FrequencyHz);
        var secondaryOnWaveGrid = secondaryRao.InterpolateTo(waveSpectrum.FrequencyHz);
        var relative = RelativePointRao(
            primaryOnWaveGrid,
            primaryPointFromPrimaryOrigin,
            secondaryOnWaveGrid,
            secondaryPointFromSecondaryOrigin,
            secondaryOriginFromPrimaryOrigin,
            headingRadians,
            phaseReferenceConvention,
            sense,
            waterDepth,
            secondaryAxesYawFromPrimaryRadians);

        var analysis = new PointMotionAnalysis(
            FrequencyDomainAnalyzer.AnalyzeRao($"{name} X", waveSpectrum, relative.X, shortTermDuration),
            FrequencyDomainAnalyzer.AnalyzeRao($"{name} Y", waveSpectrum, relative.Y, shortTermDuration),
            FrequencyDomainAnalyzer.AnalyzeRao($"{name} Z", waveSpectrum, relative.Z, shortTermDuration));

        return new TwoBodyRelativePointResult(relative, analysis);
    }

    private static Complex SecondaryPhaseShift(
        double frequencyHz,
        BodyPoint secondaryOriginFromPrimaryOrigin,
        double headingRadians,
        RaoPhaseReferenceConvention convention,
        double? waterDepth)
    {
        if (convention == RaoPhaseReferenceConvention.CommonWaveReference)
        {
            return Complex.One;
        }

        var waveNumber = WaveKinematics.WaveNumber(frequencyHz, waterDepth);
        return WaveKinematics.WaveElevationRaoAtPoint(
            waveNumber,
            headingRadians,
            secondaryOriginFromPrimaryOrigin);
    }

    private static PointMotionRao RotateHorizontalComponents(
        PointMotionRao pointMotion,
        double secondaryAxesYawFromPrimaryRadians)
    {
        if (Math.Abs(secondaryAxesYawFromPrimaryRadians) < 1.0e-14)
        {
            return pointMotion;
        }

        var cos = Math.Cos(secondaryAxesYawFromPrimaryRadians);
        var sin = Math.Sin(secondaryAxesYawFromPrimaryRadians);
        var x = new Complex[pointMotion.FrequencyHz.Count];
        var y = new Complex[pointMotion.FrequencyHz.Count];

        for (var i = 0; i < pointMotion.FrequencyHz.Count; i++)
        {
            x[i] = (cos * pointMotion.X[i]) - (sin * pointMotion.Y[i]);
            y[i] = (sin * pointMotion.X[i]) + (cos * pointMotion.Y[i]);
        }

        return new PointMotionRao(pointMotion.FrequencyHz, x, y, pointMotion.Z);
    }
}
