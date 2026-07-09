# B3DM BoundingVolume 버그 — 근본 원인과 수정 기록 (해결 완료 ✅)

> 2026-07-05 기준. 협재해변 tileset 정상 렌더링 확인 완료.
> 대상 코드: `D:\work\MapPrimeNetV2` (A4L.Mapprime3DNet / A4L.MP3DCore), RH(오른손/OpenGL) 빌드(LEFT_H 미정의).

---

## 증상 (해결 전)

- 협재해변(제주) `tileset.json` 로드 시 화면에 아무것도 렌더링되지 않음.
- `B3dmTilesetTraversal.DetermineFrustumSet(root)` → `false` → 타일 순회/요청 전체 중단.
- 타일 `BoundingVolume.Center`가 원점-상대가 아님 (지구 반지름급 ~6.37×10⁶, 케이스에 따라 8×10¹⁵까지 폭발).
- 단, 콘텐츠(메시) 렌더링 경로는 정상 — **컬링 경로만 고장**.

---

## 전제: MP3DCore Matrix4d의 행렬 관례

`Matrix4d`는 **행-벡터(row-vector, OSG 스타일) 관례**다. 이 사실이 모든 분석의 기준점이었다.

| 항목 | 규칙 | 근거 |
|------|------|------|
| translation 저장 위치 | `[12],[13],[14]` (행4) | `Matrix4d.Translate` |
| 점 변환 | `v' = v·M` → `PreMultiply` / `MultiplyPoint` | `_preMultiplySelf`: 회전은 열과 내적, translation은 [12-14]에서 가산 |
| 변환 합성 | 먼저 적용할 것이 왼쪽 → `자식 * 부모` | `B3dmTile.cs` computedTransform |
| TRS | `S * R * T` | `MathExtensions.TRS` |
| 열-벡터 방향 함수 | `PostMultiply`/`PostMultiply3X3` — translation을 `[3],[7],[11]`에서 읽음 | 이 엔진 행렬에서 그 자리는 항상 0 |

행렬을 **만들고 합성하는 코드는 전부 이 관례로 일관**되어 있었다. 문제는 관례가 어긋난 **입구와 출구** 두 곳이었다.

---

## 근본 원인 — 컬링 경로의 행렬 관례 불일치 2건

### 원인 ① (입구): `GetTransform`이 tileset transform을 전치(transpose) 로드

`B3dmTilesetSchemaExtensions.GetTransform` — 3D Tiles 스펙의 `transform`은 column-major(열-벡터 관례)
배열로 `T[12..14]`가 translation. 기존 코드는 이를 "수학적으로 같은 배치"로 옮기느라 `T[12]`를
Row0-Col3(=`[3]`)에 넣었다.

이 엔진에서는 **flat 배열 순차 복사가 정답**이다: 저장 순서 전치(column-major→row-major)와
관례 전치(열-벡터→행-벡터)가 서로 상쇄되기 때문. 전치 로드의 결과, 타일 transform의 ECEF
translation이 열4(`[3],[7],[11]`)에 들어앉았고, 행4(−WO)와 열4(ECEF)가 공존하는 오염된 행렬이
곱셈 교차항을 만들며 center가 8×10¹⁵까지 폭발했다.

**결정적 로그 증거** (`[XFORM]` 검증 로그):
```
row4T=(0.1, 19642.1, -6371665.0)          ← 정상 자리 (−WO성 translation)
col4=(-3150897.0, 4299833.5, 3490477.8)   ← 0이어야 할 자리에 협재 절대 ECEF!
```

### 원인 ② (출구): `TileOrientedBoundingBox.Transform`이 행렬을 반대 방향으로 적용

`B3dmTileBoundingVolume.cs` — `PostMultiply`(=`M·v`, 열-벡터 방향)를 사용.
translation을 `[3],[7],[11]`(항상 0)에서 읽어 **−worldOrigin 차감이 통째로 소실**되고, 회전은
전치(역회전)로 적용됐다. Left/Right 경로가 **같은 증상**을 보인 이유(둘 다 이 함수를 공유).
바로 아래 `TileBoundingSphere.Transform`은 `MultiplyPoint`(올바름)를 쓰고 있었다 — 이 대비가
이 라이브러리의 의도된 사용법을 보여주는 방증이었다.

### 원인 ③ (①의 워크어라운드 잔재): root transform 이중 주입

`CustomMultiTilesetBehaviour`가 `opts.Translation/Rotation = rootTransform...`으로 스키마 root
transform을 옵션에 복사하고, `B3dmTile.UpdateWorldOriginRight`가 `transform.Position + opts.Translation`으로
또 더하는 구조. ① 버그 동안에는 `GetPosition()`이 0을 반환해(전치 로드 탓) 조용했지만 — 그래서
생긴 워크어라운드로 추정 — ①을 고치면 ECEF가 이중 합산된다.

### 왜 렌더링(메시)은 정상이었나

