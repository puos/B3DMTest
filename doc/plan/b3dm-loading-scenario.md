# B3DM Loading 예제 시나리오 (MapPrime3D 기반)

## 개요

MapPrime3D (`A4L.Mapprime3DNet` / `A4L.MP3DCore`) DLL을 렌더링 엔진으로 사용하여,
단일 `.b3dm` 파일 또는 `tileset.json` 기반 3D Tiles를 로드하고 씬에 렌더링하는 예제를 구현한다.

기존 `CityGMLViewer` 패턴(`DataSource → CreateRenderableGroup → SceneGroups.Add`)을 그대로 따른다.

---

## 기술 스택

| 역할 | 구성요소 |
|------|---------|
| 렌더링 엔진 | `A4L.MP3DCore` (MapPrime3D) |
| 씬 제어 | `ObjectsViewWPF`, `ViewController`, `SceneView` |
| 카메라 | `CustomCameraController` (PCDTest 방식) |
| B3DM 파싱 | `A4L.Mapprime3DNet.IO.B3DM.B3DMLoader` (기존 존재) |
| 데이터 소스 | `B3dmDataSource` (기존 존재, `WorldGlobe` 기반) |
| 렌더 그룹 | `B3mdRenderGroup` |
| 씬 오브젝트 | `B3dmObject` + `B3dmMesh` |
| UI 프레임워크 | WPF (.NET 8) |
| 뷰어 구조 참고 | `PCDTest.TDViewer` (`3DViewer.xaml.cs`) |

---

## 기존 코드 구조 파악

### 이미 존재하는 핵심 클래스

```
A4L.Mapprime3DNet/
├── IO/
│   ├── B3DM/
│   │   ├── B3DMLoader.cs          ← b3dm 헤더 파싱 (magic, featureTable, GLB 추출)
│   │   ├── B3dmObject.cs          ← SceneObject 상속, Unity 스타일 씬 오브젝트
│   │   ├── B3dmMesh.cs            ← 메시 데이터 + CreateGeometryPrimitive()
│   │   ├── B3dmTileset.cs         ← tileset.json 스키마
│   │   ├── B3dmTilesetTraversal.cs← LOD 트래버설
│   │   └── CustomMultiTilesetBehaviour.cs ← 멀티 타일셋 관리
│   ├── DataSource/
│   │   └── B3dmDataSource.cs      ← SceneDataSource 상속, CreateRenderableGroup()
│   └── World/
│       └── WorldGlobe.cs          ← 지구본 컨텍스트, StartTile(rootUrl) 진입점
```

### 핵심 씬 추가 패턴 (CityGMLViewer에서 확인)

```csharp
// 1. WorldGlobe 초기화 (위도/경도 원점 설정)
WorldGlobe.Instance.Initialize(SceneView.View, latitude, longitude, altitude);

// 2. DataSource 생성
B3dmDataSource dataSource = new B3dmDataSource(WorldGlobe.Instance);

// 3. tileset.json URL로 타일 시작
dataSource.StartTile(rootUrl);  // 내부적으로 CustomMultiTilesetBehaviour 초기화

// 4. RenderableGroup 생성 후 씬에 추가
var group = dataSource.CreateRenderableGroup();  // → B3mdRenderGroup 반환
_objectsView.SceneView.SceneGroups.Add(group);

// 5. DataSource 등록
Workspace.Instance.DataSources.Add(dataSource);
```

---

## 시나리오 흐름

```
[사용자] 파일 선택 or URL 입력
    │
    ▼
[MainWindow or 예제 Window]
    │
    ├─ WorldGlobe.Instance.Initialize(viewPort, lat, lon, alt)
    │       좌표계 원점 설정 (ECEF → 로컬 변환 기준)
    │
    ├─ B3dmDataSource(WorldGlobe.Instance) 생성
    │
    ├─ dataSource.StartTile(rootUrl)
    │       └─ CustomMultiTilesetBehaviour.Initialize(viewPort, rootUrl, worldContext)
    │               └─ tileset.json 파싱 → B3dmTileset 구성
    │
    ├─ dataSource.CreateRenderableGroup()
    │       └─ B3mdRenderGroup 반환 (BoundingBox, Matrix 설정 포함)
    │
    ├─ SceneView.SceneGroups.Add(group)
    │
    └─ Update 루프 (WorldGlobe.OnUpdate 호출)
            └─ CustomMultiTilesetBehaviour.OnUpdate(UpdateRenderable)
                    └─ 카메라 거리/LOD에 따라 B3dmTile 로드/언로드
                            └─ B3DMLoader → GLB 추출
                                    └─ B3dmMesh → B3dmObject.BuildGeometry()
                                            └─ SceneObject에 Primitive 추가
```

