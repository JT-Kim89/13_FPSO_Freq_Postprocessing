namespace FpsoFrequencyDomain;

public static class WaveSpectra
{
    /// <summary>
    /// Bretschneider / Pierson-Moskowitz style spectrum in m^2/Hz.
    /// </summary>
    public static WaveSpectrum PiersonMoskowitz(
        IReadOnlyList<double> frequencyHz,
        double significantWaveHeight,
        double peakPeriod,
        string name = "PM")
    {
        ValidateSeaState(significantWaveHeight, peakPeriod);
        var frequency = Numerics.Copy(frequencyHz, nameof(frequencyHz));
        var density = new double[frequency.Length];
        var fp = 1.0 / peakPeriod;

        for (var i = 0; i < frequency.Length; i++)
        {
            var f = frequency[i];
            if (f <= 0.0)
            {
                density[i] = 0.0;
                continue;
            }

            density[i] = (5.0 / 16.0)
                * significantWaveHeight
                * significantWaveHeight
                * Math.Pow(fp, 4.0)
                * Math.Pow(f, -5.0)
                * Math.Exp((-5.0 / 4.0) * Math.Pow(fp / f, 4.0));
        }

        return new WaveSpectrum(frequency, density, name);
    }

    /// <summary>
    /// JONSWAP spectrum in m^2/Hz. By default the spectrum is scaled so m0 = Hs^2 / 16.
    /// </summary>
    public static WaveSpectrum Jonswap(
        IReadOnlyList<double> frequencyHz,
        double significantWaveHeight,
        double peakPeriod,
        double gamma = 3.3,
        bool normalizeToSignificantWaveHeight = true,
        string name = "JONSWAP")
    {
        if (gamma <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(gamma), "Gamma must be positive.");
        }

        ValidateSeaState(significantWaveHeight, peakPeriod);
        var pm = PiersonMoskowitz(frequencyHz, significantWaveHeight, peakPeriod, name);
        var frequency = pm.FrequencyHz.ToArray();
        var density = pm.Density.ToArray();
        var fp = 1.0 / peakPeriod;

        for (var i = 0; i < frequency.Length; i++)
        {
            var f = frequency[i];
            if (f <= 0.0)
            {
                density[i] = 0.0;
                continue;
            }

            var sigma = f <= fp ? 0.07 : 0.09;
            var exponent = -Math.Pow((f / fp) - 1.0, 2.0) / (2.0 * sigma * sigma);
            density[i] *= Math.Pow(gamma, Math.Exp(exponent));
        }

        if (normalizeToSignificantWaveHeight)
        {
            ScaleToM0(frequency, density, significantWaveHeight * significantWaveHeight / 16.0);
        }

        return new WaveSpectrum(frequency, density, name);
    }

    public static WaveSpectrum Create(
        SpectrumKind kind,
        IReadOnlyList<double> frequencyHz,
        double significantWaveHeight,
        double peakPeriod,
        double gamma = 3.3)
    {
        return kind switch
        {
            SpectrumKind.PiersonMoskowitz => PiersonMoskowitz(frequencyHz, significantWaveHeight, peakPeriod),
            SpectrumKind.Jonswap => Jonswap(frequencyHz, significantWaveHeight, peakPeriod, gamma),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unsupported spectrum kind.")
        };
    }

    private static void ValidateSeaState(double significantWaveHeight, double peakPeriod)
    {
        if (significantWaveHeight < 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(significantWaveHeight), "Hs cannot be negative.");
        }

        if (peakPeriod <= 0.0)
        {
            throw new ArgumentOutOfRangeException(nameof(peakPeriod), "Tp must be positive.");
        }
    }

    private static void ScaleToM0(IReadOnlyList<double> frequency, double[] density, double targetM0)
    {
        var m0 = Numerics.Trapz(frequency, density);
        if (m0 <= 0.0)
        {
            return;
        }

        var scale = targetM0 / m0;
        for (var i = 0; i < density.Length; i++)
        {
            density[i] *= scale;
        }
    }
}
