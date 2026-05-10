# FPSO / FLNG Frequency-Domain Postprocessing

이 저장소는 RAO와 wave spectrum을 이용해 FPSO/FLNG의 frequency-domain 응답을 후처리하는 계산 코드를 관리합니다. 현재 활성 모듈은 `flng/` 아래에 분리되어 있으며, FLNG 단독 운동과 FLNG 우현에 LNGC가 접안한 side-by-side 하역 상태의 relative motion까지 다룹니다.

## 폴더 구조

```text
flng/
  src/FpsoFrequencyDomain/          C# 계산 라이브러리
  samples/FpsoFrequencyDomain.Sample/  실행 예제
  tests/FpsoFrequencyDomain.Tests/     smoke test
```

## FLNG 모듈 범위

- 6DOF motion RAO 기반 response spectrum
- 수선면 중심 RAO에서 COG 및 local point motion 변환
- RMS, spectral moments, significant amplitude
- short-term MPM, expected maximum, exceedance probability
- long-term annual return value
- velocity / acceleration spectrum
- relative wave elevation, air gap, deck wetness probability
- FLNG-LNGC side-by-side relative motion
- stochastic equivalent linear roll damping helper
- operability / downtime evaluation

v1에서는 stress RAO, load RAO, mooring RAO, riser RAO, second-order QTF는 제외합니다. 이런 응답은 추후 별도 모듈로 붙이는 구조가 적합합니다.

## 실행

.NET 8 SDK가 설치된 환경에서:

```powershell
dotnet run --project flng/samples/FpsoFrequencyDomain.Sample
dotnet run --project flng/tests/FpsoFrequencyDomain.Tests
```

현재 작업 PC에는 .NET runtime만 있고 SDK가 없어 local build 검증은 실행하지 못했습니다.

## RAO 기준

좌표계는 `x` forward, `y` port, `z` up입니다. Translational RAO는 `m/m`, rotational RAO는 `rad/m`를 사용합니다.

FLNG RAO가 수선면 배 중심 기준이면 COG 변환은 다음처럼 적용합니다.

```csharp
var cogFromWaterlineCentre = new BodyPoint(0.0, 0.0, -12.0);
var cogRao = waterlineCentreRao.TranslateReferenceTo(cogFromWaterlineCentre);
```

LNGC가 우현에 붙은 하역 상태는 두 RAO 모델을 입력으로 넣고 두 local point의 complex RAO 차이를 계산합니다. Shielding effect나 hydrodynamic interaction은 RAO 생성 단계에서 반영한 파일을 넣으면 됩니다.
