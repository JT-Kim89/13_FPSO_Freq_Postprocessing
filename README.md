# Ship Motion Dashboard

Python??泥섏쓬 ?ъ슜?섎뒗 ??쒕낫???ㅼ뒿???덉젣?낅땲?? `Streamlit`?쇰줈 ?붾㈃??留뚮뱾怨? ?낅젰 ?곗씠?곕뒗 `SQLite` ?뚯씪(`data/dashboard.db`)????ν빀?덈떎.

## ?곗씠??援ъ“

?댁꽍 ?낅젰??怨꾩링 援ъ“???꾨옒? 媛숈뒿?덈떎.

```text
L, B, D
  -> Loading_Condition: Ballast / FullLoad
    -> T
      -> RAO, MPM, Natural Period, Topside Acceleration
```

利? `T`??`L`, `B`, `D`? ?숇벑???ㅺ퀎蹂?섍? ?꾨땲??`Ballast`, `FullLoad` 議곌굔蹂?draft 媛믪엯?덈떎.

## 沅뚯옣 Python

- Python `3.12` 64-bit 沅뚯옣
- Windows ?ㅼ튂 ??`Add python.exe to PATH` 泥댄겕 沅뚯옣
- ?꾩옱 PC??湲곕낯 `python`??32-bit?대㈃ 理쒖떊 ?곗씠??遺꾩꽍 ?⑦궎吏 ?ㅼ튂媛 ?ㅽ뙣?????덉뒿?덈떎.

## 湲곕뒫

- `Engineering Review` 肄섏뀎???붾㈃: Summary, Limit Check, Wave Spectrum, Response Spectrum, Hydrostatic & Topside
- `L`, `B`, `D` 湲곗? ?꾪꽣留?諛?寃쏀뼢??鍮꾧탳
- `Ballast`, `FullLoad` 議곌굔蹂?寃곌낵 鍮꾧탳
- RAO?먯꽌 `B`, `D`瑜?怨좎젙?섍퀬 `L` 蹂?붾쭔 ??踰덉뿉 蹂대뒗 geometry sweep ?좏깮
- `Motion RAO`, `WaveElev RAO` 二쇳뙆???묐떟 怨≪꽑 ?쒓컖??- MPM, Natural Period, Topside Acceleration ?쒓컖??- `L`, `B`, `D`? 寃곌낵媛??ъ씠??Pearson/Spearman ?곴?遺꾩꽍
- CSV/XLSX ?낅줈????SQLite ???- ?섑뵆 ?곗씠???먮룞 ?앹꽦

## ?ㅽ뻾 諛⑸쾿

```powershell
pip install -r requirements.txt
streamlit run app.py
```

釉뚮씪?곗?媛 ?대━吏 ?딆쑝硫??곕??먯뿉 ?쒖떆?섎뒗 二쇱냼, 蹂댄넻 `http://localhost:8501`濡??묒냽?섎㈃ ?⑸땲??

?대? ?대젮 ?덈뜕 PowerShell?먯꽌 `python`???덉쟾 32-bit Anaconda濡??≫엳硫???PowerShell???닿굅???꾨옒泥섎읆 踰꾩쟾??吏?뺥빐???ㅽ뻾?섏꽭??

```powershell
py -3.12 -m pip install -r requirements.txt
py -3.12 -m streamlit run app.py
```

## C# frequency-domain engine

`src/FpsoFrequencyDomain` contains a .NET 8 C# calculation engine for FPSO
motion RAO + wave spectrum workflows. The v1 scope intentionally excludes
stress RAO, load RAO, mooring RAO, riser RAO, and second-order QTF calculations.

Implemented items:

- 6DOF response spectrum, spectral moments, RMS, significant amplitude
- short-term MPM / expected maximum / exceedance probability
- long-term annual return value from sea-state probabilities
- waterline-centre RAO to COG/local-point transformation
- local point velocity and acceleration spectra
- relative wave elevation spectrum and MPM
- air gap / deck wetness probability from relative wave response
- two-body relative motion for side-by-side FLNG/LNGC offloading
- operability/downtime checks
- stochastic equivalent linear roll damping helper

The RAO coordinate convention is `x` forward, `y` port, `z` up. Translational
RAOs are `m/m`; rotational RAOs are `rad/m`. See
`src/FpsoFrequencyDomain/README.md` for the transformation formula and sample
usage.

```powershell
dotnet run --project samples/FpsoFrequencyDomain.Sample
dotnet run --project tests/FpsoFrequencyDomain.Tests
```

The current PC has the .NET runtime but not the .NET SDK, so these commands need
the .NET 8 SDK installed.

## ?낅젰 ?뚯씪 ?뺤떇

### Design result

```text
L,B,D,Loading_Condition,T,Heave_MPM,Roll_MPM,Pitch_MPM,Heave_Tn,Roll_Tn,Pitch_Tn,Topside_Accx,Topside_Accy,Topside_Accz
320,65,32,Ballast,19.8,6.2,8.4,7.3,11.5,20.5,14.3,0.128,0.158,0.198
320,65,32,FullLoad,24.0,5.7,7.9,6.8,12.0,21.2,14.8,0.118,0.145,0.184
```

### RAO data

RAO??媛숈? 怨꾩링 ?ㅼ씤 `L`, `B`, `D`, `Loading_Condition`, `T`瑜??④퍡 ?ｌ뒿?덈떎.

```text
case_name,L,B,D,Loading_Condition,T,rao_type,dof,frequency,amplitude,phase_deg
Case-320-65-32-Ballast-T19.8,320,65,32,Ballast,19.8,Motion RAO,Heave,0.25,0.83,-92.5
Case-320-65-32-FullLoad-T24.0,320,65,32,FullLoad,24.0,Motion RAO,Heave,0.25,0.74,-88.1
```

## 珥덈낫?먯슜 ?ъ슜 ?쒖꽌

1. ?깆쓣 ?ㅽ뻾?⑸땲??
2. ?쇱そ ?ъ씠?쒕컮?먯꽌 `Add sample data`瑜??꾨쫭?덈떎.
3. `Design Results`, `Correlation`, `RAO` ??쓣 ?뚮윭 洹몃옒?꾨? ?뺤씤?⑸땲??
4. ?ㅼ젣 ?곗씠?곌? ?덉쑝硫?`Data Input` ??뿉??CSV ?먮뒗 XLSX瑜??낅줈?쒗빀?덈떎.
