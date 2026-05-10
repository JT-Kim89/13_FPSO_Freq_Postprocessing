using System.Numerics;

namespace FpsoFrequencyDomain;

public sealed record ResponseAnalysis(
    string Name,
    ResponseSpectrum Displacement,
    ResponseSpectrum Velocity,
    ResponseSpectrum Acceleration,
    ResponseStatistics Statistics,
    ShortTermExtremeResult ShortTermExtreme);

public sealed record SixDofMotionAnalysis(
    IReadOnlyDictionary<MotionDof, ResponseAnalysis> Motions);

public sealed record PointMotionAnalysis(
    ResponseAnalysis X,
    ResponseAnalysis Y,
    ResponseAnalysis Z);

public static class FrequencyDomainAnalyzer
{
    public static SixDofMotionAnalysis AnalyzeSixDof(
        SixDofRao rao,
        WaveSpectrum waveSpectrum,
        TimeSpan shortTermDuration)
    {
        ArgumentNullException.ThrowIfNull(rao);
        ArgumentNullException.ThrowIfNull(waveSpectrum);

        var raoOnWaveGrid = rao.InterpolateTo(waveSpectrum.FrequencyHz);
        var motions = new Dictionary<MotionDof, ResponseAnalysis>
        {
            [MotionDof.Surge] = AnalyzeRao("Surge", waveSpectrum, raoOnWaveGrid.Surge, shortTermDuration),
            [MotionDof.Sway] = AnalyzeRao("Sway", waveSpectrum, raoOnWaveGrid.Sway, shortTermDuration),
            [MotionDof.Heave] = AnalyzeRao("Heave", waveSpectrum, raoOnWaveGrid.Heave, shortTermDuration),
            [MotionDof.Roll] = AnalyzeRao("Roll", waveSpectrum, raoOnWaveGrid.Roll, shortTermDuration),
            [MotionDof.Pitch] = AnalyzeRao("Pitch", waveSpectrum, raoOnWaveGrid.Pitch, shortTermDuration),
            [MotionDof.Yaw] = AnalyzeRao("Yaw", waveSpectrum, raoOnWaveGrid.Yaw, shortTermDuration)
        };

        return new SixDofMotionAnalysis(motions);
    }

    public static PointMotionAnalysis AnalyzePoint(
        SixDofRao rao,
        WaveSpectrum waveSpectrum,
        BodyPoint pointFromReferenceOrigin,
        TimeSpan shortTermDuration,
        string name = "Point")
    {
        ArgumentNullException.ThrowIfNull(rao);
        ArgumentNullException.ThrowIfNull(waveSpectrum);

        var raoOnWaveGrid = rao.InterpolateTo(waveSpectrum.FrequencyHz);
        var point = raoOnWaveGrid.AtPoint(pointFromReferenceOrigin);
        return new PointMotionAnalysis(
            AnalyzeRao($"{name} X", waveSpectrum, point.X, shortTermDuration),
            AnalyzeRao($"{name} Y", waveSpectrum, point.Y, shortTermDuration),
            AnalyzeRao($"{name} Z", waveSpectrum, point.Z, shortTermDuration));
    }

    public static ResponseAnalysis AnalyzeRao(
        string name,
        WaveSpectrum waveSpectrum,
        IReadOnlyList<Complex> rao,
        TimeSpan shortTermDuration)
    {
        if (shortTermDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(shortTermDuration), "Short-term duration must be positive.");
        }

        var displacement = ResponseSpectrum.FromRao(waveSpectrum, rao, name);
        var velocity = displacement.ToVelocity();
        var acceleration = displacement.ToAcceleration();
        var statistics = displacement.Statistics();
        var shortTerm = ExtremeStatistics.ShortTerm(statistics, shortTermDuration);
        return new ResponseAnalysis(name, displacement, velocity, acceleration, statistics, shortTerm);
    }
}
