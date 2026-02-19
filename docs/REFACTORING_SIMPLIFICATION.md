# PhotoshopToUnity 코드 단순화 리팩토링

## 현재 상황 분석

### 현재 코드 구조
- **파일 크기**: 783줄
- **레이어 타입**: 9가지 (Button, IconImage, DecoImage, UIImage, BGImage, ItemImage, InnerImage, Txt, None)
- **실제 필요**: 2가지 (Txt, Image)
- **불필요 코드**: 약 ~60% 추정

### 현재 레이어 타입별 처리

```csharp
enum LayerType
{
    None,              // ❌ 불필요
    Button,            // ❓ 이미지 처리와 동일
    IconImage,         // ❌ 불필요
    DecoImage,         // ❌ 불필요
    UIImage,           // ❌ 불필요
    BGImage,           // ❌ 불필요
    ItemImage,         // ❌ 불필요
    InnerImage,        // ❌ 불필요
    Txt,               // ✅ 필요
}
```

---

## 단순화 전략

### 레이어 타입 축소
```csharp
// Before: 9가지 타입
public enum LayerType { None, Button, IconImage, DecoImage, UIImage, BGImage, ItemImage, InnerImage, Txt }

// After: 2가지 타입
public enum LayerType { Txt, Image }
```

### 주요 변경사항

#### 1. Switch 문 단순화

**Before** (약 180줄):
```csharp
switch (layerType)
{
    case LayerType.Txt:
        // 120줄 텍스트 처리
        break;
    case LayerType.Button:
        // 이미지 처리
        break;
    case LayerType.IconImage:
        // 이미지 처리 (중복)
        break;
    case LayerType.DecoImage:
        // 이미지 처리 (중복)
        break;
    // ... 더 많은 중복 ...
    default:
        // 이미지 처리
        break;
}
```

**After** (약 30줄):
```csharp
switch (layerType)
{
    case LayerType.Txt:
        ApplyTextLayer(child, targetSprite, targetImportLayerData, datas, folders, layerName);
        break;
    default:  // Image
        ApplyImageLayer(child, targetSprite, psdLayer, rectTransform, x, y);
        break;
}
```

---

## 세부 구현 방안

### 1. 단순화된 LayerType

```csharp
public enum LayerType
{
    Txt,     // txt_, stxt_
    Image,   // 나머지 모든 레이어
}
```

### 2. 단순화된 GetLayerType()

**Before**:
```csharp
public static LayerType GetLayerType(string layerName, int inQueueCount = 1)
{
    string name = layerName.ToUpper();

    if ((name.Contains(PrefixList[(int)LayerType.Txt]) || name.Contains("STXT_")) && inQueueCount > 4)
    {
        return LayerType.Txt;
    }
    else if (name.Contains(PrefixList[(int)LayerType.Button]))
    {
        return LayerType.Button;
    }
    // ... 더 많은 조건 ...
    return LayerType.None;
}
```

**After**:
```csharp
public static LayerType GetLayerType(string layerName, int inQueueCount = 1)
{
    string name = layerName.ToUpper();

    // txt_ 또는 stxt_로 시작하고 충분한 데이터가 있으면 텍스트
    if ((name.Contains("TXT_") || name.Contains("STXT_")) && inQueueCount > 4)
    {
        return LayerType.Txt;
    }

    // 나머지는 모두 이미지
    return LayerType.Image;
}
```

### 3. PrefixList 제거 가능

**Before**:
```csharp
public static List<string> PrefixList = new List<string>()
{
    "IMG_",      // ❌ 사용 안함
    "BTN_",      // ❌ 사용 안함
    "ICON_",     // ❌ 사용 안함
    "DECO_",     // ❌ 사용 안함
    "UI_",       // ❌ 사용 안함
    "BG_",       // ❌ 사용 안함
    "ITEM_",     // ❌ 사용 안함
    "INNER_",    // ❌ 사용 안함
    "TXT_",      // ✅ 사용
};
```

**After** (완전 제거):
```csharp
// PrefixList 제거 - 상수로 변경
private const string TEXT_PREFIX = "TXT_";
private const string STXT_PREFIX = "STXT_";
```

### 4. 텍스트 처리 메서드 추출

