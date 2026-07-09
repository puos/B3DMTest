# B3DM 텍스처 "실패" → 실은 메시 안 보임(투명) 문제 — 조사 기록

> 협재해변(제주) DJI Terra 타일셋(`https://211.178.39.228:22480/terra_b3dms_HyeopjaeBeach_prev/tileset.json`)
> 로딩 중, Nsight Graphics에서 "텍스처 실패"로 의심했으나 추적 결과 **텍스처는 정상**이고
> 실제 문제는 **메시가 화면에 안 나오는(투명) 위치/변환 문제**로 좁혀진 과정을 기록.

---

## 0. 증상 진행

1. 메시는 생성됨 (`glDrawElements` 실행, draw call이 Nsight에 들어옴).
2. Nsight에서 텍스처가 검정으로 보여 "텍스처 실패"로 의심.
3. 추적 결과 텍스처 데이터는 정상 → 다시 보니 **메시 모양 자체가 화면에 안 나옴**.
4. BackFaceCulling은 이미 `false`인데도 안 보임 → 컬링 문제 아님.
5. 결론 방향: **메시가 화면 밖(절대 ECEF) 또는 잘못된 위치로 변환되어 투명하게 안 보임.**

---

## 1. Nsight 관측

| 관측 | 내용 |
|------|------|
| API Error 행 전체 빨강 | `glEnable/glDisable(GL_TEXTURE_2D)`가 Core Profile에서 `GL_INVALID_ENUM` (레거시 고정 파이프라인 호출). **샘플링엔 무해**하나 에러 스팸 |
| 샘플러 3개 (`uTexture`/`uNormalTexture`/`uOverlayTexture`) 전부 Unit 0 | 아래 §3 버그와 연결 |
| `uTexture` 리소스(171) | 128×128, `GL_COMPRESSED_RGB`(BC1/DXT1), 8 mips, `GL_TEXTURE_2D`. **미리보기가 검정처럼** 보였으나 실제론 아틀라스 미사용 영역 |
| draw call | `glDrawElements(GL_TRIANGLES, count=9288/462, GL_UNSIGNED_INT, nullptr)` 정상 실행 |

---

## 2. 텍스처는 정상 (확정)

받아둔 b3dm(`BlockXXXYXX_L15_1.b3dm`, 113,724 bytes)을 직접 파싱:

- b3dm 헤더의 모든 테이블 길이 = 0 → **GLB는 오프셋 28부터**. **featureTable 없음 = RTC_CENTER 없음.**
- GLB(glTF 2.0, generator=**DJI Terra**)에 **JPEG 텍스처 2장 임베드** (bufferView 2, 3).
- 추출 결과: 두 장 모두 **256×256 사진 텍스처 아틀라스, 실제 내용 존재**
  (평균 밝기 81.8 / 55.8 of 255, 디코딩 성공).
  - `tex_1`은 **L자 아틀라스로 우하단이 검정** → Nsight에서 본 "검정"의 정체 = **아틀라스 미사용 영역** (텍스처 실패 아님).

> 부수 관찰: 원본 256×256인데 GPU엔 128×128 → 어딘가 다운스케일(품질 저하지만 "실패" 아님).

**텍스처 파이프라인(디코딩·압축·바인딩) 모두 정상.** 따라서 "텍스처 실패"는 오진.

### 로더 경로 (참고)
- PNG/JPEG → ImageSharp로 RGBA 디코드 → `CompressStandardImageToBC`에서 BCnEncoder로 **BC1(알파 없음)/BC3(알파)** 압축
  ([GLTFImport.cs:1842](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/GLTFLoader/GLTFImport.cs))

---

## 3. 부수적으로 발견한 렌더러 버그 (메시 안 보임의 원인은 아님)

