using System.Numerics;
using FpsoFrequencyDomain;

var frequencyHz = Linspace(0.03, 0.80, 240);
var waterlineCentreRao = BuildSyntheticWaterlineCentreRao(frequencyHz);

// Coordinate convention: x forward, y port, z up. RAO origin is waterline centre.
// Example COG is 12 m below the waterline centre.
var cogFromWaterlineCentre = new BodyPoint(0.0, 0.0, -12.0);
var cogRao = waterlineCentreRao.TranslateReferenceTo(cogFromWaterlineCentre);

var wave = WaveSpectra.Jonswap(
    frequencyHz,
    significantWaveHeight: 6.0,
    peakPeriod: 12.0,
    gamma: 3.3);

var heaveAtCog = ResponseSpectrum
    .FromRao(wave, cogRao.Heave, "COG heave")
    .Statistics();
var heaveShortTerm = ExtremeStatistics.ShortTerm(heaveAtCog, TimeSpan.FromHours(3));

Console.WriteLine("=== FPSO frequency-domain sample ===");
Console.WriteLine($"COG Heave RMS          : {heaveAtCog.Rms:F4} m");
Console.WriteLine($"COG Heave 3h MPM       : {heaveShortTerm.MostProbableMaximum:F4} m");
Console.WriteLine($"COG Heave Tz           : {heaveAtCog.MeanZeroUpcrossingPeriod:F3} s");

var topsideFromCog = new BodyPoint(65.0, 18.0, 42.0);
var topsideMotion = waterlineCentreRao.AtPointFromCog(cogFromWaterlineCentre, topsideFromCog);
var topsideVerticalAcceleration = ResponseSpectrum
    .FromRao(wave, topsideMotion.Z, "Topside vertical")
    .ToAcceleration()
    .Statistics();
Console.WriteLine($"Topside vertical acc RMS: {topsideVerticalAcceleration.Rms:F4} m/s^2");

var bowDeckPointFromReference = new BodyPoint(150.0, 0.0, 18.0);
var relativeWave = RelativeWaveAnalyzer.Spectrum(
    wave,
    waterlineCentreRao,
    bowDeckPointFromReference,
    headingRadians: Math.PI,
    waterDepth: null,
    name: "Bow relative wave");
var relativeWaveStats = relativeWave.Statistics();
var relativeWave3h = ExtremeStatistics.ShortTerm(relativeWaveStats, TimeSpan.FromHours(3));
var bowAirGap = AirGapAnalyzer.Evaluate(
    relativeWave,
    deckElevation: 18.0,
    shortTermDuration: TimeSpan.FromHours(3));
Console.WriteLine($"Bow relative wave 3h MPM: {relativeWave3h.MostProbableMaximum:F4} m");
Console.WriteLine($"Bow air gap at MPM       : {bowAirGap.MinimumAirGapAtMpm:F4} m");

var lngcWaterlineCentreRao = BuildSyntheticLngcRao(frequencyHz);
var lngcOriginFromFlngOrigin = new BodyPoint(0.0, -92.0, 0.0);
var flngStarboardManifold = new BodyPoint(20.0, -34.0, 18.0);
var lngcPortManifold = new BodyPoint(20.0, 22.0, 16.0);
var sideBySide = TwoBodyRelativeMotionAnalyzer.AnalyzeRelativePoint(
    primaryRao: waterlineCentreRao,
    primaryPointFromPrimaryOrigin: flngStarboardManifold,
    secondaryRao: lngcWaterlineCentreRao,
    secondaryPointFromSecondaryOrigin: lngcPortManifold,
    secondaryOriginFromPrimaryOrigin: lngcOriginFromFlngOrigin,
    waveSpectrum: wave,
    shortTermDuration: TimeSpan.FromHours(3),
    headingRadians: Math.PI,
    phaseReferenceConvention: RaoPhaseReferenceConvention.EachBodyOrigin,
    sense: RelativeMotionSense.SecondaryMinusPrimary,
    name: "LNGC minus FLNG manifold");
Console.WriteLine($"Side-by-side relative Y 3h MPM: {sideBySide.Analysis.Y.ShortTermExtreme.MostProbableMaximum:F4} m");

var longTermStates = BuildLongTermStates(frequencyHz, waterlineCentreRao, cogFromWaterlineCentre);
var heave100Year = ExtremeStatistics.AnnualReturnValue(
    longTermStates,
    seaStateDuration: TimeSpan.FromHours(3),
    returnPeriodYears: 100.0);
