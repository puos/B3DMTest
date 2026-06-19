using A4L.MP3DCore.Common.Math;
using A4L.MP3DCore.Platform;
using A4L.MP3DCore.Scene.Animator;
using A4L.MP3DCore.Scene.Animator.CameraAnimator;
using A4L.MP3DCore.Scene.Camera;
using A4L.MP3DCore.Scene.InputHandler;
using A4L.MP3DCore.Scene.Renderer;
using A4L.MP3DCore.Scene.SceneGraph.Factory;
using A4L.MP3DCore.Scene.SceneGraph.Object;
using A4L.MP3DCore.Scene.SceneGraph.Primitive;
using KeyEventArgs = A4L.MP3DCore.Platform.KeyEventArgs;
using MouseEventArgs = A4L.MP3DCore.Platform.MouseEventArgs;

namespace A4L.MP3DCore.Scene.InputHandler.ViewingInputHandler;

public class CustomCameraController : InputHandlerBase
{
    public class KeySet : InputKeysBase
    {
        private const int _keyLeft = 0;
        private const int _keyRight = 1;
        private const int _keyFront = 2;
        private const int _keyBack = 3;
        private const int _keyUp = 4;
        private const int _keyDown = 5;
        private const int _firstPersionRotation = 6;
        private const int _screenCenterRotationdrag = 7;
        private const int _mousepointCenterRotationDrag = 8;
        private const int _screenpointCenterRotationdrag = 9;
        private const int _normalZoom = 10;
        private const int _lessZoom = 11;
        private const int _greaterZoom = 12;

