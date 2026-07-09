# B3DMTest Task List

## 목표
MapPrime3D(`A4L.Mapprime3DNet` / `A4L.MP3DCore`) DLL을 사용하여
단일 `.b3dm` 파일을 로드하고 WPF 뷰어에서 렌더링하는 예제 구현

---

## Tasks

### [T-01] 프로젝트 셋업 ✅
- [x] WPF 프로젝트 생성 (`.NET 8`, `net8.0-windows`)
- [x] ProjectReference 추가 (PCDTest 방식)
  - `A4L.Mapprime3DNet.csproj`
  - `A4L.MP3DCore.csproj`
- [x] NuGet 패키지 추가
  - `NetTopologySuite.IO.ShapeFile`
  - `System.Management`
  - `MathNet.Numerics`
- [x] `AllowUnsafeBlocks=true` 설정
- [x] 빌드 확인 (0 errors)
- [x] 구조: `B3DMTest/src/B3DMTest.csproj`, `B3DMTest.sln`

---

### [T-02] 3DViewer 윈도우 생성 ✅
> PCDTest의 `TDViewer` (`3DViewer.xaml.cs`) 패턴을 그대로 가져온다

- [x] `Viewer3D.xaml` / `Viewer3D.xaml.cs` 생성
- [x] `clientViewGrid` (Grid) XAML 배치 (3-row: 툴바 36px / 뷰 * / 상태바 24px)
- [x] `InitializeScene()` 구현
  - [x] `ObjectsViewWPF` 생성 및 Grid에 추가
  - [x] `ViewController` 연결
  - [x] `CustomCameraController` 설정 (`ZoomMinDistance = 1.0`)
  - [x] `HUD3AxisInputHandler` 추가 (LeftBottom)
  - [x] `SceneView.RenderAbort` 콜백 설정
  - [x] `SceneView.ShowDebugFPS = true`
  - [x] 배경색 설정 (`0.06f, 0.10f, 0.20f`)
  - [x] `BackFaceCulling = true`
- [x] `ConfigureNearFar()` 구현 (Near=0.1, Far=10000)
- [x] `Workspace.Instance` 초기화
- [x] `Tools/CustomCameraController.cs` (PCDTest 복사, LidarRecon 의존성 제거)

---

### [T-03] B3DM 로드 기능 구현 ✅ (코드 완성, 실파일 테스트 필요)
- [x] 파일 열기 버튼 → `OpenFileDialog` (`.b3dm` 필터)
- [x] `NearFarUpdater.FarUpdateEnable = false`, `TargetFar = 200000` 설정
- [x] `WorldGlobe.Instance.Initialize(view, lat, lon, alt)` 호출
- [x] `B3dmDataSource(WorldGlobe.Instance)` 생성
- [x] `dataSource.StartTile(path)` 호출 (async void → fire-and-forget)
- [x] `dataSource.CreateRenderableGroup()` → `SceneGroups.Add()`
- [x] `Workspace.Instance.DataSources.Add(dataSource)`
- [x] `CommandUi.RefreshSceneGroup()` 호출

---

### [T-04] Update 루프 연결 ✅
- [x] `DispatcherTimer` (100ms 간격) 생성
- [x] Tick에서 카메라 위치 상태바 갱신
- [x] Tick에서 `WorldGlobe.Instance.OnUpdate()` 호출

---

### [T-05] 카메라 자동 포커스 (ZoomFit) ✅ (코드 완성, 실파일 테스트 필요)
- [x] BoundingBox 기반 거리 계산
- [x] `camera.LookAt`, `camera.Distance` 설정
- [x] `GlobalOption.Far` 자동 조정

---

### [T-06] 기본 UI ✅ (Drag & Drop 미완)
- [x] 파일 열기 버튼
- [x] 카메라 위치 상태바 (`X, Y, Z`)
- [x] 로딩 상태 표시 (`txtStatus`)
- [ ] Drag & Drop 지원 (`.b3dm` 파일 드롭) — 미구현

---

### [T-07] 동작 확인 및 디버깅
- [ ] 샘플 `.b3dm` 파일로 렌더링 확인
- [ ] `B3DMLoader` magic 검증 통과 확인 (`0x6D643362`)
- [ ] `B3dmObject` 씬 등록 및 `ActiveSelf` 확인
- [ ] 마우스 회전/줌 동작 확인
- [ ] `SceneView.ShowDebugFPS` 로 FPS 확인

---

## 진행 상태

| Task | 상태 |
|------|------|
| T-01 프로젝트 셋업 | ✅ 완료 |
| T-02 3DViewer 윈도우 | ✅ 완료 |
| T-03 B3DM 로드 | ✅ 코드 완성 |
| T-04 Update 루프 | ✅ 완료 |
| T-05 카메라 ZoomFit | ✅ 코드 완성 |
| T-06 기본 UI | ✅ 완료 (Drag&Drop 제외) |
| T-07 동작 확인 | ⏳ 대기 — 실파일 필요 |

---

## 참고 파일
- 시나리오 문서: `doc/b3dm-loading-scenario.md`
- **3DViewer 참고**: `D:\work\PCDTest_bet4Bug\PCDTest\3DViewer.xaml.cs`
- **프로젝트 참조 참고**: `D:\work\PCDTest_bet4Bug\PCDTest\PCDTest.csproj`
- CityGMLViewer 참고: `D:\work\MapPrimeNet\src\MapPrimeNet\A4L.Mapprime3DViewer\MainWindow.xaml.cs`
- B3DM 기존 코드: `D:\work\MapPrimeNet\src\MapPrimeNet\A4L.Mapprime3DNet\IO\B3DM\`
