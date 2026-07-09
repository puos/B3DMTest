# B3DM 메시(RTC) 월드좌표 버그 — 분석 및 수정안

## 증상

- 좌표 수정(BoundingVolume) 이후 **frustum 은 통과(타일 bbox 정상), 콘텐츠(b3dm)도 로드되는데 화면에 메시가 안 보임.**
- wireframe(`PolygonMode.Line`) + `BackFaceCulling=false` 로 해도 안 보임 → 셰이더 문제가 아니라 **메시가 화면 밖(절대 ECEF)** 에 있음.

---

## 근본 원인

b3dm Feature Table 의 `RTC_CENTER`(Relative-To-Center, ECEF 절대좌표)를 메시 루트 노드에 적용할 때
**`worldOrigin` 차감이 빠져 있어 메시가 절대 ECEF 위치에 놓인다.**

### 경로

1. `B3dmComponent` 가 b3dm 로드 → `RTC_CENTER` 를 `GLTFImport` 에 전달
   (`B3dmComponent.cs:354-359`)
2. `GLTFImport.GetRtcTranslationMatrix()` 가 RTC 변환 행렬 생성
   (`GLTFLoader/GLTFImport.cs:113-134`)
3. 루트 노드 생성 시 `nodeMatrix = rtcMatrix * nodeMatrix` → `LocalPosition = RTC_CENTER`
   (`GLTFLoader/GLTFImport.cs:1279-1289`)

### 문제 코드 — `GetRtcTranslationMatrix()`

```csharp
private Matrix4d GetRtcTranslationMatrix()
{
    if (RTC_CENTER == null || RTC_CENTER.Length != 3)
        return null;

    // RTC_CENTER 는 Z-up ECEF (절대좌표, 크기 ≈ 6.37e6)
    var rtcTranslation = Matrix4d.GetTranslate(RTC_CENTER[0], RTC_CENTER[1], RTC_CENTER[2]);

    var yupRot = Quaterniond.AngleAxis(90, Vector3d.Right);   // Y-up → Z-up
    var yupRotMatrix = Matrix4d.GetRotate(yupRot);

    _rtcTransformMatrix = yupRotMatrix * rtcTranslation;       // ← worldOrigin 차감 없음!
    return _rtcTransformMatrix;
}
```

→ 결과 행렬 translation = `RTC_CENTER`(절대 ECEF). 메시 루트 노드 `LocalPosition` 이 ~6.37e6 →
카메라/boundingVolume 은 원점-상대인데 메시만 절대 → **화면 밖.**

### BoundingVolume 과의 대조

| | worldOrigin 차감 | 결과 |
|---|---|---|
| BoundingVolume (`B3dmTileBoundingVolume.Transform`) | ✅ computedTransform 에 −worldOrigin 포함 + `MultiplyPoint` 적용 | 원점-상대 → frustum 통과 |
| **메시 (RTC, `GetRtcTranslationMatrix`)** | ❌ 차감 없음 | 절대 ECEF → 안 보임 |

---

## 확인 방법 (디버거)

1. `b3dmLoader.RTC_CENTER` 가 null 이 아니고 값이 절대 ECEF(~ `-3151036, 4299528, 3490554` 수준)인지.
2. `GetRtcTranslationMatrix()` 결과 행렬의 translation([12],[13],[14] = M30,M31,M32)이 ~6.37e6 인지.
3. 루트 메시 노드 `b3dmObject.LocalPosition` 이 ~6.37e6 인지.

→ 하나라도 ~6.37e6 이면 **메시가 월드(절대 ECEF) 좌표로 생성됨** 확정.

---

## 수정안

대상: `D:\work\MapPrimeNetV2\src\A4L.Mapprime3DNet\IO\B3DM\GLTFLoader\GLTFImport.cs`
메서드: `GetRtcTranslationMatrix()`

`RTC_CENTER`(ECEF)를 BoundingVolume / 타일 위치와 **동일한 프레임으로 localize** 해야 한다.
즉 ECEF 에서 `WorldOrigin` 을 빼고, 동일한 월드 회전을 적용한다.

```
local_rtc = worldRot · (RTC_ecef − WorldOrigin_ecef)
```

후보 구현 (BoundingVolume 의 computedTransform 생성과 동일한 규약을 따를 것):

```csharp
private Matrix4d GetRtcTranslationMatrix()
{
    if (RTC_CENTER == null || RTC_CENTER.Length != 3)
        return null;

    if (_rtcTransformMatrix != null)
        return _rtcTransformMatrix;

    // RTC_CENTER(ECEF) 를 원점-상대로 변환 (BoundingVolume 과 동일 프레임)
    var rtcEcef = new Vec3LeftHandedGeocentric(RTC_CENTER[0], RTC_CENTER[1], RTC_CENTER[2]);
    var worldOrigin = QuaternionLeftHandedGeocentric
        .ToLeftHandedQuaternion(Coordinates.GetWorldInvRotation())
        .Multiply(Coordinates.GetWorldOrign());
    var localPos = (rtcEcef - worldOrigin).ToVector3d();   // ← worldOrigin 차감

    var rtcTranslation = Matrix4d.GetTranslate(localPos.X, localPos.Y, localPos.Z);

    var yupRot = Quaterniond.AngleAxis(90, Vector3d.Right); // Y-up → Z-up
    var yupRotMatrix = Matrix4d.GetRotate(yupRot);

    _rtcTransformMatrix = yupRotMatrix * rtcTranslation;
    return _rtcTransformMatrix;
}
```

> 주의:
> - 실제 적용 시 BoundingVolume 이 사용하는 변환(`UpdateWorldOriginRight/Left` + `CreateBoundingVolume`)과
>   **회전·축 swap·(LEFT_H 시) X 반전 규약을 정확히 일치**시켜야 한다. 위 코드는 차감(−worldOrigin)을
>   넣은 골격이며, 최종 회전/swap 은 동작하는 BoundingVolume 결과와 대조해 맞춘다.
> - LEFT_H / Right 경로에 따라 swap/X반전이 달라지므로, BoundingVolume 과 같은 `#if` 분기를 적용한다.

---

## 검증 절차

1. 위 수정 후 리빌드.
2. 협재해변 tileset 로드.
3. `GetRtcTranslationMatrix()` translation 이 **작은 원점-상대 값**인지 확인.
4. 메시가 카메라 근처(원점 부근)에 배치되어 화면에 보이는지 확인.
5. 안 보이면 그때 셰이더/머티리얼/winding 순서로 점검.

---

## 관련 수정 (이미 적용됨, 좌표 파이프라인 일관성)

| # | 위치 | 내용 |
|---|------|------|
| ① | `B3dmTile.CreateBoundingVolume` | `#if LEFT_H` box center/halfAxes X 반전 ([b3dm-boundingvolume-fix.md](b3dm-boundingvolume-fix.md)) |
| ② | `B3dmTileBoundingVolume.Transform` | `PostMultiply`→`MultiplyPoint`, `PostMultiply3X3`→`PreMultiply3X3` (translation 누락 수정) |
| ③ | (본 문서) `GLTFImport.GetRtcTranslationMatrix` | RTC_CENTER 에 worldOrigin 차감 추가 |

①②는 BoundingVolume(frustum) 을, ③은 메시 실물 위치를 각각 원점-상대로 맞춘다.
세 곳 모두 "ECEF → 원점-상대" 변환을 적용해야 카메라·bbox·메시가 같은 프레임에 놓인다.
