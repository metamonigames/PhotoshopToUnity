# UniTask를 활용한 PhotoshopToUnity 최적화 가이드

## 개요
프로젝트에 UniTask가 이미 설치되어 있어, PhotoshopToUnity 스크립트를 비동기 처리로 최적화할 수 있습니다.

---

## 현재 코드의 문제점

### 1. Thread.Sleep(10) - UI 블로킹
**파일**: `Assets/Editor/PhotoshopToUnity/PhotoshopToUnity.cs`, Line 457

```csharp
// ❌ 문제: UI 스레드 블로킹, 에디터 응답 불가
Thread.Sleep(10);
```

**영향**:
- 에디터가 멈춤 (약간이지만 누적되면 심각)
- 게임 객체 계층 구조 업데이트 지연
- 사용자 입력 무시

---

## UniTask를 활용한 해결방법

### 1. Yield를 통한 UI 응답성 개선

**개선 안 1: UniTask.Yield() 사용** (권장)
```csharp
// ✅ 개선: UI 스레드 양보
await UniTask.Yield();
```

**장점**:
- 최소 지연 시간 (한 프레임)
- 다른 UI 업데이트 허용
- 가장 간단한 구현

---

### 2. Delay를 통한 명시적 대기

**개선 안 2: UniTask.Delay() 사용**
```csharp
// 명시적 10ms 대기
await UniTask.Delay(10);
```

**사용 시기**:
- 특정 시간만큼 대기 필요할 때
- 시스템 리소스 정리 시간 필요할 때

---

## 구현 방법

### 방법 1: CreatePopupChild 메서드 비동기화

**현재 코드**:
```csharp
private static void CreatePopupChild(
    Action<int, GameObject, List<Sprite>, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer>, List<ImportLayerData>> inCallback,
    int inIndex, GameObject inParentGameObject, List<Sprite> inSprites, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
    List<ImportLayerData> inImportLayerDatas)
{
    // ... 코드 ...

    Thread.Sleep(10);  // ❌ 블로킹

    // ... 계속 ...
}
```

**개선 안**:
```csharp
private static async UniTask CreatePopupChildAsync(
    Func<int, GameObject, List<Sprite>, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer>, List<ImportLayerData>, UniTask> inCallback,
    int inIndex, GameObject inParentGameObject, List<Sprite> inSprites, List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers,
    List<ImportLayerData> inImportLayerDatas)
{
    // ... 코드 ...

    await UniTask.Yield();  // ✅ UI 응답 유지

    // ... 계속 ...

    await inCallback?.Invoke(nextIndex, inParentGameObject, inSprites, inPsdLayers, inImportLayerDatas);
}
```

---

### 방법 2: OnCallback 메서드 업데이트

**현재 코드**:
```csharp
private static void OnCallback(int inIndex, GameObject inPresetGameObject, List<Sprite> inSprites,
    List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers, List<ImportLayerData> inImportLayerDatas)
{
    if (inIndex < 0)
    {
        // ... 프리펩 저장 ...
    }
    else
    {
        CreatePopupChild(OnCallback, inIndex, ...);
    }
}
```

**개선 안**:
```csharp
private static async UniTask OnCallbackAsync(int inIndex, GameObject inPresetGameObject, List<Sprite> inSprites,
    List<SubjectNerd.PsdImporter.PsdParser.PsdLayer> inPsdLayers, List<ImportLayerData> inImportLayerDatas)
{
    if (inIndex < 0)
    {
        // ... 프리펩 저장 ...
    }
    else
    {
        await CreatePopupChildAsync(OnCallbackAsync, inIndex, ...);
    }
}
```

---

## EditorCoroutineRunner 대체 방안

현재 사용 중인 EditorCoroutineRunner:
```csharp
EditorCoroutineRunner.KillAllCoroutines();
```

**UniTask 대체**:
```csharp
// 모든 비동기 작업 자동 정리
// (UniTask는 스코프를 벗어나면 자동 정리)
```

---

## 구현 체크리스트

- [ ] `Cysharp.Threading.Tasks` using 추가
- [ ] `CreatePopupChild` 메서드를 `CreatePopupChildAsync`로 변경
- [ ] `OnCallback` 메서드를 `OnCallbackAsync`로 변경
- [ ] `Thread.Sleep(10)` → `await UniTask.Yield()` 변경
- [ ] 모든 재귀 호출을 `await` 추가
- [ ] `EditorCoroutineRunner` 사용 줄이기
- [ ] 테스트 및 검증

---

## 성능 비교

| 항목 | Thread.Sleep(10) | UniTask.Yield() |
|------|-----------------|-----------------|
| UI 블로킹 | ❌ 예 | ✅ 아니오 |
| 메모리 할당 | 높음 | 낮음 |
| GC 부담 | 높음 | 낮음 |
| 에디터 응답성 | 낮음 | 높음 |
| 프리펩 생성 속도 | 약간 더 느림 | 비슷 |

---

## 추가 최적화 사항

### 1. 배치 처리
```csharp
// 여러 레이어를 한 번에 처리
private static async UniTask CreateMultipleLayersAsync(List<Sprite> sprites)
{
    for (int i = 0; i < sprites.Count; i++)
    {
        await ProcessLayerAsync(sprites[i]);

        // 매 N개마다 UI 응답성 회복
        if (i % 10 == 0)
            await UniTask.Yield();
    }
}
```

### 2. 타임아웃 설정
```csharp
using var cts = new System.Threading.CancellationTokenSource();
cts.CancelAfterSlim(System.TimeSpan.FromSeconds(30));

try
{
    await CreatePopupChildAsync(...).AttachExternalCancellation(cts.Token);
}
catch (OperationCanceledException)
{
    Debug.LogError("프리펩 생성 타임아웃");
}
```

---

## 참고자료

- [UniTask GitHub](https://github.com/Cysharp/UniTask)
- [UniTask Documentation](https://github.com/Cysharp/UniTask#readme)
- [에디터 스크립트 비동기 처리](https://docs.unity3d.com/Manual/asynchronous-work-in-editor.html)

---

## 예상 효과

✅ 에디터 응답성 개선
✅ 메모리 할당 감소
✅ GC 부담 감소
✅ 더 부드러운 프리펩 생성 경험
