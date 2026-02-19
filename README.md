# PhotoshopToUnity

PSD 파일을 Unity UI Prefab으로 자동 변환해주는 에디터 툴입니다.

---

## 목차

1. [요구 사항](#요구-사항)
2. [전체 흐름](#전체-흐름)
3. [1단계 — Photoshop 스크립트 설치](#1단계--photoshop-스크립트-설치)
4. [2단계 — Unity 설정](#2단계--unity-설정)
5. [3단계 — Photoshop에서 레이어 준비](#3단계--photoshop에서-레이어-준비)
6. [4단계 — Photoshop 스크립트 실행](#4단계--photoshop-스크립트-실행)
7. [5단계 — Unity에서 Prefab 생성](#5단계--unity에서-prefab-생성)
8. [레이어 명명 규칙](#레이어-명명-규칙)
9. [Settings 필드 설명](#settings-필드-설명)
10. [자주 묻는 질문](#자주-묻는-질문)

---

## 요구 사항

| 항목 | 버전 |
|---|---|
| Adobe Photoshop | 2021 (v22) 이상 (권장: 2026 / v27.3.1) |
| Unity | 2020.3 LTS 이상 |
| TextMeshPro | Unity 패키지 포함 |

---

## 전체 흐름

```
[Photoshop]                         [Unity]
PSD 파일 열기
    ↓
스크립트 실행 (레이어명 자동 정리)
    ↓
PSD 저장
    ↓
                                    PSD 파일을 Assets에 복사
                                         ↓
                                    PSD 우클릭
                                    → Assets/Tools/Create (PhotoshopToUnity)
                                         ↓
                                    Sprite 추출 + Prefab 자동 생성
```

---

## 1단계 — Photoshop 스크립트 설치

### 파일 복사

`PhotoshopScripts/PhotoshopToUnity_v3.jsx` 파일을 아래 경로에 복사합니다.

```
C:\Program Files\Adobe\Adobe Photoshop 2026\Presets\Scripts\
```

> 다른 Photoshop 버전을 사용하는 경우 폴더명의 연도만 맞게 변경하세요.
> 예: `Adobe Photoshop 2024`, `Adobe Photoshop 2025`

### 확인

Photoshop을 재시작한 뒤 메뉴에서 확인합니다.

```
File > Scripts > PhotoshopToUnity_v3
```

---

## 2단계 — Unity 설정

### Settings 에셋 위치

```
Assets/Resources/PhotoshopToUnitySettings.asset
```

없을 경우 Unity 메뉴에서 자동 생성합니다.

```
BaliGames > Framework > PhotoshopToUnity > CreateSettingsAsset
```

### Settings 설정

Project 창에서 `PhotoshopToUnitySettings` 에셋을 선택하면 Inspector에 아래 항목이 표시됩니다.

| 필드 | 설명 | 예시 |
|---|---|---|
| **Referance Resolution** | 기준 해상도 (Canvas 크기) | `720 x 1280` |
| **Preset** | 생성될 Prefab의 베이스 GameObject | `PresetPopup` |
| **Save Path** | Prefab/Sprite 저장 경로 (`{0}` = PSD 파일명) | `Assets/SamplePsd/{0}` |
| **Parent Game Object Name** | 레이어가 삽입될 부모 오브젝트 이름 | `Background` |
| **Is Folder** | PSD 그룹(폴더)을 빈 GameObject 계층으로 생성할지 여부 | `true` |
| **Common Path** | 공용 이미지 경로 (참조용) | `Assets/Resources/Common/PackSources` |

설정 후 **저장하기** 버튼을 클릭합니다.

### Preset Prefab 구조

`Preset` 필드에 연결된 Prefab은 반드시 아래 구조를 포함해야 합니다.

```
PresetPopup (GameObject)
└── Background (RectTransform)   ← ParentGameObjectName 에 지정된 이름
```

> 생성된 모든 레이어 오브젝트는 `Background` 하위에 추가됩니다.

### 폰트 설정

텍스트 레이어(`txt_`)는 아래 경로에서 폰트를 자동으로 탐색합니다.

```
Assets/Resources/Fonts/Editor/
```

지원 폰트 (파일명에 해당 문자열 포함):

| Photoshop 폰트명 | 적용되는 TMP 폰트 에셋명 |
|---|---|
| `RIFFIC` 포함 | `RIFFICFREE-BOLD SDF` |
| 그 외 | `NotoSans-Bold SDF` |

---

## 3단계 — Photoshop에서 레이어 준비

### 레이어 명명 규칙 적용

스크립트를 실행하기 전에 레이어명을 아래 규칙에 맞게 정리합니다.

| 접두사 | 용도 |
|---|---|
| `btn_` | 버튼 |
| `icon_` | 아이콘 이미지 |
| `deco_` | 데코 이미지 |
| `img_` | UI 이미지 |
| `bg_` | 배경 / 타이틀 이미지 |
| `inner_` | 팝업 이너 이미지 |
| `item_` | 아이템 이미지 |
| `txt_` | TextMeshPro 텍스트 (자동 생성) |

> **중요:** 텍스트 레이어(`LayerKind.TEXT`)는 스크립트가 자동으로 `txt_` 접두사와 데이터를 붙여줍니다.
> 이미지 레이어는 사용자가 직접 접두사를 붙여야 합니다.

### PSD 파일명 규칙

- 파일명에 **공백(space) 포함 불가**
- 예: `popup_reward.psd` (O) / `popup reward.psd` (X)

---

## 4단계 — Photoshop 스크립트 실행

1. 변환할 PSD 파일을 Photoshop에서 엽니다.
2. 메뉴에서 스크립트를 실행합니다.
   ```
   File > Scripts > PhotoshopToUnity_v3
   ```
3. 안내 다이얼로그에서 **OK**를 클릭합니다.
4. 스크립트가 모든 텍스트 레이어를 자동으로 처리합니다.
   - 레이어명에 폰트명, 크기, 내용, 색상 데이터가 `^` 구분자로 추가됩니다.
   - 스트로크 / 드롭섀도우가 있으면 함께 기록됩니다.
5. PSD 파일을 저장합니다.

> 스크립트는 이미 처리된 레이어(`txt_` 또는 `stxt_` 포함)는 건너뜁니다.
> 반복 실행해도 안전합니다.

---

## 5단계 — Unity에서 Prefab 생성

1. 처리된 PSD 파일을 Unity 프로젝트의 `Assets` 폴더에 복사합니다.
2. Unity Project 창에서 PSD 파일을 **우클릭**합니다.
3. 컨텍스트 메뉴에서 선택합니다.
   ```
   Assets > Tools > Create (PhotoshopToUnity)
   ```
4. 자동으로 아래 작업이 진행됩니다.
   - PSD에서 Sprite 추출 (Save Path 경로에 PNG 저장)
   - 텍스트 레이어의 PNG는 자동으로 삭제됨
   - Preset을 기반으로 Prefab 생성
   - 각 레이어가 `RectTransform`으로 배치됨
   - 텍스트 레이어에 `RBTextMeshProUGUI` 컴포넌트 자동 부착
5. 생성된 Prefab이 `Save Path`에 저장되고 Scene에 배치됩니다.

### 다중 PSD 처리

여러 PSD 파일을 동시에 선택 후 우클릭해도 순서대로 자동 처리됩니다.

---

## 레이어 명명 규칙

### 이미지 레이어

레이어명에 아무 특수문자 없이 접두사만 붙이면 됩니다.

```
btn_close
icon_coin
bg_main
```

### 텍스트 레이어

스크립트가 자동으로 아래 형식으로 레이어명을 변경합니다.

```
txt_{레이어명}^{폰트명}^{폰트크기}^{텍스트내용}^{색상}^{스트로크크기}^{스트로크색상}^{섀도우각도}^{섀도우거리}^{섀도우투명도}^{섀도우색상}
```

**예시:**

```
txt_title^NotoSansCJKkr-Bold^48^보상 획득^#FFFFFF^null^null^null^null^null^null
```

| 항목 | 설명 |
|---|---|
| 줄바꿈 | Photoshop 내 줄바꿈은 `<br>`로 저장, Unity에서 `\n`으로 변환 |
| 스트로크 없음 | `null^null` |
| 드롭섀도우 없음 | `null^null^null^null` |

### 그룹(폴더) 레이어

`Is Folder`가 활성화된 경우 PSD의 그룹은 빈 `GameObject` 계층으로 생성됩니다.

```
[PSD 그룹 구조]          [Unity 계층 구조]
Background               Background
└── reward_group    →    └── reward_group (RectTransform)
    ├── icon_coin            ├── icon_coin (Image)
    └── txt_amount           └── txt_amount (RBTextMeshProUGUI)
```

---

## Settings 필드 설명

### Save Path

`{0}`은 PSD 파일명으로 자동 치환됩니다.

```
Assets/SamplePsd/{0}
```

`popup_reward.psd` → `Assets/SamplePsd/popup_reward/`에 저장

### Referance Resolution

Photoshop Canvas와 Unity Canvas의 기준 해상도를 일치시켜야 위치가 정확합니다.

```
예: Photoshop 문서 크기가 720x1280 → Unity Canvas도 720x1280
```

### Parent Game Object Name

생성된 레이어 오브젝트가 삽입될 부모 오브젝트의 이름입니다.
Preset Prefab 내부에 반드시 해당 이름의 오브젝트가 존재해야 합니다.

---

## 자주 묻는 질문

**Q. `PhotoshopToUnitySettings 이 null 입니다.` 에러가 납니다.**
A. `BaliGames > Framework > PhotoshopToUnity > CreateSettingsAsset` 메뉴로 에셋을 생성하세요.

**Q. `Preset 이 null 입니다.` 에러가 납니다.**
A. Settings 에셋의 `Preset` 필드에 베이스 Prefab을 연결하세요.

**Q. PSD 파일명에 공백이 있으면 안 된다고 나옵니다.**
A. PSD 파일명의 공백을 `_`로 변경 후 다시 시도하세요.

**Q. 텍스트가 Unity에 나타나지 않습니다.**
A. `Assets/Resources/Fonts/Editor/` 경로에 TMP 폰트 에셋이 있는지 확인하세요.

**Q. 위치가 어긋납니다.**
A. Photoshop 문서 크기와 Unity `Canvas > Canvas Scaler > Reference Resolution`이 동일한지 확인하세요.

**Q. 같은 텍스트 레이어에 스크립트를 두 번 실행했습니다.**
A. `txt_` 또는 `stxt_`가 포함된 레이어는 자동으로 건너뛰므로 안전합니다.