```csharp
private static void ApplyTextLayer(
    GameObject child,
    Sprite targetSprite,
    ImportLayerData targetImportLayerData,
    Queue<string> datas,
    string[] folders,
    string layerName)
{
    if (datas.Count <= 4) return;

    var targetText = child.AddComponent<RBTextMeshProUGUI>();

    string fontName = datas.Dequeue();
    int fontSize = Mathf.RoundToInt(float.Parse(datas.Dequeue()));
    string fontText = datas.Dequeue();
    string fontColor = datas.Dequeue();

    // 텍스트 설정
    targetText.fontSize = fontSize;
    targetText.enableAutoSizing = true;
    targetText.fontSizeMin = 1;
    targetText.fontSizeMax = fontSize;
    targetText.text = fontText.Replace("<br>", "\n");
    targetText.alignment = TextAlignmentOptions.Center;
    ColorUtility.TryParseHtmlString(fontColor, out Color newCol);
    targetText.color = newCol;

    // 폰트 로드
    LoadTextFont(targetText, fontName);

    // 사이즈 조정
    var rect = targetText.GetComponent<RectTransform>();
    int newLineCount = fontText.Split(new[] { "<br>" }, System.StringSplitOptions.None).Length;
    rect.sizeDelta = new Vector2(rect.sizeDelta.x * 1.1f, (fontSize * 1.5f) * newLineCount);

    // 스트로크, 드롭섀도우 등 처리
    ApplyTextEffects(child, datas);

    // 임포트된 PNG 제거
    RemoveTextLayer PNG(targetImportLayerData, folders, layerName);
}

private static void ApplyImageLayer(
    GameObject child,
    Sprite targetSprite,
    PsdLayer psdLayer,
    RectTransform rectTransform,
    float x,
    float y)
{
    var targetImage = child.AddComponent<UnityEngine.UI.Image>();
    targetImage.sprite = targetSprite;
    targetImage.type = UnityEngine.UI.Image.Type.Simple;

    // 나인슬라이스 체크
    if (targetSprite.border != Vector4.zero)
    {
        targetImage.type = UnityEngine.UI.Image.Type.Sliced;
        rectTransform.sizeDelta = new Vector2(psdLayer.Width, psdLayer.Height);
        x = (x - rectTransform.rect.size.x * 0.5f);
        y = (y + rectTransform.rect.size.y * 0.5f);
    }

    // 투명도 적용
    if (psdLayer.Opacity != 1.0f)
    {
        Color tempColor = targetImage.color;
        tempColor.a = psdLayer.Opacity;
        targetImage.color = tempColor;
    }
}
```

---

## 코드 라인 비교

| 항목 | Before | After | 감소율 |
|------|--------|-------|--------|
| PhotoshopToUnity.cs | 783줄 | ~300줄 | ~62% |
| LayerType 정의 | 10줄 | 3줄 | ~70% |
| GetLayerType() | 30줄 | 8줄 | ~73% |
| CreatePopupChild() | 180줄 | 50줄 | ~72% |
| PrefixList | 13줄 | 0줄 | 제거 |

---

## 예상 효과

✅ **코드 가독성 증가** (62% 축소)
✅ **유지보수성 개선** (명확한 텍스트/이미지 구분)
✅ **버그 위험 감소** (중복 코드 제거)
✅ **신규 기능 추가 용이** (메서드 분리)
✅ **테스트 용이성** (작은 메서드)

---

## 구현 순서

1. **Phase 2-1**: LayerType 단순화
   - enum 수정
   - GetLayerType() 단순화
   - PrefixList 제거

2. **Phase 2-2**: 메서드 추출
   - ApplyTextLayer() 생성
   - ApplyImageLayer() 생성
   - ApplyTextEffects() 생성

3. **Phase 2-3**: CreatePopupChild() 리팩토링
   - switch 문 단순화
   - 메서드 호출로 대체

4. **Phase 2-4**: 테스트 및 검증
   - 기능 동작 확인
   - 성능 측정

---

## 마이그레이션 영향도

| 항목 | 영향도 | 설명 |
|------|--------|------|
| Photoshop 스크립트 | 없음 | PSD 레이어명 규칙 동일 |
| 기존 프리펩 | 없음 | 새로 생성된 프리펩만 영향 |
| 런타임 동작 | 없음 | 기능 동작 동일 |
| 설정 파일 | 없음 | 변경 없음 |

---

## 보존할 기능

✅ 텍스트 레이어 처리 (스트로크, 드롭섀도우)
✅ 이미지 처리 (나인슬라이스)
✅ 폰트 로딩
✅ 폴더 구조 생성
✅ 프리펩 저장

---

## 선택사항: 완전 단순화 (극단적)

레이어 타입 구분 없이 더 단순하게:

```csharp
// 텍스트 데이터가 충분하면 텍스트, 아니면 이미지
if (datas.Count > 4 && IsTextLayer(layerName))
{
    ApplyTextLayer(...);
}
else
{
    ApplyImageLayer(...);
}
```

---

**이 단순화를 적용하시겠습니까?**
