namespace FpsoFrequencyDomain;

/// <summary>
/// One-sided wave spectrum S_eta(f) in m^2/Hz.
/// </summary>
public sealed class WaveSpectrum
{
    private readonly double[] _frequencyHz;
    private readonly double[] _density;

    public WaveSpectrum(IReadOnlyList<double> frequencyHz, IReadOnlyList<double> density, string name = "")
    {
        _frequencyHz = Numerics.Copy(frequencyHz, nameof(frequencyHz));
        _density = Numerics.Copy(density, nameof(density));
        Numerics.EnsureSameLength(_frequencyHz.Length, _density.Length, nameof(density));
        Numerics.EnsureAscending(_frequencyHz, nameof(frequencyHz));
        Name = name;
    }

    public string Name { get; }

    public IReadOnlyList<double> FrequencyHz => _frequencyHz;

    public IReadOnlyList<double> Density => _density;

    public int Count => _frequencyHz.Length;

    public double M0 => Numerics.Trapz(_frequencyHz, _density);

    public double SignificantWaveHeight => 4.0 * Math.Sqrt(Math.Max(M0, 0.0));

    public WaveSpectrum InterpolateTo(IReadOnlyList<double> targetFrequencyHz)
    {
        var target = Numerics.Copy(targetFrequencyHz, nameof(targetFrequencyHz));
        var density = Numerics.InterpolateReal(_frequencyHz, _density, target);
        return new WaveSpectrum(target, density, Name);
    }
}
