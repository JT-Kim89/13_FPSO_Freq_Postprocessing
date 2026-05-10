# FLNG Frequency Domain Engine

This C# library implements the first-pass FLNG frequency-domain workflow using
6DOF motion RAO and wave spectra only. Stress RAO, load RAO, mooring RAO, riser
RAO, and second-order QTF workflows are intentionally outside this v1 scope.

## Coordinate And Unit Convention

- Body axes: `x` forward, `y` port, `z` up.
- Coordinates are metres.
- Translational RAOs are `m/m`.
- Rotational RAOs are `rad/m`.
- Wave spectra are one-sided `S_eta(f)` in `m^2/Hz`.
- The input 6DOF RAO origin is assumed to be the waterline centre unless the
  caller translates it.

For a point `r = (x, y, z)` from the current RAO origin, local small-angle
motion is:

```text
u_x = surge + pitch*z - yaw*y
u_y = sway  + yaw*x   - roll*z
u_z = heave + roll*y  - pitch*x
```

So if the RAO origin is waterline centre and COG is 12 m below it:

```csharp
var cogFromWaterlineCentre = new BodyPoint(0.0, 0.0, -12.0);
var cogRao = waterlineCentreRao.TranslateReferenceTo(cogFromWaterlineCentre);
```

If a local point is defined from COG:

```csharp
var topsideFromCog = new BodyPoint(65.0, 18.0, 42.0);
var topsideMotion = waterlineCentreRao.AtPointFromCog(cogFromWaterlineCentre, topsideFromCog);
```

## Implemented

- PM and JONSWAP wave spectra
- 6DOF response spectra
- COG/reference-point transformation
- local point displacement, velocity, and acceleration spectra
- spectral moments `m0`, `m1`, `m2`, `m4`
- RMS, significant single/double amplitude
- zero-upcrossing period and peak response period
- short-term MPM and expected maximum
- short-term exceedance probability
- long-term annual return value from scatter probabilities
- relative wave elevation spectrum and MPM
- air gap / deck wetness probability from relative wave response
- two-body relative motion for side-by-side FLNG/LNGC offloading
- operability/downtime checks
- stochastic equivalent linear roll damping helper

## Side-By-Side FLNG / LNGC Relative Motion

Use `TwoBodyRelativeMotionAnalyzer` when a second RAO model is available for
the LNG carrier. The default output is:

```text
relative = LNGC point motion - FLNG point motion
```

The result is expressed in the FLNG axes. For a starboard-side LNGC with the
same heading as the FLNG, the LNGC origin offset normally has negative `Y`
because `y` is positive to port.

```csharp
var lngcOriginFromFlngOrigin = new BodyPoint(0.0, -92.0, 0.0);
var flngStarboardManifold = new BodyPoint(20.0, -34.0, 18.0);
var lngcPortManifold = new BodyPoint(20.0, 22.0, 16.0);

var relative = TwoBodyRelativeMotionAnalyzer.AnalyzeRelativePoint(
    primaryRao: flngRao,
    primaryPointFromPrimaryOrigin: flngStarboardManifold,
    secondaryRao: lngcRao,
    secondaryPointFromSecondaryOrigin: lngcPortManifold,
    secondaryOriginFromPrimaryOrigin: lngcOriginFromFlngOrigin,
    waveSpectrum: wave,
    shortTermDuration: TimeSpan.FromHours(3),
    headingRadians: Math.PI,
    phaseReferenceConvention: RaoPhaseReferenceConvention.EachBodyOrigin,
    sense: RelativeMotionSense.SecondaryMinusPrimary,
    name: "LNGC minus FLNG manifold");

var transverseGapMpm = relative.Analysis.Y.ShortTermExtreme.MostProbableMaximum;
var verticalRelativeMpm = relative.Analysis.Z.ShortTermExtreme.MostProbableMaximum;
```

If both RAO files were exported against one shared incident-wave phase reference,
use `RaoPhaseReferenceConvention.CommonWaveReference`. If each RAO was exported
with wave phase referenced to that vessel's own origin, use
`RaoPhaseReferenceConvention.EachBodyOrigin`; the LNGC RAO will be phase-shifted
by its offset from the FLNG origin.

## Example

```powershell
dotnet run --project flng/samples/FpsoFrequencyDomain.Sample
dotnet run --project flng/tests/FpsoFrequencyDomain.Tests
```

This workspace currently has the .NET runtime but not the .NET SDK installed,
so the commands above need a machine with the .NET 8 SDK.
