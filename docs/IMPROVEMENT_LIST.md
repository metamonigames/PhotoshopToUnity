# PhotoshopToUnity 개선 사항 목록

## 프로젝트 개요
PSD 파일을 Unity 프리펩으로 변환하는 시스템입니다.
- Photoshop 스크립트로 레이어 정리 및 이름 변경
- Unity 에디터 스크립트로 PSD 임포트 및 프리펩 생성

---

## 긴급 개선 사항 (Priority: HIGH)

### 1. txt_ 레이어를 RBTextMeshProUGUI로 통일
**파일**: `Assets/Editor/PhotoshopToUnity/PhotoshopToUnity.cs`
**현재 상태**:
- `txt_` → `RBText` 컴포넌트 사용
- `stxt_` → `RBTextMeshProUGUI` 컴포넌트 사용

**요청사항**: `txt_` 및 모든 텍스트는 `RBTextMeshProUGUI`로만 사용

**변경 예정**:
- Line 467-585: `LayerType.Txt` 처리 로직 → `RBTextMeshProUGUI` 사용으로 변경
- `stxt_` 레이어 처리 제거 또는 통합
- 기존 `txt_` 레이어명을 새로운 규칙으로 마이그레이션

---

## 코드 개선 사항 (Priority: MEDIUM)

### 2. 중복 코드 제거
**파일**: `Assets/Editor/PhotoshopToUnity/PhotoshopToUnity.cs`

**문제점**:
- `LayerType.Txt` (Line 467-585)와 `LayerType.STxt` (Line 588-705) 처리 로직이 대부분 동일
- 폰트 설정, 스트로크, 드롭섀도우 처리 코드 중복

**개선안**:
- 공통 텍스트 처리 메서드 추출: `ApplyTextLayerSettings()`
- 폰트 로딩 로직 분리: `LoadFont()`
- 스트로크 적용 로직 분리: `ApplyStroke()`
- 드롭섀도우 적용 로직 분리: `ApplyDropShadow()`

---

### 3. Photoshop 스크립트 개선
**파일**: `PhotoshopScripts/PhotoshopToUnity_v3.jsx`

#### 3-1. 레이어명 길이 제한 개선
- Line 369: `MAX_LAYER_NAME_LEN = 15` → 더 유연한 설정 필요
- 포토샵에서 자동으로 잘리는 레이어명에 대한 경고 추가

#### 3-2. 한글 변환 함수 최적화
- `convertKorToEng()` 함수 (Line 362-436) 성능 개선
- 결과 캐싱 메커니즘 추가
- 엣지 케이스 처리 (연속된 자음, 모음 등)

#### 3-3. 에러 핸들링 개선
- 텍스트 레이어 없음 경고
- 레이어명 유효성 검증 강화
- 스크립트 실행 전 PSD 상태 점검

---

### 4. Unity 에디터 스크립트 개선
**파일**: `Assets/Editor/PhotoshopToUnity/PhotoshopToUnity.cs`

#### 4-1. UniTask를 활용한 비동기 처리 개선 (NEW!)
- Line 457: `Thread.Sleep(10)` → `await UniTask.Yield()` 또는 `await UniTask.Delay(10)`
- UI 스레드 블로킹 제거로 에디터 응답성 개선
- EditorCoroutineRunner 최소화
- 구현 예시:
  ```csharp
  // 기존: Thread.Sleep(10) - UI 블로킹
  // 개선: await UniTask.Yield() - UI 응답 유지
  await UniTask.Yield();
  ```

#### 4-2. 로깅 개선
- Line 310: 현재 처리 중인 레이어명 로그 추가
- 스프라이트 로딩 실패 경고
- 폰트 로딩 실패 경고

#### 4-3. 설정 검증 강화
- Line 116-125: `PhotoshopToUnitySettings` 검증 개선
- 필수 폰트 존재 여부 확인
- `_settings.Preset` 유효성 검증

#### 4-4. 예외 처리 개선
- UniTask 기반 비동기 처리로 예외 안정성 향상
- 파일 삭제 실패 시 예외 처리
- 프리펩 저장 실패 처리

---

## 기능 추가 사항 (Priority: LOW)

### 5. 새로운 레이어 타입 지원
- `anim_` → 애니메이션 레이어
- `custom_` → 커스텀 컴포넌트
- 플러그인 시스템으로 확장 가능하게 설계

### 6. 프리펩 생성 후 처리
- 생성된 프리펩 자동 선택
- 생성 완료 알림
- 로그 요약 리포트 생성

### 7. 설정 UI 개선
- `PhotoshopToUnitySettings` GUI 개선
- 폰트 프리뷰
- 레이어 타입별 처리 규칙 시각화

---

## 문서화 (Priority: MEDIUM)

### 8. 개발자 문서
- 레이어 네이밍 규칙 상세 설명
- 새로운 레이어 타입 추가 방법
- 폰트 설정 가이드
- PSD 파일 준비 체크리스트

### 9. 사용자 가이드
- 스크립트 실행 순서 명확화
- 자주 발생하는 오류와 해결법
- 성능 최적화 팁

---

## 마이그레이션 계획

### Phase 1: txt_ → RBTextMeshProUGUI (우선순위: 긴급)
1. PhotoshopToUnity.cs 수정
2. stxt_ 제거 또는 txt_로 통합
3. 기존 PSD 파일 마이그레이션 가이드 작성

### Phase 2: 코드 리팩토링 (우선순위: 높음)
1. 중복 코드 제거
2. 로깅 개선
3. 예외 처리 강화

### Phase 3: 문서화 및 기능 추가 (우선순위: 중간)
1. 개발자 문서 작성
2. 새로운 레이어 타입 지원
3. UI 개선

---

## 진행 상태
- [x] Phase 1: txt_ → RBTextMeshProUGUI 통일 (완료)
  - PhotoshopToUnity.cs: LayerType.STxt 제거, txt_ 레이어를 RBTextMeshProUGUI로 변경
  - PhotoshopToUnity_v3.jsx: 텍스트 처리 로직 통합, stxt_ 제거, txt_만 사용
  - 기존 stxt_ 레이어는 자동으로 txt_로 변환됨
- [ ] Phase 2: 코드 리팩토링
- [ ] Phase 3: 문서화 및 기능 추가
