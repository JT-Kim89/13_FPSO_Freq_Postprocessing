# FPSO Frequency-Domain Postprocessing

이 저장소는 FPSO의 frequency-domain 후처리를 위한 C# 코드입니다. 기본 입력은 FPSO motion RAO와 wave spectrum이며, 이를 이용해 운동 응답 spectrum, short-term 및 long-term extreme, relative wave, local point motion, air gap, operability 등을 계산합니다.

FLNG/LNGC side-by-side 해석은 기본 FPSO 후처리 기능 위에 추가되는 확장 케이스로 다룹니다. 즉, 메인 대상은 FPSO이고 FLNG 관련 내용은 하단의 side-by-side 확장 항목에서 관리합니다.

## 폴더 구조

```text
fpso/
  src/FpsoFrequencyDomain/              FPSO frequency-domain 계산 라이브러리
  samples/FpsoFrequencyDomain.Sample/   실행 예제
  tests/FpsoFrequencyDomain.Tests/      smoke test

flng/
  README.md                             FLNG/LNGC side-by-side 확장 설명
```

## FPSO 기본 후처리 범위

- 6DOF motion RAO 기반 response spectrum
- 수선면 중심 RAO에서 COG 및 local point motion 변환
- spectral moments `m0`, `m1`, `m2`, `m4`
- RMS, significant single amplitude, significant double amplitude
- short-term MPM, expected maximum, exceedance probability
- long-term exposure 및 annual return value
- velocity / acceleration spectrum
- relative wave elevation
- air gap 및 deck wetness probability
- operability / downtime evaluation
- stochastic equivalent linear roll damping helper

v1에서는 stress RAO, load RAO, mooring RAO, riser RAO, second-order QTF는 제외했습니다. 이런 응답은 추후 별도 response RAO 또는 QTF 모듈로 추가하는 구조가 적합합니다.

## 실행

.NET 8 SDK가 설치된 환경에서 저장소 루트 기준으로 실행합니다.

```powershell
dotnet run --project fpso/samples/FpsoFrequencyDomain.Sample
dotnet run --project fpso/tests/FpsoFrequencyDomain.Tests
```

현재 작업 PC에는 .NET runtime만 있고 SDK가 없어 local build 검증은 실행하지 못했습니다.

## RAO 기준

좌표계는 `x` forward, `y` port, `z` up을 사용합니다. Translational RAO는 `m/m`, rotational RAO는 `rad/m`를 사용합니다.

FPSO RAO가 수선면 배 중심 기준이면 COG 변환은 다음처럼 적용합니다.

```csharp
var cogFromWaterlineCentre = new BodyPoint(0.0, 0.0, -12.0);
var cogRao = waterlineCentreRao.TranslateReferenceTo(cogFromWaterlineCentre);
```

local point가 COG 기준으로 정의되어 있으면 다음처럼 계산합니다.

```csharp
var topsideFromCog = new BodyPoint(65.0, 18.0, 42.0);
var topsideMotion = waterlineCentreRao.AtPointFromCog(cogFromWaterlineCentre, topsideFromCog);
```

## FLNG / LNGC Side-By-Side 확장

FLNG 옆에 LNGC가 접안해 하역하는 상태처럼 두 선체 사이의 relative motion 평가가 필요한 경우, 두 개의 6DOF RAO 모델을 입력으로 넣어 각 local point의 complex RAO 차이를 계산합니다.

```text
relative = LNGC point motion - FLNG point motion
```

Shielding effect나 hydrodynamic interaction은 RAO 생성 단계에서 반영된 파일을 입력하면 됩니다. 후처리 코드는 입력된 RAO의 phase, origin offset, heading 기준을 맞춘 뒤 relative motion spectrum과 MPM을 계산합니다.

자세한 side-by-side 사용법은 [flng/README.md](flng/README.md)를 참고하세요.