        public KeySet()
        {
            KeyArr = new KeyBinding[]
            {
                new() { Key = Platform.Keys.A, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.D, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.W, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.S, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.E, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.Q, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.Alt },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.Shift },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.Control },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.None },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.Control },
                new() { Key = Platform.Keys.None, Modifiers = ModifierKeys.Shift }
            };
        }

        public KeyBinding KeyLeft => KeyArr[_keyLeft];
        public KeyBinding KeyRight { get => KeyArr[_keyRight]; set => KeyArr[_keyRight] = value; }
        public KeyBinding KeyFront { get => KeyArr[_keyFront]; set => KeyArr[_keyFront] = value; }
        public KeyBinding KeyBack { get => KeyArr[_keyBack]; set => KeyArr[_keyBack] = value; }
        public KeyBinding KeyUp { get => KeyArr[_keyUp]; set => KeyArr[_keyUp] = value; }
        public KeyBinding KeyDown { get => KeyArr[_keyDown]; set => KeyArr[_keyDown] = value; }
        public KeyBinding FirstPersionRotation { get => KeyArr[_firstPersionRotation]; set => KeyArr[_firstPersionRotation] = value; }
        public KeyBinding ScreenCenterRotationDrag { get => KeyArr[_screenCenterRotationdrag]; set => KeyArr[_screenCenterRotationdrag] = value; }
        public KeyBinding MousePointCenterRotationDrag { get => KeyArr[_mousepointCenterRotationDrag]; set => KeyArr[_mousepointCenterRotationDrag] = value; }
        public KeyBinding ScreenPointCenterRotationDrag { get => KeyArr[_screenpointCenterRotationdrag]; set => KeyArr[_screenpointCenterRotationdrag] = value; }
        public KeyBinding NormalZoom { get => KeyArr[_normalZoom]; set => KeyArr[_normalZoom] = value; }
        public KeyBinding LessZoom { get => KeyArr[_lessZoom]; set => KeyArr[_lessZoom] = value; }
        public KeyBinding GreaterZoom { get => KeyArr[_greaterZoom]; set => KeyArr[_greaterZoom] = value; }
    }

    public class Operation : InputOperationsBase
    {
        private const int _rotationDrag = 0;
        private const int _panningDrag = 1;
        private const int _zoomDrag = 2;
        private const int _moveIn = 3;
        private const int _centerFocus = 4;
        private const int _back = 5;

        public Operation()
        {
            Operations = new InputOperation[]
            {
                new DragInputOperation { Name = "RotationDrag" },
                new DragInputOperation { Name = "PanningDrag" },
                new DragInputOperation { Name = "ZoomDrag" },
                new PointInputOperation { Name = "MoveIn" },
                new PointInputOperation { Name = "CenterFocus" },
                new PointInputOperation { Name = "BACK" }
            };
        }

        public DragInputOperation RotationDragOperation => (DragInputOperation)Operations[_rotationDrag];
        public DragInputOperation PanningDragOperation => (DragInputOperation)Operations[_panningDrag];
        public DragInputOperation ZoomDragOperation => (DragInputOperation)Operations[_zoomDrag];
        public PointInputOperation MoveInOperation => (PointInputOperation)Operations[_moveIn];
        public PointInputOperation CenterFocusOperation => (PointInputOperation)Operations[_centerFocus];
        public PointInputOperation BackOperation => (PointInputOperation)Operations[_back];
    }

    public enum RotationTypes
    {
        None = 0,
        ScreenPointCenterRotationDrag,
        ScreenCenterRotationDrag,
        FirsrPersionRotation,
        MousePointCenterRotationDrag
    }

    private Vector3d _downPanningEye;
    private Vector3d _downPanningIp;
    private Vector3d _downPanningLookAt;
    private System.Drawing.Point _downPanningPoint = new(-1, -1);
    private Quaterniond _downPanningRot;
    private System.Drawing.Point _dragPanningPoint = new(-1, -1);
    private bool _panningStarted;

    public bool Movable = true;
    public double PannindgDistancePerPixelBoostRatio = 2.0f;
    public bool PanningPicking = false;
    public KeySet Keys { get; } = new();
    public double CustomSpeed { get; set; } = 3;
    public double BaseHeight { get; set; } = 0;
    public double PanningDistancePerPixel { get; set; } = 6000 / 1080;
    public bool PickPointCenterMoveZoom = true;
    public bool PointMoveMouse = true;
    public Operation Operations => (Operation)OperationsBase;

    private bool _bRotationStart;
    private int _currentMouseX = int.MinValue;
    private int _currentMouseY = int.MinValue;
    private System.Drawing.Point _downRotationPoint;
    private System.Drawing.Point _preRotationDragPoint;
    private Quaterniond _rotDown;
    private double _rotDownDist;
    private double _rotDownDist0;
    private Vector3d _rotDownIp;
    private Vector3d _rotDownLookAt0;
    private Vector3d _rotDownLootAt;
    private Ray _rotDownRay;
    private Quaterniond _rotDownRot0;
    private int _updateCurrentMouseTick = int.MinValue;
    private int _updatedCurrentMouseX = int.MinValue;
    private int _updatedCurrentMouseY = int.MinValue;
    private int _updatedMouseX = int.MinValue;
    private int _updatedMouseY = int.MinValue;

    public RotationTypes CurretnRotationType = RotationTypes.None;
    public float FirstPersonRotationHorizontalDegreePerWidth = 360;
    public float FirstPersonRotationVerticalDegreePerHeight = 90;
    public AABB PlaneBoundary = null;
    public bool PlaneEnable = true;
    public List<Plane> Planes = null;
    public RotationTypes RightRotType = RotationTypes.None;
    public bool Rotation3 = true;
    public bool RotationEnabled = true;
    public float RotationHorizontalDegreePerWidth = 180;
    public bool RotationModeFix = false;
    public float RotationVerticalDegreePerHeight = 90;

    private AnimatorBase _animatorSrs;
    private CameraAnimator _animator;
    private SelectiveRenderState _srs;
    private OrthoScaleSceneObject _downPointObj;

    public double ZoomMinDistance = 0.01;
    public double ZoomMaxDistance = double.MaxValue;
    public bool DragLineVisible { get; set; } = false;
    public int DragLinePixelSize { get; set; } = 8;
    public ColorF DragLineColor { get; set; } = ColorF.Red.Clone();
    public int DragSnapPixelSize { get; set; } = 16;
    public bool DragSnap { get; set; } = true;
    public RotationTypes MiddleDragRotationType = RotationTypes.FirsrPersionRotation;

    public CustomCameraController()
    {
        ID = MultiInput;
        PickContextVisible = true;
        PickType = PickTypes.WorldOnly;

        OperationsBase = new Operation();
        Operations.RotationDragOperation.DragFunc = onRotationDrag;
        Operations.RotationDragOperation.FirstDragFunc = onRotationDragDown;
        Operations.RotationDragOperation.UpFunc = onRotationDragUp;
        Operations.MoveInOperation.ClickFunc = onPointMoveAnimation;

        _wheel = new ScrollInputOperation();
        _wheel.ScrollFunc = zoom;

        _RDrag = Operations.RotationDragOperation;
        RdbClick = Operations.MoveInOperation;

        _LDrag = Operations.PanningDragOperation;
        _LDrag.DragFunc = onPanningDrag;
        _LDrag.FirstDragFunc = onPanningDragDown;
        _LDrag.UpFunc = onPanningDragUp;

        _MDrag = Operations.ZoomDragOperation;
        _MDrag.DragFunc = onMiddleDrag;
        _MDrag.FirstDragFunc = onMiddleDragDown;
        _MDrag.UpFunc = onMiddleDragUp;

        TouchDoubleTabAsLdbClick = false;
        TouchTabAsLClick = false;
        TouchZoomNRotAsWheel = false;
    }

    public override void Render(RenderContext context)
    {
        if (_srs != null)
        {
            if (_animatorSrs == null)
            {
                _animatorSrs = new AnimatorBase();
                _animatorSrs.EndModeValue = AnimatorBase.EndModes.RepeatMirror;
                _animatorSrs.RepeatCount = 0;
                _animatorSrs.TotalAnimationTime = 1;
                _animatorSrs.Start();
            }
            _animatorSrs.Update(context);
            _srs.DepthTest = _animatorSrs.CurrentRatio > 0.5f;
            _srs.DepthTest = false;
        }

        var bCull = context.ApplyCulling;
        context.ApplyCulling = false;
        if (_downPointObj != null)
            _downPointObj.Render(context);
        context.ApplyCulling = bCull;
    }

    protected override void dispose()
    {
        _animator = null;
        base.dispose();
    }

    public override void OnDeActive(InputHandlers handlers) { }
    public override void OnMouseLeave(EventArgs e, HandleContext context) { }
    public override void OnMouseMoveNoButton(MouseEventArgs e, HandleContext context) { }

    private void onMiddleDragUp(MouseEventArgs arg1, HandleContext arg2) => onRotationDragUp(arg1, arg2);
    private void onMiddleDragDown(MouseEventArgs arg1, HandleContext arg2)
    {
        CurretnRotationType = MiddleDragRotationType;
        onRotationDragDownFromCenter(arg1, arg2);
    }
    private void onMiddleDrag(MouseEventArgs arg1, HandleContext context) => onRotationDrag(arg1, context);

    public override void PreRender(RenderContext rc)
    {
        if (_animator != null)
            _animator.Update(rc);

        if ((_bRotationStart || _panningStarted) &&
            (_updatedMouseX != _currentMouseX || _updatedMouseY != _currentMouseY))
        {
            float step = 100;
            var upos = new Vector2d(_updatedMouseX, _updatedMouseY);
            var tpos = new Vector2d(_currentMouseX, _currentMouseY);
            var ldir = (tpos - upos).GetNormalized();
            var dist = tpos.GetDistance(upos);
            dist = Math.Min(dist, step);
            upos = upos + ldir * dist;

            var cx = (int)Math.Round(upos.X);
            var cy = (int)Math.Round(upos.Y);
            _updatedMouseX = cx;
            _updatedMouseY = cy;

            var tick = Environment.TickCount;
            var timeStep = 100;
            var offTick = tick - _updateCurrentMouseTick;
            if (offTick == 0) offTick = timeStep;
            if (offTick > timeStep) offTick = timeStep;
            var ratio = (float)offTick / timeStep;
            var r2 = ratio * Math.PI * 0.5;
            ratio = (float)Math.Sin(r2);
            if (ratio < 0) ratio = 0;
            if (ratio > 1) ratio = 1;

            _updatedMouseX = _currentMouseX;
            _updatedMouseY = _currentMouseY;

            if (_bRotationStart)
                onUpdateRotation(_updatedMouseX, _updatedMouseY, rc);
        }
    }

    private void updateDrag(int mx, int my)
    {
        _currentMouseX = mx; _currentMouseY = my;
        _updatedCurrentMouseX = _updatedMouseX; _updatedCurrentMouseY = _updatedMouseY;
        _updateCurrentMouseTick = Environment.TickCount;
    }

    private void startDrag(int x, int y)
    {
        _currentMouseX = x; _currentMouseY = y;
        _updatedMouseX = x; _updatedMouseY = y;
        _updatedCurrentMouseX = x; _updatedCurrentMouseY = y;
    }

    private void stopAnimation()
    {
        if (_animator != null) { _animator.Cancel(); _animator = null; }
    }

    private void setAnimator(string op, HandleContext context, KeyFrameCameraAnimator ani, bool history = true)
    {
        if (_animator != null) { _animator.Cancel(); _animator = null; }
        _animator = ani;
        if (ani != null && history)
        {
            context.StopViewChanging = true;
            ani.AddEndFunc(delegate (AnimatorBase ani2)
            {
                var ani3 = (KeyFrameCameraAnimator)ani2;
                if (ani3.Current.LookAt == null) return;
                context.StopViewChanging = false;
                context.View.Add(new ViewPoint(op, ani3.Current));
            });
        }
    }

    private void updatePointer(Vector3d point, Vector3d normal, bool pick = false)
    {
        if (point == null) return;
        updatePointerCircle(point, normal, pick);
    }

    private void clearPointer() => _downPointObj = null;

    private void updatePointerCircle(Vector3d point, Vector3d normal, bool pick)
    {
        if (_srs == null) _srs = new SelectiveRenderState();
        var srs = _srs;
        srs.EnableAlphaBlending(true);
        srs.EnableLighting(false);
        srs.EnableCull(false);
        srs.EnableDepthOrder(1);
        srs.EnableDepthTest(false);

        normal = Vector3d.ZAxis.Clone();
        var c = pick ? ColorF.White.Clone(0.125f) : ColorF.Red.Clone(0.125f);
        var spCenter = GeometryFactory.FromCircle(0, 0, 0, 0.15f, 16, false, Material.GetColorMaterial(ColorF.Yellow), srs);
        var sp = GeometryFactory.FromCircle(0, 0, 0, 1.25f, 16, false, Material.GetColorMaterial(c), srs);

        var ortho = new OrthoScaleSceneObject();
        _downPointObj = ortho;
        _downPointObj.Transform = new MatrixTransform();
        if (normal != null)
            _downPointObj.MatrixTransform.SetRotation(Quaterniond.GetRotate(Vector3d.ZAxis, normal));
        _downPointObj.MatrixTransform.Translation = new Vector3d(point);
        _downPointObj.UpdateMatrix();
        _downPointObj.Add(sp);
        _downPointObj.Add(spCenter);
        _downPointObj.TargetSize = 24;
        _downPointObj.PreScale = 1;
        _downPointObj.SetUpdateFrame(true);
    }

    private void extendLookAtWithoutEyeMove(Camera.Camera cam, Vector3d point)
    {
        Vector3d newLookAt = null;
        var r = new Ray(cam.GetEye(), cam.GetViewVector());
        if (!Plane.GetIntersectPointNormal(r, point, cam.GetViewVector(), out newLookAt)) return;
        var eye0 = cam.GetEye();
        var dist = eye0.GetDistance(newLookAt);
        cam.LookAt = newLookAt;
        cam.Distance = dist;
    }

    internal void onPanningDrag(MouseEventArgs e, HandleContext context)
    {
        var mx = e.X; var my = e.Y;
        if (!_panningStarted || _downPanningIp == null) return;

        if (DragSnap)
        {
            var dist = Math.Sqrt((mx - _downPanningPoint.X) * (mx - _downPanningPoint.X) +
                                 (my - _downPanningPoint.Y) * (my - _downPanningPoint.Y));
            if (dist < DragSnapPixelSize) { mx = _downPanningPoint.X; my = _downPanningPoint.Y; }
        }

        var ray = context.View.GetRayFromScreenCoord(mx, my, _downPanningRot, _downPanningEye, context.View.Camera.Distance);
        var planeNormal = new Vector3d(0, 0, 1);

        Vector3d ip = null;
        if (!Plane.GetIntersectPointNormal(ray, _downPanningIp, planeNormal, out ip))
        {
            var viewDir = (_downPanningLookAt - _downPanningEye).GetNormalized();
            Plane.GetIntersectPointNormal(ray, _downPanningIp, viewDir.GetNegated(), out ip);
            if (ip == null) return;
        }

        var offset = _downPanningIp - ip;
        context.View.Camera.LookAt = _downPanningLookAt + offset;
        _dragPanningPoint.X = mx; _dragPanningPoint.Y = my;
    }

    private void onPanningDragDown(MouseEventArgs e, HandleContext context)
    {
        var x = e.X; var y = e.Y;
        stopAnimation();
        if (Movable == false) return;

        var ray = context.View.GetRayFromScreenCoord(x, y);
        Vector3d normal = null; Vector3d ip = null;
        bool bPick = pickTrans(ray, context, out ip, out normal);

        if (normal != null)
        {
            var worldUp = new Vector3d(0, 0, 1);
            if (Vector3d.Dot(ray.Direction, worldUp) > 0) return;
        }

        context.StopViewChanging = true;
        _panningStarted = false; _bRotationStart = false;
        _downPanningPoint = new System.Drawing.Point(x, y);
        _downPanningIp = ip;
        _downPanningEye = context.View.Camera.GetEye();
        _downPanningLookAt = new Vector3d(context.View.Camera.LookAt);
        _downPanningRot = context.View.Camera.Quat.Clone();
        _panningStarted = true;
        startDrag(x, y);
        SetMouseOverEnable(context.RenderTarget, false);
        context.SetGlobalCursor(Resource.ECursor.Panning);
        updatePointer(ip, normal, bPick);
    }

    private void onPanningDragUp(MouseEventArgs e, HandleContext context)
    {
        if (_panningStarted == false) return;
        _panningStarted = false;
        context.StopViewChanging = false;
        reseatLookAtOnSurface(context);
        context.View.Add("panning", context.View.Camera);
        context.RestoreGlobalCursor();
        clearPointer();
        SetMouseOverEnable(context.RenderTarget, true);
    }

    private bool reseatLookAtOnSurface(HandleContext context)
    {
        var cam = context.View.Camera;
        var eye = cam.GetEye();
        var ray = context.View.GetRayFromScreenCoord(context.View.Width / 2, context.View.Height / 2);
        Vector3d ip = null; Vector3d normal = null;
        if (!pickTrans(ray, context, out ip, out normal) || ip == null) return false;
        var dist = eye.GetDistance(ip);
        if (dist < ZoomMinDistance) dist = ZoomMinDistance;
        if (dist > ZoomMaxDistance) dist = ZoomMaxDistance;
        cam.LookAt = ip;
        cam.Distance = dist;
        return true;
    }

    private bool pickTrans(Ray ray, HandleContext context, out Vector3d ip, out Vector3d outNormal)
    {
        ip = null; outNormal = null;
        PickedIntersections infos = null;
        GetPickPoints(ray, context, false,  out infos);
        infos.Intersections.RemoveAll(x => x.PickType == PickedIntersection.PickedTypes.Plane);

        if (infos.Count <= 0)
        {
            var pc = new PickContext(context.RenderTarget, ray, PickContextVisible, context.RenderTarget.CullingFace);
            var pl = new Plane(context.View.Camera.LookAt, context.View.Camera.GetViewVector().GetNegated());
            if (pl.IntersectPoint(pc.Ray, out ip))
            {
                var pi = new PickedIntersection();
                pi.PickedPoint = ip; pi.PickedNormal = pl.Normal;
                pi.Distance = pc.Ray.Origin.GetDistance(ip);
                pi.PickedName = "LookATPlane";
                pi.PickType = PickedIntersection.PickedTypes.Plane;
                infos.Intersections.Add(pi);
            }
        }

        infos.Sort();
        if (infos.Count > 0)
        {
            var first = infos.GetFirst();
            ip = first.PickedPoint; outNormal = first.PickedNormal;
            if (outNormal == null) outNormal = context.View.Camera.GetViewVector().GetNegated();
            return first.PickType != PickedIntersection.PickedTypes.Plane;
        }
        return false;
    }

    private bool pickRot(Ray ray, HandleContext context, out Vector3d ip, out Vector3d outNormal)
    {
        ip = null; outNormal = null;
        PickedIntersections infos = null;
        var hasPick = GetPickPoints(ray, context, false,  out infos);

        if (hasPick && infos != null)
        {
            infos.Intersections.RemoveAll(x => x.PickType == PickedIntersection.PickedTypes.Plane);
            if (infos.Count > 0)
            {
                infos.Sort();
                var first = infos.GetFirst();
                if (first?.PickedPoint != null)
                {
                    ip = first.PickedPoint; outNormal = first.PickedNormal;
                    if (outNormal == null) outNormal = context.View.Camera.GetViewVector().GetNegated();
                    return true;
                }
            }
        }

        var fallbackPlane = new Plane(context.View.Camera.LookAt, context.View.Camera.GetViewVector().GetNegated());
        if (fallbackPlane.IntersectPoint(ray, out ip) && ip != null)
        {
            outNormal = context.View.Camera.GetViewVector().GetNegated();
            return false;
        }

        ip = context.View.Camera.LookAt.Clone();
        outNormal = context.View.Camera.GetViewVector().GetNegated();
        return false;
    }

    private void onRotationDragUp(MouseEventArgs e, HandleContext context) => onRotationDragUp(e.X, e.Y, context);

    private void onRotationDragUp(int x, int y, HandleContext context)
    {
        if (_bRotationStart)
        {
            reseatLookAtOnSurface(context);
            context.View.Add("rot", context.View.Camera);
        }
        context.StopViewChanging = false;
        context.RestoreGlobalCursor();
        _bRotationStart = false;
        clearPointer();
        SetMouseOverEnable(context.RenderTarget, true);
    }

    internal void onRotationDragDown(MouseEventArgs e, HandleContext context)
    {
        if (!RotationModeFix)
        {
            RightRotType = RotationTypes.None;
            if (RightRotType == RotationTypes.None && KeyEventArgs.IsKeyDown(Keys.MousePointCenterRotationDrag))
                RightRotType = RotationTypes.MousePointCenterRotationDrag;
            if (RightRotType == RotationTypes.None && KeyEventArgs.IsKeyDown(Keys.ScreenCenterRotationDrag))
                RightRotType = RotationTypes.ScreenCenterRotationDrag;
            if (RightRotType == RotationTypes.None && KeyEventArgs.IsKeyDown(Keys.ScreenPointCenterRotationDrag))
                RightRotType = RotationTypes.ScreenPointCenterRotationDrag;
            if (RightRotType == RotationTypes.None)
                RightRotType = RotationTypes.ScreenPointCenterRotationDrag;
        }
        CurretnRotationType = RightRotType;
        onRotationDragDownFromCenter(e, context);
    }

    internal void onRotationDragDownFromCenter(MouseEventArgs e, HandleContext context)
        => onRotationDragDownFromCenterXY(e.X, e.Y, context);

    internal void onRotationDragDownFromCenterXY(int x, int y, HandleContext context)
    {
        stopAnimation();
        if (RotationEnabled == false) return;
        if (!Rotation3 && CurretnRotationType != RotationTypes.FirsrPersionRotation) return;

        _panningStarted = false; _bRotationStart = false;
        context.StopViewChanging = true;

        _rotDownLookAt0 = context.View.Camera.LookAt.Clone();
        _rotDownRot0 = context.View.Camera.Quat.Clone();
        _rotDownDist0 = context.View.Camera.Distance;

        var ray = context.View.GetRayFromScreenCoord(x, y);
        Vector3d ip = null; Vector3d normal = null;
        var bPick = pickRot(ray, context, out ip, out normal);

        extendLookAtWithoutEyeMove(context.View.Camera, ip);
        if (CurretnRotationType == RotationTypes.MousePointCenterRotationDrag ||
            CurretnRotationType == RotationTypes.ScreenPointCenterRotationDrag)
        {
            _rotDownDist = context.View.Camera.Distance;
            _rotDownIp = ip; _rotDownRay = ray;
            _rotDown = context.View.Camera.Quat.Clone();
            _rotDownLootAt = context.View.Camera.LookAt.Clone();
            updatePointer(ip, normal, bPick);
        }

        _preRotationDragPoint = new System.Drawing.Point(x, y);
        _downRotationPoint = new System.Drawing.Point(x, y);
        context.SetGlobalCursor(Resource.ECursor.Rotate);
        _bRotationStart = true;
        startDrag(x, y);
        SetMouseOverEnable(context.RenderTarget, false);
    }

    internal void onUpdateRotation(int mx, int my, RenderContext rc)
    {
        var view = rc.View;
        if (_rotDown == null || _bRotationStart == false) return;

        if (DragSnap)
        {
            var dist = Math.Sqrt((mx - _downRotationPoint.X) * (mx - _downRotationPoint.X) +
                                 (my - _downRotationPoint.Y) * (my - _downRotationPoint.Y));
            if (dist < DragSnapPixelSize) { mx = _downRotationPoint.X; my = _downRotationPoint.Y; }
        }

        if (CurretnRotationType == RotationTypes.FirsrPersionRotation ||
            CurretnRotationType == RotationTypes.ScreenCenterRotationDrag)
        {
            double degZ = mx - _downRotationPoint.X;
            double degX = my - _downRotationPoint.Y;
            degZ = Math.Sin(MathUtil.ToRadian(degZ / rc.View.Width * 90)) * rc.View.Width;
            degX = Math.Sin(MathUtil.ToRadian(degX / rc.View.Height * 90)) * rc.View.Height;
            degZ = degZ / view.Width * FirstPersonRotationHorizontalDegreePerWidth;
            degX = degX / view.Height * FirstPersonRotationVerticalDegreePerHeight;
            view.Camera.Quat = _rotDown.Clone();
            view.Camera.RotateByZAxis(-degZ);
            view.Camera.RotateByRight(-degX);
        }
        else
        {
            double degZ = mx - _downRotationPoint.X;
            double degX = my - _downRotationPoint.Y;
            degZ = Math.Sin(MathUtil.ToRadian(degZ / rc.View.Width * 90)) * rc.View.Width;
            degX = Math.Sin(MathUtil.ToRadian(degX / rc.View.Height * 90)) * rc.View.Height;
            degZ = degZ / view.Width * RotationHorizontalDegreePerWidth;
            degX = degX / view.Height * RotationVerticalDegreePerHeight;

            var cx = CurretnRotationType == RotationTypes.MousePointCenterRotationDrag ? mx : _downRotationPoint.X;

            var newQ = ViewUtil.GetRotateByZAxis(_rotDown, -degZ);
            newQ = ViewUtil.GetRotateByRight(newQ, -degX);
            var newQ2 = ViewUtil.GetRotateByZAxis(Quaterniond.IDENTITY, degZ);
            newQ2 = ViewUtil.GetRotateByRight(newQ2, degX);

            var newEye = _rotDownRay.Origin - _rotDownIp;
            newEye = newEye * newQ2;
            newEye = newEye + _rotDownIp;

            var viewVector = ViewUtil.GetViewVector(newQ);

            var degZOff = (mx - _downRotationPoint.X) * (mx - _downRotationPoint.X);
            var degXOff = (my - _downRotationPoint.Y) * (my - _downRotationPoint.Y);
            var ratio = (degZOff + degXOff) / (double)(256 * 256);
            ratio = MathUtil.Saturate(0, 1, ratio);
            ratio = ratio * ratio;

            var at0 = _rotDownLookAt0 * (1 - ratio) + _rotDownLootAt * ratio;
            var dist = _rotDownDist0 * (1 - ratio) + _rotDownDist * ratio;
            var eye = ViewUtil.GetEye(at0, newQ, dist);

            var newray = view.GetRayFromScreenCoord(cx, cx, newQ, eye, dist);
            Vector3d ip2 = null;
            if (!Plane.GetIntersectPointNormal(newray, _rotDownIp, viewVector, out ip2)) return;

            var newat = at0 + (_rotDownIp - ip2);
            view.Camera.LookAt = newat;
            view.Camera.Quat = newQ;
            view.Camera.Distance = dist;
        }

        _preRotationDragPoint = new System.Drawing.Point(mx, my);
    }

    internal void onRotationDrag(MouseEventArgs e, HandleContext context) => updateDrag(e.X, e.Y);

    internal void zoom(MouseEventArgs e, HandleContext context)
    {
        var cam = context.View.Camera;
        var dist = cam.Distance;
        var wheel = Math.Clamp(-(double)e.Delta / 120.0, -1.0, 1.0);
        var zoomStep = Math.Clamp(dist * 0.12, 0.02, 100.0);
        var nextDist = dist + wheel * zoomStep;
        if (nextDist < ZoomMinDistance) nextDist = ZoomMinDistance;
        if (nextDist > ZoomMaxDistance) nextDist = ZoomMaxDistance;
        cam.Distance = nextDist;
        context.View.Add("zoom", cam);
    }

    private void onPointMoveAnimation(MouseEventArgs e, HandleContext context)
    {
        if (!Movable) return;
        var view = context.View;
        var cam = view.Camera;
        var ray = context.View.GetRayFromScreenCoord(e.X, e.Y);
        Vector3d ip = null; Vector3d normal = null;
        var bPick = pickTrans(ray, context, out ip, out normal);
        if (ip == null) return;

        var eye = context.View.Camera.GetEye();
        var vidot = (ip - eye) * context.View.Camera.GetViewVector();
        var dist = vidot * 0.5;
        if (dist >= ZoomMaxDistance) dist = ZoomMaxDistance;

        var newEye = eye + context.View.Camera.GetViewVector() * dist;
        ray = context.View.GetRayFromScreenCoord(e.X, e.Y, cam.Quat, newEye, dist);

        Vector3d ip2 = null;
        Plane.GetIntersectPointNormal(ray, ip, context.View.Camera.GetViewVector(), out ip2);
        if (ip2 == null) return;

        var offset = ip - ip2;
        newEye = newEye + offset;
        var at = newEye + context.View.Camera.GetViewVector() * dist;
        var q = context.View.Camera.Quat.Clone();

        var ani = new KeyFrameCameraAnimator();
        ani.Add(0, context.View.Camera);
        ani.Add(0.5, at, q, dist);
        ani.Start();
        setAnimator("point Move", context, ani);

        updatePointer(ip, normal, bPick);
        ani.AddEndFunc(delegate { clearPointer(); });
    }
}
