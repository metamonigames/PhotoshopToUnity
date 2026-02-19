# PhotoshopToUnity 마이그레이션 가이드

## txt_ → RBTextMeshProUGUI 통일 (Phase 1 완료)

### 변경 사항

#### 1. 레이어 타입 통합
- **이전**: `txt_` (RBText), `stxt_` (RBTextMeshProUGUI) 두 가지 사용
- **현재**: `txt_` (RBTextMeshProUGUI) 단일 타입으로 통합

#### 2. Photoshop 스크립트 변경
**파일**: `PhotoshopScripts/PhotoshopToUnity_v3.jsx`

변경 내용:
- `stxt_` 레이어 처리 제거
- 모든 TEXT 레이어를 `txt_`로 통일
- 중복 코드 제거 (약 100줄 감소)
- 헬프 텍스트 업데이트

#### 3. Unity 에디터 스크립트 변경
**파일**: `Assets/Editor/PhotoshopToUnity/PhotoshopToUnity.cs`

변경 내용:
- `LayerType.STxt` 제거
- `LayerType.Txt`를 RBTextMeshProUGUI로 변경
- `GetLayerType()` 메서드에서 `stxt_` 호환성 추가 (자동 변환)

### 마이그레이션 전략

#### 기존 프로젝트의 경우:
1. 이전 버전으로 생성된 PSD 파일에서 `stxt_`로 시작하는 레이어가 있는지 확인
2. 새로운 버전에서 PSD를 임포트하면 자동으로 `txt_` 타입으로 인식됨
3. 생성된 프리펩에서 텍스트 컴포넌트가 `RBTextMeshProUGUI`로 설정됨

#### 호환성:
- `stxt_` 레이어명을 가진 파일도 자동으로 `txt_`로 변환
- 기존 프리펩 재생성 시 새로운 형식으로 업데이트 가능

### 영향 범위

| 항목 | 영향도 | 설명 |
|------|--------|------|
| Photoshop 스크립트 | 높음 | 텍스트 레이어명 자동 변경 |
| Unity 에디터 | 높음 | 프리펩 생성 시 컴포넌트 변경 |
| 런타임 | 없음 | RBTextMeshProUGUI와 RBText 동작 동일 |
| 기존 프리펩 | 없음 | 기존 프리펩은 변경 없음 |

### 테스트 체크리스트

- [ ] Photoshop에서 스크립트 실행하여 레이어명 변경 확인
- [ ] txt_ 레이어가 올바르게 생성되었는지 확인
- [ ] Unity에서 PSD 임포트하여 프리펩 생성
- [ ] 생성된 프리펩의 텍스트 컴포넌트가 RBTextMeshProUGUI인지 확인
- [ ] 폰트 로딩 및 텍스트 렌더링 확인
- [ ] Stroke, DropShadow 등 텍스트 효과 적용 확인

### 롤백 방법

이전 버전으로 돌아가야 하는 경우:
1. Photoshop 스크립트: `PhotoshopToUnity_v2.jsx` 사용 (별도 보관)
2. Unity 스크립트: 이전 커밋으로 되돌리기 (git revert)
3. 새로 생성된 프리펩은 직접 삭제

---

## 다음 단계 (Phase 2, 3)

### Phase 2: 코드 리팩토링
- 중복 코드 제거
- 로깅 개선
- 예외 처리 강화

### Phase 3: 문서화 및 기능 추가
- 개발자 문서 작성
- 사용자 가이드 작성
- 새로운 레이어 타입 지원