### ① 텍스처 유닛 인덱스 덮어쓰기
[GenericModelPipeline.cs:971](../../MapPrimeNetV2/src/A4L.MP3DCore/Scene/Renderer/Pipeline/LocalPipeline/GenericModelPipeline.cs#L971) `ApplyTex`:
```csharp
var aindex = _uniformTextureIndexValue;     // 타입별 0/1/2 (Color=0, Normal=1, Overay=2)
switch (ta.TextureType) { ... }
aindex = texIndex++;   // ← 위 타입별 유닛을 순번(0,1,2…)으로 덮어씀! (버그)
```
Color 텍스처 하나면 0==0이라 우연히 동작하나, normal/overlay 섞이면 유닛 어긋남.
Nsight에서 세 샘플러가 전부 Unit 0으로 보인 것과 같은 증상. → `aindex = texIndex++;` 줄 제거하면 타입별 유닛 복원.

### ② 레거시 `glEnable(GL_TEXTURE_2D)`
`EnableTex(true/false)`가 고정 파이프라인 enable/disable 호출 → Core Profile에서 `GL_INVALID_ENUM` 스팸.
샘플링 무해하나 제거 대상.

---

## 4. 셰이더 색 계산 (조명/텍스처 곱)

[perPixel.frag:726-752](../../MapPrimeNetV2/src/A4L.MP3DCore/Scene/Renderer/Pipeline/Shader/perPixel.frag#L726):
```glsl
if(uUseLight==1) fragColor = smoothPongShader(fragColor, normalColor);
else             fragColor = frontColor;        // frontColor = uColor = (1,1,1,1) 흰색
if(uUseTexture==1) fragColor = fragColor * texture(uTexture, tc);
```
- `frontColor` 기본 흰색 (`SetColor(1,1,1,1)` + `uUseColor=false`).
- **조명 OFF + 텍스처 ON = 텍스처 그대로** (사진 측량 텍스처엔 이게 정석).
- **조명 ON + 노멀 없음/뒤집힘 → 어두워짐**. (이 GLB는 **NORMAL 속성 없음** → 조명 켜면 위험)
- 흰 화면 = `uUseTexture=0` 결과 / 검정 = 조명 결과가 검정.
→ **조명을 켜는 건 해결책이 아님.** 안 보이는 건 색 문제가 아니라 위치 문제.

---

## 5. 근본 원인 가설 — 메시 위치(transform localize) 미적용

### 메시 좌표 실측 (GLB accessor min/max)
- 정점은 **로컬 좌표**: X[-907~-667], Y[-491~-217], Z[-114~-90] (수백 m, 절대 ECEF 아님).
- GLB **노드에 matrix/translation 없음** → 위치는 전적으로 **타일 transform**에 의존.
- 메시 2개 모두 `mode=4`(TRIANGLES), POSITION/TEXCOORD_0/indices/material 있음, **NORMAL 없음**.

### 메시가 위치를 받는 경로
[B3dmTile.cs:685-694](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/B3dmTile.cs#L685):
```csharp
var pos = computedTransform.Position;
B3dmObject rootObject = new B3dmObject(SID);
rootObject.LocalPosition = pos;          // 메시 부모 = computedTransform 위치
```
RTC 없으면 [GLTFImport.cs:594-601](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/GLTFLoader/GLTFImport.cs#L594)에서 RTC 보정도 없음 →
메시 자식 노드 LocalPosition=0. 즉 **메시 월드 위치 = `computedTransform.Position`.**

### localize(worldOrigin 빼기)는 `Parent == null`에서만
[B3dmTile.cs:241-247](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/B3dmTile.cs#L241):
```csharp
if (Parent == null) {
    var pos3d = ToVec3LeftHandedGeocentric(position) - worldOrigin;  // ECEF→원점상대
}
```

### 데이터는 다단계 중첩 타일셋
```
tileset.json
  └─ BlockXXXYXX/tileset.json   ← transform에 ECEF translation (-3150896, 4299833, 3490477)
       └─ block_root.json
            └─ BlockXXXYXX_L15_1.b3dm (정점 로컬, RTC 없음)
```
ECEF translation이 **Block 레벨 tileset의 transform**에 박혀 있음.
- 중첩 루트가 `Parent==null`로 인식 → worldOrigin 차감 → 작은 원점-상대 → **화면 OK**
- 자식으로 인식 → worldOrigin **미차감** → 메시 절대 ECEF(~3e6+) → **Far(200km) 밖 → 투명하게 안 보임**

→ bbox는 frustum 통과시키지만 메시만 화면 밖. [b3dm-mesh-rtc-fix.md](b3dm-mesh-rtc-fix.md)의 **transform 버전**.

---

## 5-B. 로그로 확정된 결과 (★실측)

`[B3DM-POS]` 로그(65개 타일 전부):
```
pos        = (0.1, 19642.1, -6371665.0)   ← 모든 타일 동일
bboxCenter = (0.11, 19642.14, -6371664.99) ← pos와 일치
```

**확정 사항:**
1. `pos === bboxCenter` → 메시·bbox **같은 자리** (프레임 불일치 아님).
2. **Z ≈ -6,371,665 m = -지구 반지름(6371km)**, 모든 타일이 **한 점으로 붕괴** → 카메라에서 6371km 밖 → Far(200km) 밖 → **투명/안 보임**.
3. 모든 타일 `Parent=child` → `if (Parent == null)` localize 분기 **미실행**.

**추가 확정 — 빌드는 `LEFT_H` 미정의:** csproj/props/`#define` 어디에도 `LEFT_H` 없음 →
`UpdateWorldOrigin()`은 `#else`인 **`UpdateWorldOriginRight()` 실행**. (boundingvolume-fix의 LEFT_H X-반전 작업은 이 빌드에서 비활성.)

### 근본 원인 (메커니즘)
- `GetRootTransform()` ([B3dmTileset.cs:120](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/B3dmTileset.cs#L120)) = **회전만, translation=0**.
- worldOrigin 차감은 `Parent==null`(최상위 루트)에서만. 그런데 **루트 tileset.json엔 지리 transform 없음**(위치 0).
- 실제 지구 좌표 ECEF(-3150896,…)는 **중첩 Block 타일셋의 transform**에 있고, 그 타일들은 `Parent=child`라 localize를 건너뜀.
- 결과: 루트는 `0 − worldOrigin = −worldOrigin`(≈ -6371km), 중첩 ECEF는 localize 없이 합성되어 상쇄 안 됨 →
  **모든 지오메트리가 ~6371km 밖 한 점**.

---

## 5-C. 수정안

### 수정안 1 (★현재 테스트 중) — localize를 GetRootTransform(최외곽)으로 이전
ECEF 오프셋이 어느 중첩 단계에 있든, **변환 체인 전체의 최종 위치에 한 번** localize:
```
p_local = worldRot × (p_ecef − worldOrigin)
```
구현:
1. **`GetRootTransform()`** 가 localize 포함:
   ```csharp
   var rotation = Coordinates.GetWorldRotationd();
   var worldOrigin = QuaternionLeftHandedGeocentric
       .ToLeftHandedQuaternion(Coordinates.GetWorldInvRotation())
       .Multiply(Coordinates.GetWorldOrign());
   var t = worldOrigin.ToVector3d();
   var m = Matrix4d.GetTranslate(-t.X, -t.Y, -t.Z) * Matrix4d.GetRotate(rotation);
   return new MatrixTransform(m);
   ```
   (행 우선: `p · GetTranslate(−origin) · GetRotate(worldRot) = worldRot×(p−origin)`)
2. **`UpdateWorldOriginRight()`의 `Parent==null` 부분 localize 제거** (체인은 raw ECEF로 올림 → 최종 GetRootTransform이 한 번에 localize).

검증: `[B3DM-POS]` 로그의 `pos`가 **0 근처(수십~수백 m)** 로 떨어지고 타일마다 값이 흩어지면 성공.
회전/프레임 규약이 미묘하므로 로그를 보며 1~2회 반복 조정 예정.

### 수정안 2 (대안) — 리프에서 최종 위치 localize
체인을 raw ECEF로 합성 후 `rootObject.LocalPosition`/bbox 설정 시점에 표준 localize 식 1회 적용.
수정안 1이 프레임 문제로 수렴 안 하면 전환.

---

## 6. 다음 단계 — 확정용 로그

[B3dmTile.cs:692](../../MapPrimeNetV2/src/A4L.Mapprime3DNet/IO/B3DM/B3dmTile.cs#L692) `rootObject.LocalPosition = pos;` 직후:
```csharp
System.Diagnostics.Debug.WriteLine(
    $"[B3DM-POS] SID={SID} Parent={(Parent==null?"ROOT":"child")} " +
    $"pos=({pos.X:F1}, {pos.Y:F1}, {pos.Z:F1}) bboxCenter={BoundingVolume?.Center}");
```
판정:
- `pos` ~3e6 → **localize 누락 확정** → 수정: 중첩 타일셋 루트도 worldOrigin 차감(또는 transform 합성 시 한 번만 차감)
- `pos` 작은데 `bboxCenter`와 다름 → 메시/bbox 프레임 불일치

대안: Nsight에서 해당 draw의 `glUniformMatrix4fv`(모델 행렬) translation 행 직접 확인.

---

## 7. 정리 — 할 일 목록

- [ ] **(핵심)** §6 로그로 `rootObject.LocalPosition` 크기 확정 → 중첩 타일셋 transform localize 수정
- [ ] §3① `aindex = texIndex++;` 제거 (텍스처 유닛 복원)
- [ ] §3② 레거시 `glEnable(GL_TEXTURE_2D)` 제거
- [ ] GLB에 NORMAL 없음 → 조명 경로 대비(노멀 생성 or 조명 off 정책)
- [ ] 텍스처 256→128 다운스케일 지점 확인(품질)

---

## 부록 — 재현용 데이터 추출 메모

- b3dm 1개: `BlockXXXYXX/BlockXXXYXX_L15_1.b3dm` (block_root.json에서 b3dm uri 확보)
- GLB 시작 = b3dm 오프셋 28 (테이블 길이 전부 0).
- 임베드 텍스처 2장: bufferView 2,3 / `image/jpg` / 256×256.
- 서버는 자체서명 인증서 → `curl -sk` (SSL 우회) 필요.