콘텐츠 배치는 별도 경로다: `computedTransform` **분해값**(Position/Rotation)을 콘텐츠 루트
오브젝트에 넣고(B3dmTile Process), GLB 노드/버텍스는 로컬 좌표로 씬그래프에서 합성된다.
이 경로와 sphere 컬링그룹 경로(`Rot·(c − InvRot·WO)`, 쿼터니언 연산만 사용)는 `Matrix4d`
소비 지점(OBB Transform)을 거치지 않아 무사했다.

---

## 적용된 수정 (전부 컬링 경로, 렌더 경로 무변경)

| # | 파일 | 수정 |
|---|------|------|
| ① | `B3dmTilesetSchemaExtensions.cs` `GetTransform` | 전치 로드 → **flat 배열 순차 복사**. `(float)` 캐스트 제거(ECEF ~3×10⁶은 float에서 미터급 오차) |
| ② | `B3dmTileBoundingVolume.cs` `TileOrientedBoundingBox.Transform` | `PostMultiply`/`PostMultiply3X3` → **`PreMultiply`/`PreMultiply3X3`** (center는 translation 포함 4×4, HalfAxes는 회전만 3×3) |
| ③ | `CustomMultiTilesetBehaviour.cs` | `opts.Translation/Rotation` 자동 주입 **제거** (기본값 Zero/IDENTITY, 사용자 오프셋 용도로만) |

수정 후 검증 수치 (같은 타일 기준):
```
타일 OBB center           = (199.3, 168.9, 25.9)     ← 행렬 경로
메시 AABB + 루트 변환 합   = (202.8, 154.2, 25.9)     ← 분해 경로 (Z 정확 일치)
합성 회전(rotAxisAngle)    ≈ identity                 ← 블록 ENU 회전 ∘ WorldRotation 상쇄, 기대값
```
행렬 경로와 쿼터니언/분해 경로가 일치 → `DetermineFrustumSet` include → 타일 스트리밍/렌더 정상.

---

## 검증 방법 (재발 시 재사용)

`UpdateWorldOriginRight`의 BoundingVolume 생성 직후 임시 로그 3종이 결정타였다:

1. **`col4` 검사** — `computedTransform`의 `[3],[7],[11]`은 **(0,0,0)이어야 정상**. 값이 있으면
   전치/열-벡터 행렬이 체인에 유입된 것 (원인 ①을 이걸로 잡음).
2. **`row4T` 검사** — `[12],[13],[14]`에 −WO성 큰 값이 살아 있는지 (translation 생존 확인).
3. **경로 대조** — 행렬 경로 결과(OBB center) vs 쿼터니언 경로 기대값(sphere `realCenter` 식)
   vs 콘텐츠 분해 경로(메시 AABB + 루트 변환). 세 경로의 일치가 최종 판정 기준.

교훈: **증상이 보이는 계산부(UpdateWorldOrigin)가 아니라, 행렬의 입구(로드 관례)와 출구(적용 관례)의
일치부터 검증할 것.** 계산부 자체는 처음부터 올바랐다.

---

## 부수 결과물: 타일 BoundingVolume 시각화

- `B3dmTile.DebugDrawBoundingVolume()` — BoundingVolume 생성 직후 OBB의 축정렬 포락 AABB를
  `DebugHelper.UpdateBoundingBox(SID, bb)`로 그림. SID 키 기반이라 재계산 시 중복 없이 갱신.
  `B3dmTile.DebugDrawBounds = false`로 끌 수 있음.
- **주의(과거 함정)**: `SceneObject.BoundingBox`(메시 AABB)는 부모 변환이 빠진 **로컬 프레임**이라
  DebugHelper(월드 프레임)에 그대로 그리면 어긋난 위치에 그려진다. 실제로 이것 때문에
  "박스와 모델이 다른 위치" 오인 소동이 있었음 — 박스 시각화는 반드시 씬 프레임인
  `tile.BoundingVolume`을 사용할 것.

---

## 남은 항목 (이번 증상과 무관, 추후)

| 항목 | 위치 | 조건 |
|------|------|------|
| RTC_CENTER 없는 b3dm의 `tile.transform` 기반 콘텐츠 배치 | B3dmTile.cs TODO 주석 | 해당 데이터 로드 시 |
| Region 분기 — `Transform()` 미적용 + 프레임 혼합 | `B3dmTile.CreateBoundingVolume` Region 경로 | region 기반 tileset 로드 시 |
| `MatrixTransform.SetMatrix`의 Scale 소실 (분해 시 `Scale=null` 고정) | MatrixTransform.cs | transform에 scale 있는 tileset 로드 시 |
| `GetWorldInvRotation()` float 반환 → ~0.5m 정밀도 손실 | Coordinates.cs | 정밀도 요구 시 double 버전 |
| `UpdateWorldOriginLeft` 포팅 불일치 (worldOrigin 차감 대상, 90°X 주입 위치, `GetRootTransform` 미러 스케일 부재) | B3dmTile.cs / B3dmTileset.cs | LEFT_H 빌드 재사용 시 |
| 타일 언로드 시 디버그 박스 제거 미구현 | B3dmTile.DebugDrawBoundingVolume | 시각화 상시 사용 시 |

---

