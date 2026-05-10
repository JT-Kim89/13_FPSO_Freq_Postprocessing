using System.Numerics;

namespace FpsoFrequencyDomain;

public static class RelativeWaveAnalyzer
{
    /// <summary>
    /// Relative vertical wave RAO: wave elevation at point minus body vertical motion at point.
    /// Heading is the wave propagation direction measured from +x toward +y, in radians.
    /// </summary>
    public static Complex[] RelativeVerticalRao(
        SixDofRao bodyRao,
        BodyPoint pointFromReferenceOrigin,
        double headingRadians,
        double? waterDepth = null)
    {
        ArgumentNullException.ThrowIfNull(bodyRao);
        var pointMotion = bodyRao.AtPoint(pointFromReferenceOrigin);
        var relative = new Complex[bodyRao.Count];

        for (var i = 0; i < bodyRao.Count; i++)
        {
            var waveNumber = WaveKinematics.WaveNumber(bodyRao.FrequencyHz[i], waterDepth);
            var waveElevation = WaveKinematics.WaveElevationRaoAtPoint(
                waveNumber,
                headingRadians,
                pointFromReferenceOrigin);
            relative[i] = waveElevation - pointMotion.Z[i];
        }

        return relative;
    }

    public static ResponseSpectrum Spectrum(
        WaveSpectrum waveSpectrum,
        SixDofRao bodyRao,
        BodyPoint pointFromReferenceOrigin,
        double headingRadians,
        double? waterDepth = null,
        string name = "Relative wave")
    {
        var raoOnWaveGrid = bodyRao.InterpolateTo(waveSpectrum.FrequencyHz);
        var relativeRao = RelativeVerticalRao(raoOnWaveGrid, pointFromReferenceOrigin, headingRadians, waterDepth);
        return ResponseSpectrum.FromRao(waveSpectrum, relativeRao, name);
    }
}

public static class WaveKinematics
{
    public static double WaveNumber(double frequencyHz, double? waterDepth = null, double gravity = Numerics.Gravity)
    {
        if (frequencyHz <= 0.0)
        {
            return 0.0;
        }

        var omega = Numerics.TwoPi * frequencyHz;
        if (waterDepth is null || double.IsPositiveInfinity(waterDepth.Value))
        {
            return omega * omega / gravity;
        }

        var depth = waterDepth.Value;
        if (depth <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(waterDepth), "Water depth must be positive.");
        }

        var k = Math.Max(omega * omega / gravity, 1.0e-8);
        for (var i = 0; i < 30; i++)
        {
            var kh = k * depth;
            var tanh = Math.Tanh(kh);
            var sech2 = 1.0 / (Math.Cosh(kh) * Math.Cosh(kh));
            var f = (gravity * k * tanh) - (omega * omega);
            var df = gravity * (tanh + (k * depth * sech2));
            var step = f / df;
            k = Math.Max(1.0e-12, k - step);
            if (Math.Abs(step) < 1.0e-12)
            {
                break;
            }
        }

        return k;
    }

    public static Complex WaveElevationRaoAtPoint(
        double waveNumber,
        double headingRadians,
        BodyPoint pointFromReferenceOrigin)
    {
        var projectedDistance =
            (pointFromReferenceOrigin.X * Math.Cos(headingRadians)) +
            (pointFromReferenceOrigin.Y * Math.Sin(headingRadians));
        return Complex.FromPolarCoordinates(1.0, -waveNumber * projectedDistance);
    }
}

public sealed record AirGapResult(
    double DeckElevation,
    double RelativeWaveMpm,
    double MinimumAirGapAtMpm,
    double ShortTermDeckWetnessProbability);

public static class AirGapAnalyzer
{
    /// <summary>
    /// Evaluates deck wetness from a relative wave response spectrum.
    /// Deck elevation is the vertical distance from still-water level to the checked deck point.
    /// </summary>
    public static AirGapResult Evaluate(
        ResponseSpectrum relativeWaveSpectrum,
        double deckElevation,
        TimeSpan shortTermDuration)
    {
        ArgumentNullException.ThrowIfNull(relativeWaveSpectrum);
        var statistics = relativeWaveSpectrum.Statistics();
        var shortTerm = ExtremeStatistics.ShortTerm(statistics, shortTermDuration);
        var wetnessProbability = ExtremeStatistics.ShortTermMaximumExceedanceProbability(
            statistics,
            shortTermDuration,
            deckElevation);

        return new AirGapResult(
            deckElevation,
            shortTerm.MostProbableMaximum,
            deckElevation - shortTerm.MostProbableMaximum,
            wetnessProbability);
    }
}