Console.WriteLine($"COG Heave 100-year value: {heave100Year.Value:F4} m");

var operability = OperabilityAnalyzer.Evaluate(
    longTermStates.Select(state => new OperabilitySeaState(
        state.Name,
        state.Probability,
        new Dictionary<string, ResponseStatistics>
        {
            ["COG heave"] = state.Statistics
        })).ToArray(),
    new[]
    {
        new OperabilityCriterion(
            "Heave MPM limit",
            "COG heave",
            Limit: 7.0,
            Metric: OperabilityMetric.ShortTermMpm)
    },
    TimeSpan.FromHours(3));
Console.WriteLine($"Operability             : {100.0 * operability.OperableProbability:F1}%");

static LongTermSeaState[] BuildLongTermStates(
    double[] frequencyHz,
    SixDofRao waterlineCentreRao,
    BodyPoint cogFromWaterlineCentre)
{
    var seaStates = new[]
    {
        (Name: "Hs2 Tp8", Hs: 2.0, Tp: 8.0, Probability: 0.55),
        (Name: "Hs4 Tp10", Hs: 4.0, Tp: 10.0, Probability: 0.30),
        (Name: "Hs6 Tp12", Hs: 6.0, Tp: 12.0, Probability: 0.12),
        (Name: "Hs9 Tp14", Hs: 9.0, Tp: 14.0, Probability: 0.03)
    };

    var cogRao = waterlineCentreRao.TranslateReferenceTo(cogFromWaterlineCentre);
    return seaStates
        .Select(state =>
        {
            var wave = WaveSpectra.Jonswap(frequencyHz, state.Hs, state.Tp);
            var stats = ResponseSpectrum.FromRao(wave, cogRao.Heave, "COG heave").Statistics();
            return new LongTermSeaState(state.Name, state.Probability, stats);
        })
        .ToArray();
}

static SixDofRao BuildSyntheticWaterlineCentreRao(double[] frequencyHz)
{
    Complex[] Gaussian(double centre, double width, double gain, double phaseDeg)
    {
        var result = new Complex[frequencyHz.Length];
        for (var i = 0; i < frequencyHz.Length; i++)
        {
            var f = frequencyHz[i];
            var amplitude = 0.04 + gain * Math.Exp(-Math.Pow(f - centre, 2.0) / (2.0 * width * width));
            result[i] = Complex.FromPolarCoordinates(amplitude, phaseDeg * Math.PI / 180.0);
        }

        return result;
    }

    var zero = frequencyHz.Select(_ => Complex.Zero).ToArray();
    return new SixDofRao(
        frequencyHz,
        surge: Gaussian(0.060, 0.030, 0.45, -20.0),
        sway: zero,
        heave: Gaussian(0.090, 0.035, 0.95, -90.0),
        roll: Gaussian(0.075, 0.020, 0.030, -105.0),
        pitch: Gaussian(0.085, 0.025, 0.020, -100.0),
        yaw: zero,
        name: "Synthetic waterline-centre RAO");
}

static SixDofRao BuildSyntheticLngcRao(double[] frequencyHz)
{
    Complex[] Gaussian(double centre, double width, double gain, double phaseDeg)
    {
        var result = new Complex[frequencyHz.Length];
        for (var i = 0; i < frequencyHz.Length; i++)
        {
            var f = frequencyHz[i];
            var amplitude = 0.03 + gain * Math.Exp(-Math.Pow(f - centre, 2.0) / (2.0 * width * width));
            result[i] = Complex.FromPolarCoordinates(amplitude, phaseDeg * Math.PI / 180.0);
        }

        return result;
    }

    var zero = frequencyHz.Select(_ => Complex.Zero).ToArray();
    return new SixDofRao(
        frequencyHz,
        surge: Gaussian(0.070, 0.030, 0.55, -25.0),
        sway: Gaussian(0.055, 0.025, 0.40, -35.0),
        heave: Gaussian(0.100, 0.035, 1.05, -95.0),
        roll: Gaussian(0.090, 0.018, 0.045, -115.0),
        pitch: Gaussian(0.095, 0.025, 0.025, -105.0),
        yaw: zero,
        name: "Synthetic LNGC waterline-centre RAO");
}

static double[] Linspace(double start, double end, int count)
{
    var values = new double[count];
    var step = (end - start) / (count - 1);
    for (var i = 0; i < count; i++)
    {
        values[i] = start + (i * step);
    }

    return values;
}