## 부록: 행렬 관례 해설 — 왜 "순차 복사"가 정답이고 "재배치"가 버그였나

이번 버그 ①·②의 공통 뿌리는 행렬 관례다. 경계면(스펙↔엔진) 코드를 다룰 때 참고.

### A. 관례(convention): 벡터를 어느 쪽에서 곱하나

같은 "이동 (100,200,300)" 변환이라도, 곱하는 방향에 따라 행렬 모양이 다르다.
translation은 **동차좌표의 1과 곱해지는 자리**에 있어야 하는데, 그 자리가 방향에 따라 바뀌기 때문:

```
열-벡터 M·v (3D Tiles/glTF/OpenGL/Unity)     행-벡터 v·M (MP3DCore/OSG/DirectX)
   결과성분 = M의 "행" · v                      결과성분 = v · M의 "열"

    1  0  0  100      ┌x┐                                    ┌  1   0   0   0 ┐
    0  1  0  200   ·  │y│                    [x y z 1]  ·    │  0   1   0   0 │
    0  0  1  300      │z│                                    │  0   0   1   0 │
    0  0  0   1       └1┘                                    └ 100 200 300  1 ┘
    translation = 4열                          translation = 4행
```

두 행렬은 서로 **전치(transpose)** 관계다. 수식으로: `w = M·v  ⇔  wᵀ = vᵀ·Mᵀ`.
→ **열-벡터 세계의 행렬을 행-벡터 엔진에서 쓰려면 반드시 Mᵀ가 필요하다.**

### B. 저장 순서(storage): 배열 자체에는 모양이 없다

flat 배열 16개는 그냥 숫자 나열이다. "column-major / row-major"는 배열의 성질이 아니라
**4×4 격자로 복원할 때의 읽기 규칙**(세로로 채우기 / 가로로 채우기)일 뿐이다.

```
같은 배열 [1,0,0,0, 0,1,0,0, 0,0,1,0, 100,200,300,1] 을

column-major로 읽으면 → translation이 4열에 있는 격자 (= M)
row-major로 읽으면    → translation이 4행에 있는 격자 (= Mᵀ)
```

즉 **같은 배열을 다른 규칙으로 읽는 순간, 전치가 "계산 없이 공짜로" 일어난다.**
숫자 100은 배열 12번 칸에 그대로 있고, 그 칸의 이름이 (1행,4열)→(4행,1열)로 바뀔 뿐이다.

### C. 상쇄 법칙: 두 겹 차이 = 아무것도 안 하기

3D Tiles → MP3DCore 경계에서는 차이가 정확히 두 겹이다:

```
관례 차이 (열-벡터 → 행-벡터):   전치 1번 필요
저장 차이 (column-major → row-major): 읽기 규칙 차이가 전치 1번을 공짜로 수행
──────────────────────────────────────────
직접 할 일: 0번  →  flat 배열 순차 복사가 정답     [항등식: column-major(M) == row-major(Mᵀ)]
```

버그 ①은 여기에 재배치(전치)를 한 번 **더** 해서 — 전치 2번 = 원상복구 — 열-벡터 행렬이
그대로 행-벡터 엔진에 들어갔고, translation이 죽은 자리(`[3],[7],[11]`)에 앉았다.

> **기억법**: 관례와 저장 순서가 **둘 다 다르면 그대로 복사**, **하나만 다르면 그때가 진짜 전치할 때.**

### D. 소비 방향도 같은 문제의 다른 얼굴 (버그 ②)

행렬을 올바르게 만들어도, 점에 적용할 때 방향이 틀리면 같은 사고가 난다:

- `PreMultiply` (v·M): translation을 `[12],[13],[14]`에서 읽음 — 이 엔진의 올바른 방향
- `PostMultiply` (M·v): translation을 `[3],[7],[11]`에서 읽음 — 이 엔진 행렬에선 항상 0 → 이동 소실, 회전은 전치(역회전) 적용

버그 ①이 "만드는 쪽"의 관례 위반이라면, 버그 ②는 "쓰는 쪽"의 관례 위반이었다.

### E. float 캐스트 금지 이유

ECEF 좌표는 ~3.15×10⁶ m. float(유효 ~7자리)는 이 크기에서 표현 간격이 **0.25 m** —
좌표가 성분당 최대 십수 cm씩 반올림되어 타일 경계 틈/미세 어긋남이 생긴다.
double(유효 ~15자리)은 같은 크기에서 오차가 나노미터 수준. **지구 좌표는 double 유지.**

---

## 비고

- 좌표 프레임 흐름 다이어그램: [b3dm-worldorigin-collaboration.svg](b3dm-worldorigin-collaboration.svg) (초기 분석 기준 — 본 문서의 결론 미반영)
- 검증 완료된 정상 경로: `SetOrigin`/geodetic→ECEF/`ToWorld`·`ToLocal`, sphere 컬링그룹 경로, 콘텐츠 분해 경로, `GetQuaternion`↔`Rotate(quat)` 상호 일관성.
- 참고 좌표: 협재 ECEF `(-3151036, 4299528, 3490554)` = 33.394127°N, 126.236928°E, h≈33m.
