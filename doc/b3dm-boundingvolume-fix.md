# B3DM BoundingVolume 좌표 프레임 버그 — 분석 및 수정안

## 증상

- 협재해변(제주) `tileset.json` 로드 시 화면에 아무것도 렌더링되지 않음.
- `B3dmTilesetTraversal.DetermineFrustumSet(root)` 가 `false` 반환 → `isInclude=false` → 타일 순회/요청 전체 중단.
- 원인: **root 타일의 `BoundingVolume.Center` 가 원점-상대가 아니라 절대 ECEF 로 남음.**

### 관측 데이터

| | OBB Center | 크기 |
|---|---|---|
| 원본 ECEF (tileset.json box) | `(-3151036, 4299528, 3490554)` | 6.37×10⁶ |
| MapPrimeNet (Right 경로) | `(2217719, 2304534, 5510878)` | 6.37×10⁶ |
| MapPrimeNet (Left 경로) | `(1555157, 5280849, 3208279)` | 6.37×10⁶ |
| **정상 (Unity / NeoWorldDemo)** | `(-27908, -15, 545)` | 작음(원점-상대) |

- 세 MapPrimeNet 결과 모두 크기 = **지구 반지름(6.37e6)** → 회전만 적용되고 **origin 차감이 안 됨**.
- 카메라는 원점-상대 `(0, 0, 100)` 인데 타일은 절대 ECEF → 6,378km 떨어진 것으로 계산 → frustum OUTSIDE.

---

## 근본 원인

`UpdateWorldOriginLeft` 는 worldOrigin/transform 을 **X 반전된 좌표 프레임**에서 만든다:

```csharp
// B3dmTile.UpdateWorldOriginLeft()
t.X = -1d * position.X;   // ← transform 은 X 반전 프레임
t.Y = position.Z;
t.Z = position.Y;
```

그런데 `CreateBoundingVolume` 의 box center 는 **raw ECEF(X 반전 안 됨)** 그대로 사용된다:

```csharp
// B3dmTile.CreateBoundingVolume()  (현재)
Vector3d center = new Vector3d(box[0], box[1], box[2]);   // X 반전 없음
```

→ **box center 프레임 ≠ transform 프레임** → `result.Transform(computedTransform)` 에서 `−worldOrigin` 이 상쇄되지 않음 → 절대 ECEF 잔존.

### Unity(NeoWorldDemo)는 어떻게 정상인가

`Unity3DTile.CreateBoundingVolume()` 는 box 를 transform 적용 **전에 X 반전**한다 (Unity 는 왼손 좌표계):

```csharp
// Unity3DTile.cs : 576-579
center.x   *= -1;
halfAxesX.x *= -1;
halfAxesY.x *= -1;
halfAxesZ.x *= -1;

var result = new TileOrientedBoundingBox(center, halfAxesX, halfAxesY, halfAxesZ);
result.Transform(transform);
```

→ box center 가 transform 과 **같은 X 반전 프레임** → `−worldOrigin` 정상 상쇄 → 원점-상대.

> 참고: `UnityTransform()` 과 MapPrimeNet `GetTransform()` 은 둘 다 transform 없을 때 `IDENTITY` 반환 — **transform 을 임의로 주입하는 곳은 양쪽 모두 없음.** 차이는 오직 box center 의 X 반전 유무.

---

## 수정안

대상: `D:\work\MapPrimeNetV2\src\A4L.Mapprime3DNet\IO\B3DM\B3dmTile.cs`
메서드: `CreateBoundingVolume(Schema.BoundingVolume, MatrixTransform)` 의 `Box.Count == 12` 분기.

`LEFT_H` 정의 시에만 box center / halfAxes 의 X 부호를 반전한다 (Unity 와 일치).

```csharp
if (boundingVolume.Box.Count == 12)
{
    var box = boundingVolume.Box;
    Vector3d center    = new Vector3d(box[0], box[1], box[2]);
    Vector3d halfAxesX = new Vector3d(box[3], box[4], box[5]);
    Vector3d halfAxesY = new Vector3d(box[6], box[7], box[8]);
    Vector3d halfAxesZ = new Vector3d(box[9], box[10], box[11]);

#if LEFT_H
    // ECEF(오른손) → 왼손 프레임 정렬. UpdateWorldOriginLeft 가 X 반전 프레임에서
    // worldOrigin/transform 을 만들므로, box center 도 같은 프레임으로 맞춰야
    // computedTransform 의 -worldOrigin 이 상쇄되어 원점-상대 좌표가 된다.
    center.X    *= -1;
    halfAxesX.X *= -1;
    halfAxesY.X *= -1;
    halfAxesZ.X *= -1;
#endif

    var result = new TileOrientedBoundingBox(center, halfAxesX, halfAxesY, halfAxesZ);
    result.Transform(transform.Matrix);
    return result;
}
```

### 분기별 적용 규칙

| 경로 | X 반전 |
|------|--------|
| `LEFT_H` (`UpdateWorldOriginLeft`) | **적용** — transform 이 X 반전 프레임 |
| Right (`UpdateWorldOriginRight`) | **미적용** — transform 이 X 반전 안 함 |

> Right 경로를 쓰는 빌드라면 이 수정으로 오히려 프레임이 어긋날 수 있으므로 반드시 `#if LEFT_H` 로 감쌀 것.

---

## 검증 절차

1. 두 어셈블리(`A4L.Mapprime3DNet`, `A4L.MP3DCore`) `DefineConstants` 에 `LEFT_H` 정의 후 전체 리빌드.
2. 협재해변 `tileset.json` 로드.
3. `CreateBoundingVolume` 직후 `BoundingVolume.Center` 확인:
   - 기대: `(-27908, ...)` 수준의 **작은 원점-상대 값**.
4. `DetermineFrustumSet(root)` → `isInclude == true` 확인.
5. 타일 요청/렌더 진행되어 화면에 모델 표시 확인.

검증 기준값(정상): Unity OBB Center `(-27908, -15, 545)`.

---

## 비고

- 본 문서는 분석 결과이며, 실제 수정은 MapPrimeNetV2 측 코드 변경을 수반한다.
- 좌표 프레임 전체 흐름은 [b3dm-worldorigin-collaboration.svg](b3dm-worldorigin-collaboration.svg) 참고.
- `SetOrigin`(WorldContext.cs:81) 은 NeoWorldDemo `BaseLocationManager.SetOrigin` 과 동일 — 원점 설정 자체는 정상.
