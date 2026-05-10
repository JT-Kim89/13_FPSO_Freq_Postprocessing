using System.Numerics;
using FpsoFrequencyDomain;

TestReferenceTranslation();
TestResponseSpectrumUnitRao();
TestShortTermExceedanceInverse();
TestTwoBodyRelativeMotion();

Console.WriteLine("All FpsoFrequencyDomain smoke tests passed.");

static void TestReferenceTranslation()
{
    var f = new[] { 0.1 };
    var zero = new[] { Complex.Zero };
    var pitch = new[] { new Complex(0.01, 0.0) };
    var rao = new SixDofRao(f, zero, zero, zero, zero, pitch, zero);

    var cog = rao.TranslateReferenceTo(new BodyPoint(0.0, 0.0, -10.0));
    AssertNear(-0.10, cog.Surge[0].Real, 1.0e-12, "Pitch*z contribution to surge");

    var point = rao.AtPoint(new BodyPoint(20.0, 0.0, 0.0));
    AssertNear(-0.20, point.Z[0].Real, 1.0e-12, "-pitch*x contribution to heave");
}

static void TestResponseSpectrumUnitRao()
{
    var f = new[] { 0.0, 1.0 };
    var wave = new WaveSpectrum(f, new[] { 2.0, 2.0 });
    var unit = new[] { Complex.One, Complex.One };
    var response = ResponseSpectrum.FromRao(wave, unit, "unit");
    var stats = response.Statistics();
    AssertNear(2.0, stats.M0, 1.0e-12, "m0 for constant unit response");
    AssertNear(Math.Sqrt(2.0), stats.Rms, 1.0e-12, "RMS for constant unit response");
}

static void TestShortTermExceedanceInverse()
{
    var f = Enumerable.Range(1, 100).Select(i => 0.01 * i).ToArray();
    var wave = new WaveSpectrum(f, f.Select(_ => 1.0).ToArray());
    var unit = f.Select(_ => Complex.One).ToArray();
    var stats = ResponseSpectrum.FromRao(wave, unit, "unit").Statistics();
    var duration = TimeSpan.FromHours(3);
    var level = ExtremeStatistics.LevelForShortTermMaximumExceedanceProbability(stats, duration, 0.10);
    var probability = ExtremeStatistics.ShortTermMaximumExceedanceProbability(stats, duration, level);
    AssertNear(0.10, probability, 1.0e-10, "short-term exceedance inverse");
}

static void TestTwoBodyRelativeMotion()
{
    var f = new[] { 0.1 };
    var zero = new[] { Complex.Zero };
    var primarySurge = new[] { new Complex(1.0, 0.0) };
    var secondarySurge = new[] { new Complex(2.5, 0.0) };
    var primary = new SixDofRao(f, primarySurge, zero, zero, zero, zero, zero);
    var secondary = new SixDofRao(f, secondarySurge, zero, zero, zero, zero, zero);

    var relative = TwoBodyRelativeMotionAnalyzer.RelativePointRao(
        primary,
        BodyPoint.Origin,
        secondary,
        BodyPoint.Origin,
        BodyPoint.Origin,
        headingRadians: 0.0,
        phaseReferenceConvention: RaoPhaseReferenceConvention.CommonWaveReference);

    AssertNear(1.5, relative.X[0].Real, 1.0e-12, "secondary minus primary surge");
}

static void AssertNear(double expected, double actual, double tolerance, string message)
{
    if (Math.Abs(expected - actual) > tolerance)
    {
        throw new InvalidOperationException($"{message}: expected {expected}, got {actual}.");
    }
}