---

## 구현 단계

### 1단계 — 프로젝트 셋업

```
B3DMTest/
├── B3DMTest.csproj        # WPF, .NET 8, MapPrime3D DLL 참조
├── MainWindow.xaml        # 씬 뷰어 UI
├── MainWindow.xaml.cs     # 진입점 로직
└── doc/
    └── b3dm-loading-scenario.md
```

**프로젝트 참조 DLL (기존 bin/Debug에서 복사)**
- `A4L.MP3DCore.dll`
- `A4L.Mapprime3DNet.dll`
- `A4L.MapprimeNet.dll`

### 2단계 — 3DViewer 윈도우 초기화

**PCDTest `TDViewer.InitializeScene()`** 패턴을 그대로 가져온다:

```csharp
private void InitializeScene()
{
    objectsViewWPF = new ObjectsViewWPF();
    objectsViewWPF.SetController(new ViewController());
    ConfigurePointCloudNearFar();   // Near=0.1, Far=10000
    clientViewGrid.Children.Add(objectsViewWPF);
    objectsViewWPF.HorizontalAlignment = HorizontalAlignment.Stretch;
    objectsViewWPF.VerticalAlignment   = VerticalAlignment.Stretch;

    SceneView.ShowDebugFPS = true;
    objectsViewWPF.BackFaceCulling = true;

    // 렌더 부하 제한
    SceneView.RenderAbort = (sta, dist) =>
        sta.DrawCallCount >= 18000 && dist >= 1000;

    Workspace.Instance.Owner      = this;
    Workspace.Instance.ViewControl = objectsViewWPF;
    Workspace.Instance.ViewController = ViewController;
    Workspace.Instance.CommandUi  = new CommandUI(objectsViewWPF, ViewController);
    Workspace.Instance.CommandUi.SetDefaultMouseMode();

    // HUD 축 표시
    var axisHud = new HUD3AxisInputHandler("axis", GlobalOption.ViewAxisPixelSize);
    axisHud.Visible = true;
    axisHud.SetDock(HUDInputHandlerBase.DockingPositions.LeftBottom);
    SceneView.InputHandlers.AddHudHandler(axisHud);

    // 배경색
    GlobalOption.BackgroundColor = new ColorF(0.06f, 0.10f, 0.20f, 1.0f);
    ViewController.BackgroundColor.Set(GlobalOption.BackgroundColor);

    // 카메라 컨트롤러 (줌 최소거리 1.0)
    var cameraController = new CustomCameraController();
    cameraController.ZoomMinDistance = 1.0;
    ViewController.InputHandlers.Remove(cameraController);
    ViewController.InputHandlers.Add(cameraController);
    ViewController.InputHandlers.SetViewingMode(cameraController);
}

private void ConfigurePointCloudNearFar()
{
    GlobalOption.Near = 0.1;
    SceneView.NearFarUpdater.NearUpdateEnable = false;
    SceneView.NearFarUpdater.TargetNear = GlobalOption.Near;
    ViewController.View.Near = GlobalOption.Near;
    GlobalOption.Far = 10000;
}
```

### 3단계 — B3DM 로드 트리거

```csharp
private async void LoadB3DM(string rootUrl, double latitude, double longitude)
{
    // Near/Far 범위 확장 (대규모 타일용)
    SceneView.NearFarUpdater.FarUpdateEnable = false;
    SceneView.NearFarUpdater.TargetFar = 200000;

    // 지구 좌표 원점 설정
    WorldGlobe.Instance.Initialize(SceneView.View, latitude, longitude, 100);

    // DataSource 생성 및 타일 시작
    var dataSource = new B3dmDataSource(WorldGlobe.Instance);
    await dataSource.StartTile(rootUrl);

    // 씬에 추가
    Workspace.Instance.DataSources.Add(dataSource);
    var group = dataSource.CreateRenderableGroup();
    _objectsView.SceneView.SceneGroups.Add(group);

    Workspace.Instance.CommandUi.RefreshSceneGroup();
}
```

