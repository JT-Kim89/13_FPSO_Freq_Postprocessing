# FLNG Frequency-Domain Module

`flng/`는 FLNG frequency-domain postprocessing 전용 모듈입니다. C# 라이브러리는 RAO와 wave spectrum을 받아 motion statistics, local acceleration, relative wave, air gap, operability, 그리고 FLNG-LNGC side-by-side relative motion을 계산합니다.

## 구성

```text
src/FpsoFrequencyDomain/              계산 라이브러리
samples/FpsoFrequencyDomain.Sample/   예제 실행 프로젝트
tests/FpsoFrequencyDomain.Tests/      외부 test framework 없는 smoke test
```

## 실행

저장소 루트에서:

```powershell
dotnet run --project flng/samples/FpsoFrequencyDomain.Sample
dotnet run --project flng/tests/FpsoFrequencyDomain.Tests
```

`flng/` 폴더 안에서:

```powershell
dotnet run --project samples/FpsoFrequencyDomain.Sample
dotnet run --project tests/FpsoFrequencyDomain.Tests
```

## 주요 입력

- frequency array
- wave spectrum, 또는 `Hs`, `Tp`, `gamma`로 생성한 PM/JONSWAP spectrum
- FLNG 6DOF complex motion RAO
- 필요 시 LNGC 6DOF complex motion RAO
- RAO reference origin에서 COG 및 local point까지의 좌표
- side-by-side 계산 시 LNGC origin의 FLNG 기준 offset
- short-term duration, 예: 3 hr
- long-term sea-state probability

## Side-By-Side Relative Motion

기본 결과는 다음 sense입니다.

```text
relative = LNGC point motion - FLNG point motion
```

우현에 LNGC가 붙는 경우 현재 좌표계에서는 `y`가 port positive이므로 LNGC origin offset은 보통 negative `Y`입니다.

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
```

두 RAO가 이미 같은 incident-wave phase 기준이면 `CommonWaveReference`를 사용합니다. 각 vessel origin 기준으로 RAO가 나온 경우에는 `EachBodyOrigin`을 사용하면 LNGC offset에 따른 phase shift가 적용됩니다.
