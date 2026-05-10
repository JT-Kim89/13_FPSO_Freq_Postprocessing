# FPSO Frequency-Domain Module

`fpso/`는 이 저장소의 메인 모듈입니다. FPSO motion RAO와 wave spectrum을 받아 motion statistics, local acceleration, relative wave, air gap, operability를 계산하는 C# 라이브러리와 예제를 포함합니다.

FLNG/LNGC side-by-side relative motion은 이 FPSO 후처리 엔진 위에 얹는 확장 기능으로 포함되어 있습니다.

## 구성

```text
src/FpsoFrequencyDomain/              계산 라이브러리
samples/FpsoFrequencyDomain.Sample/   예제 실행 프로젝트
tests/FpsoFrequencyDomain.Tests/      test framework 없는 smoke test
```

## 실행

저장소 루트에서 실행할 때:

```powershell
dotnet run --project fpso/samples/FpsoFrequencyDomain.Sample
dotnet run --project fpso/tests/FpsoFrequencyDomain.Tests
```

`fpso/` 폴더 안에서 실행할 때:

```powershell
dotnet run --project samples/FpsoFrequencyDomain.Sample
dotnet run --project tests/FpsoFrequencyDomain.Tests
```

## 주요 입력

- frequency array
- wave spectrum, 또는 `Hs`, `Tp`, `gamma`로 생성한 PM/JONSWAP spectrum
- FPSO 6DOF complex motion RAO
- RAO reference origin에서 COG 및 local point까지의 좌표
- short-term duration, 예: 3 hr
- long-term sea-state probability
- operability criteria

## 주요 출력

- 6DOF displacement response spectrum
- local point displacement / velocity / acceleration spectrum
- spectral moments, RMS, significant amplitude
- short-term MPM / expected maximum
- long-term return value
- relative wave elevation 및 air gap
- operability / downtime

## Side-By-Side 확장

두 번째 선체 RAO가 있는 경우 `TwoBodyRelativeMotionAnalyzer`로 두 local point의 상대 운동을 계산할 수 있습니다. 이 기능은 FLNG-LNGC side-by-side 하역 해석에 사용합니다.
