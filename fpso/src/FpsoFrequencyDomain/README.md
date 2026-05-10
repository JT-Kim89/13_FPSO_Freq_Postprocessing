# FPSO Frequency Domain Engine

이 라이브러리는 FPSO motion RAO와 wave spectrum을 이용한 frequency-domain 후처리 계산을 제공합니다. 기본 대상은 FPSO이며, side-by-side relative motion은 같은 엔진 위에서 쓰는 확장 기능입니다.

## Core Workflow

1. Frequency grid와 wave spectrum을 준비합니다.
2. FPSO 6DOF complex RAO를 입력합니다.
3. RAO reference origin을 COG 또는 local point로 변환합니다.
4. RAO와 wave spectrum을 곱해 response spectrum을 만듭니다.
5. Spectral moment와 extreme statistics를 계산합니다.

## Main Classes

- `WaveSpectra`: PM/JONSWAP spectrum 생성
- `SixDofRao`: 6DOF complex RAO 보간 및 기준점 변환
- `ResponseSpectrum`: response spectrum, velocity/acceleration spectrum, spectral statistics
- `FrequencyDomainAnalyzer`: 단일 RAO에 대한 short-term/long-term postprocessing facade
- `RelativeWaveAnalyzer`: relative wave elevation 계산
- `AirGapAnalyzer`: air gap 및 deck wetness probability 계산
- `OperabilityAnalyzer`: downtime/operability 계산
- `RollDampingLinearizer`: stochastic equivalent linear roll damping 계산 helper
- `TwoBodyRelativeMotionAnalyzer`: 두 선체 local point의 relative motion 계산

## RAO Reference Conversion

사용자 조건처럼 RAO가 수선면 배 중심 기준일 때는 COG 및 local point로 변환해서 사용합니다.

```csharp
var cogFromWaterlineCentre = new BodyPoint(0.0, 0.0, -12.0);
var localPointFromCog = new BodyPoint(65.0, 18.0, 42.0);

var localPointRao = waterlineCentreRao.AtPointFromCog(
    cogFromWaterlineCentre,
    localPointFromCog);
```

## FLNG / LNGC Side-By-Side Extension

FLNG 옆에 LNGC가 붙어서 하역하는 경우에는 FLNG RAO와 LNGC RAO를 각각 입력하고, 두 local point motion의 차이를 만듭니다.

```csharp
var relative = TwoBodyRelativeMotionAnalyzer.AnalyzeRelativePoint(
    primaryRao: flngRao,
    primaryPointFromPrimaryOrigin: flngStarboardManifold,
    secondaryRao: lngcRao,
    secondaryPointFromSecondaryOrigin: lngcPortManifold,
    secondaryOriginFromPrimaryOrigin: lngcOriginFromFlngOrigin,
    waveSpectrum: wave,
    shortTermDuration: TimeSpan.FromHours(3),
    headingRadians: Math.PI,
    phaseReferenceConvention: RaoPhaseReferenceConvention.EachBodyOrigin);
```

Shielding effect는 RAO 생성 단계에서 반영된 파일을 입력하는 방식을 가정합니다.