### 4단계 — Update 루프 연결

기존 `_uiSyncTimer` 또는 렌더 루프에서:

```csharp
// WorldGlobe의 타일 업데이트 (LOD 갱신)
WorldGlobe.Instance.OnUpdate(dataSource, (tileObjects) =>
{
    // B3mdRenderGroup에 신규/삭제 TileObject 반영
    group.UpdateTileObjects(tileObjects);
});
```

---

## 핵심 클래스 역할 정리

| 클래스 | 역할 |
|--------|------|
| `B3DMLoader` | b3dm 바이너리 파싱, magic/version 검증, featureTable JSON 읽기, GLB Stream 추출 |
| `B3dmObject` | `SceneObject` 상속, Unity 스타일 씬 노드 (LocalPosition, LocalRotation, SetActive 등) |
| `B3dmMesh` | 정점/인덱스/UV 데이터 보유, `CreateGeometryPrimitive(material, textureMap)` 로 렌더 프리미티브 생성 |
| `B3dmTileset` | `tileset.json` 파싱 결과 (root, children, boundingVolume, geometricError 등) |
| `B3dmTilesetTraversal` | 카메라-타일 거리 기반 LOD 선택, 로드/언로드 결정 |
| `CustomMultiTilesetBehaviour` | 멀티 tileset 관리 + `OnUpdate()` 로 매 프레임 타일 갱신 |
| `B3dmDataSource` | `SceneDataSource` 상속, `StartTile()` + `CreateRenderableGroup()` 제공 |
| `B3mdRenderGroup` | `RenderableGroup` 상속, SceneGroups에 추가되는 렌더 단위 |
| `WorldGlobe` | 지구 좌표 컨텍스트, `Initialize()` 로 원점 설정, `OnUpdate()` 로 타일 갱신 위임 |

---

## B3DM 파서 동작 상세 (`B3DMLoader`)

현재 구현 상태:
- magic (`0x6D643362` = `"b3dm"`) 검증
- version == 1 검증
- `featureTableJsonLength`, `featureTableBinaryLength` 읽기
- `BatchTableJsonLength` 읽기
- Feature Table JSON → `BATCH_LENGTH` 파싱
- **GLB Stream은 BinaryReader 포지션 이후 남은 바이트** → GLTF Loader로 전달

> `B3DMLoader`는 `ILoader` 인터페이스를 구현하며, 내부적으로 `GLTF.Loader.ILoader`를 래핑하는 데코레이터 패턴 사용.

---

## 확장 시나리오

| 단계 | 내용 |
|------|------|
| v0.1 | 단일 `.b3dm` 파일 직접 로드 (tileset 없이 B3DMLoader 직접 호출) |
| v0.2 | 로컬 `tileset.json` 기반 `StartTile(filePath)` |
| v1.0 | HTTP 서버 URL 기반 `StartTile(url)` (원격 스트리밍) |
| v1.1 | Batch Table 속성 파싱 → 클릭 픽킹 시 속성창 표시 |
| v1.2 | WorldGlobe 지형/위성 이미지와 B3DM 오버레이 |

---

## 참고

- **3DViewer 구조 참고**: `D:\work\PCDTest_bet4Bug\PCDTest\3DViewer.xaml.cs`
- **프로젝트 참조 참고**: `D:\work\PCDTest_bet4Bug\PCDTest\PCDTest.csproj`
- 기존 CityGMLViewer: `D:\work\MapPrimeNet\src\MapPrimeNet\A4L.Mapprime3DViewer\MainWindow.xaml.cs`
- B3DM 관련 기존 코드: `D:\work\MapPrimeNet\src\MapPrimeNet\A4L.Mapprime3DNet\IO\B3DM\`
- DataSource 패턴: `D:\work\MapPrimeNet\src\MapPrimeNet\A4L.Mapprime3DNet\IO\DataSource\B3dmDataSource.cs`
- 3D Tiles 스펙: https://github.com/CesiumGS/3d-tiles/blob/main/specification/TileFormats/Batched3DModel/README.md
