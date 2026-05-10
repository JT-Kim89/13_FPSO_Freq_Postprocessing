# FLNG / LNGC Side-By-Side Extension

이 폴더는 FPSO 기본 후처리 코드에 추가되는 FLNG/LNGC side-by-side 해석 메모를 관리합니다. 메인 계산 엔진은 [../fpso](../fpso)에 있으며, FLNG 관련 기능은 두 선체의 relative motion이 필요한 하역 상태를 위한 확장 기능입니다.

## 해석 개념

FLNG 옆에 LNGC가 접안한 상태에서는 FLNG manifold와 LNGC manifold 같은 두 local point 사이의 상대 운동을 평가합니다. 기본 sense는 다음과 같습니다.

```text
relative = LNGC point motion - FLNG point motion
```

후처리 코드는 두 선체의 complex RAO를 같은 frequency grid와 wave phase 기준으로 맞춘 뒤 `X`, `Y`, `Z` 방향 상대 운동 spectrum, RMS, short-term MPM 등을 계산합니다.

## Shielding Effect

Shielding effect, hydrodynamic interaction, coupled radiation/diffraction 효과는 RAO 생성 단계에서 반영하는 것이 좋습니다. 즉, side-by-side condition으로 생성한 FLNG RAO와 LNGC RAO를 후처리 코드에 입력하면 됩니다.

## 사용 예

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
