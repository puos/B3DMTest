# 단일 CustomMultiTileset — UpdateWorldOrigin 미발동 버그

## 증상

`CustomMultiTilesetBehaviour`가 tileset 하나만 로드하는 경우(현재 실사용 전부),
`UpdateWorldOrigin()`이 실질적으로 실행되지 않는다. `TilesetComplete()`은 호출되지만
`UpdateWorldOrigin` 안에서 `customTilesets.Count == 0` 조기 return으로 빠져나간다.

## 근본 원인 — 등록(Add)과 완료 콜백(TilesetComplete)의 순서 경쟁

완료 판정은 명시적 총개수가 없고 `customTilesets.Count`를 총 개수로 *간주*한다.

```csharp
// TilesetComplete
tilesetCount = tilesetCount + 1;
if (customTilesets.Count > tilesetCount) return false;   // 완료 게이트
...
UpdateWorldOrigin(worldContext.WorldOrigin);

// UpdateWorldOrigin
if (customTilesets.Count == 0) return;   // ← 여기서 빠져나감
```

전제: **모든 `AddTileset`(등록)이 먼저 끝난 뒤에야 `TilesetComplete`(완료)가 시작된다.**
옛 Promise 흐름에서는 성립했으나 async 리팩터가 이 전제를 깼다.

### 호출 스택 (버그 버전)

```
AddTileset(options)
  └─ await B3dmTileset.CreateAsync(options, this)
        └─ await tileset.InitializeAsync(url)          // B3dmTileset.cs
              └─ Root = await LoadTilesetAsync(...)      // 트리 생성 + Root 할당
              └─ Behaviour.TilesetComplete(this)   ★조기 발동 (B3dmTileset.cs:167)
  ← CreateAsync 반환
  customTilesets.Add(index, tileset)                    // 등록은 여기서야 (CMB.cs:246)
```

`TilesetComplete`이 `Add`보다 한 스택 안쪽에서 먼저 터진다.
→ 단일 케이스: `Count(0) > tilesetCount(1)` = false → 통과 → `UpdateWorldOrigin` →
`Count==0` → 조기 return.

## 동작 경계

의미 있는 완료 경계는 **`AddTileset → await CreateAsync`가 반환하는 지점**이지,
안쪽 `InitializeAsync → LoadTilesetAsync` 직후(167행 self-callback)가 아니다.

- `await CreateAsync`는 **타일셋 트리(구조) 완료 시점**에 반환한다 (tileset.json 다운로드+파싱+
  B3dmTile 트리 생성+Root 할당).
- 타일 **콘텐츠(b3dm 메시)** 로드는 무관 — 매 프레임 카메라 가시성 기준 스트리밍이며
  "전부 완료" 시점이 없다. `UpdateWorldOrigin`은 트리만 있으면 되므로 콘텐츠를 기다릴 필요 없음.

## 수정 (= V3가 이미 적용한 방식)

두 곳을 바꾼다.

1. **`B3dmTileset.cs:167`의 조기 콜백 제거**
   ```csharp
   //((CustomMultiTilesetBehaviour) Behaviour).TilesetComplete(this);
   ```

2. **`CustomMultiTilesetBehaviour.AddTileset`에서 `customTilesets.Add` 다음에 호출**
   ```csharp
   var tileset = await B3dmTileset.CreateAsync(options, this);
   int index = customTilesets.Count;
   customTilesets.Add(index, tileset);        // ★ 먼저 등록
   ...
   TilesetComplete(tileset);                   // ★ 등록 후 호출
   ```

### 검증 (단일 케이스)

```
await CreateAsync 반환          (167 콜백 없음)
customTilesets.Add → Count = 1
TilesetComplete:
    tilesetCount = 0+1 = 1
    Count(1) > tilesetCount(1) → 1>1 = false → 통과 ✓
    UpdateWorldOrigin: Count==1(≠0) → 정상 실행 ✓
```

## 남은 취약점 (미래 멀티 대비)

V3 방식도 여러 tileset을 순차 `await`로 추가하면 첫 tileset에서 이미 UpdateWorldOrigin이
발동한다(`Count(1) > 1` false). 지금은 항상 1개라 무해하지만, 멀티가 생기면:

- `expectedCount = Root.Children.Count`처럼 **총개수를 명시적으로 선언**하거나,
- `await UniTask.WhenAll(자식들 AddTileset)` 후 **UpdateWorldOrigin을 1회만** 호출.

## 테스트 데이터 (data/b3dm)

전부 `region` 바운딩 + `RTC_CENTER`로 배치되며 `tile.transform`은 없음(→ 행렬 파싱 버그와 무관).
위치: **대한민국 수도권 서부 (시흥/안산/인천 일대)**, 중심 약 **lat 37.4044 N, lon 126.634 E**.

| 파일 | 위도 (N) | 경도 (E) | 높이 |
|------|----------|----------|------|
| p-00000 | 37.406971 | 126.633360 | 103.3 m |
| p-00001 | 37.411789 | 126.633512 | 2.2 m |
| p-00003 | 37.401172 | 126.626180 | −11.1 m |
| p-00005 | 37.404608 | 126.613037 | −501.4 m |
| p-00008 | 37.401089 | 126.634149 | 7.8 m |
| p-00064 | 37.402033 | 126.642296 | 31.9 m |
| p-00065 | 37.405493 | 126.642560 | 16.3 m |
| p-00067 | 37.405480 | 126.626830 | 15.0 m |
| p-00068 | 37.402290 | 126.627435 | 29.1 m |
| p-00074 | 37.405537 | 126.631056 | 9.8 m |
| p-00075 | 37.401738 | 126.630713 | 37.3 m |
| p-00081 | 37.407974 | 126.642424 | 7.3 m |
| p-00082 | 37.403680 | 126.642324 | 22.1 m |
| p-00083 | 37.411088 | 126.628975 | 28.4 m |
| p-00084 | 37.403861 | 126.629197 | −11.0 m |
| p-00085 | 37.410191 | 126.636223 | 110.5 m |
| p-00086 | 37.403690 | 126.637363 | 15.2 m |
| p-00087 | 37.406924 | 126.632382 | 112.4 m |
| p-00088 | 37.404356 | 126.642361 | 17.2 m |
| u-00000 | 37.401493 | 126.648052 | 19.2 m |
| u-00002 | 37.390649 | 126.634922 | 8.1 m |

- 위도 범위 ≈ 37.3906 ~ 37.4118 N, 경도 범위 ≈ 126.6130 ~ 126.6481 E
- `1.b3dm`은 RTC_CENTER 없음(BATCH_LENGTH만) — 별도 케이스
- `tileset.json`(data/b3dm/4/)의 region 중심도 RTC_CENTER와 소수 5자리까지 일치 확인됨

## 관련 문서

- 좌표/행렬 관례 버그: [b3dm-boundingvolume-fix.md](b3dm-boundingvolume-fix.md)
- RTC 메시 배치: [b3dm-mesh-rtc-fix.md](b3dm-mesh-rtc-fix.md)
- 로딩 흐름: [b3dm-loading-scenario.md](b3dm-loading-scenario.md)
- 협력 다이어그램: `../design/b3dm-collaboration.puml`, `../design/b3dm-worldorigin-collaboration.puml`
