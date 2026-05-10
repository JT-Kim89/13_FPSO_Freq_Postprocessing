using System.Numerics;

namespace FpsoFrequencyDomain;

/// <summary>
/// One-sided response spectrum S_R(f), using frequency in Hz for integration.
/// </summary>
public sealed class ResponseSpectrum
{
    private readonly double[] _frequencyHz;
    private readonly double[] _density;

    public ResponseSpectrum(IReadOnlyList<double> frequencyHz, IReadOnlyList<double> density, string name = "")
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

    public static ResponseSpectrum FromRao(
        WaveSpectrum waveSpectrum,
        IReadOnlyList<Complex> rao,
        string name = "")
    {
        ArgumentNullException.ThrowIfNull(waveSpectrum);
        Numerics.EnsureSameLength(waveSpectrum.Count, rao.Count, nameof(rao));

        var density = new double[waveSpectrum.Count];
        for (var i = 0; i < waveSpectrum.Count; i++)
        {
            density[i] = rao[i].Magnitude * rao[i].Magnitude * waveSpectrum.Density[i];
        }

        return new ResponseSpectrum(waveSpectrum.FrequencyHz, density, name);
    }

    public ResponseSpectrum ToVelocity(string nameSuffix = " velocity")
    {
        var density = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            var omega = Numerics.TwoPi * _frequencyHz[i];
            density[i] = omega * omega * _density[i];
        }

        return new ResponseSpectrum(_frequencyHz, density, Name + nameSuffix);
    }

    public ResponseSpectrum ToAcceleration(string nameSuffix = " acceleration")
    {
        var density = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            var omega = Numerics.TwoPi * _frequencyHz[i];
            density[i] = Math.Pow(omega, 4.0) * _density[i];
        }

        return new ResponseSpectrum(_frequencyHz, density, Name + nameSuffix);
    }

    public SpectralMoments Moments()
    {
        return new SpectralMoments(
            Moment(0),
            Moment(1),
            Moment(2),
            Moment(4));
    }

    public ResponseStatistics Statistics()
    {
        var moments = Moments();
        var sigma = Math.Sqrt(Math.Max(moments.M0, 0.0));
        var zeroUpcrossingPeriod = moments.M0 > 0.0 && moments.M2 > 0.0
            ? Numerics.TwoPi * Math.Sqrt(moments.M0 / moments.M2)
            : double.PositiveInfinity;
        var peakIndex = Numerics.MaxIndex(_density);
        var peakFrequency = _frequencyHz[peakIndex];
        var peakPeriod = peakFrequency > 0.0 ? 1.0 / peakFrequency : double.PositiveInfinity;
        var bandwidth = moments.M0 > 0.0 && moments.M4 > 0.0
            ? Math.Sqrt(Math.Max(0.0, 1.0 - (moments.M2 * moments.M2 / (moments.M0 * moments.M4))))
            : 0.0;

        return new ResponseStatistics(
            Name,
            moments.M0,
            moments.M1,
            moments.M2,
            moments.M4,
            sigma,
            2.0 * sigma,
            4.0 * sigma,
            zeroUpcrossingPeriod,
            peakFrequency,
            peakPeriod,
            bandwidth);
    }

    private double Moment(int order)
    {
        var integrand = new double[Count];
        for (var i = 0; i < Count; i++)
        {
            var omega = Numerics.TwoPi * _frequencyHz[i];
            integrand[i] = Math.Pow(omega, order) * _density[i];
        }

        return Numerics.Trapz(_frequencyHz, integrand);
    }
}

public sealed record SpectralMoments(double M0, double M1, double M2, double M4);

public sealed record ResponseStatistics(
    string Name,
    double M0,
    double M1,
    double M2,
    double M4,
    double Rms,
    double SignificantSingleAmplitude,
    double SignificantDoubleAmplitude,
    double MeanZeroUpcrossingPeriod,
    double PeakFrequencyHz,
    double PeakPeriod,
    double Bandwidth);
