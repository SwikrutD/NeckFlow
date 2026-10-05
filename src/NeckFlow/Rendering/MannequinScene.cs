using System.Windows.Media;
using System.Windows.Media.Media3D;
using NeckFlow.Models;
using MediaColor = System.Windows.Media.Color;

namespace NeckFlow.Rendering;

/// <summary>
/// Stylised upper-body mannequin posed for one exercise. The figure faces +Z, +X is the figure's left (viewer's right
/// from the front) and Y is up. Head, shoulder girdles and shoulder blades are rigid parts driven by matrices; the neck,
/// trapezius slopes, highlighted muscles and motion arrows are swept tubes rebuilt from anchor points every frame.
/// </summary>
internal sealed class MannequinScene
{
    private static readonly Point3D NeckBase = new(0, .08, -.03);
    private static readonly Point3D HeadPivotRest = new(0, .57, .02);
    private const double NeckRadius = .2;
    private const double TorsoExponent = 2.7;

    private static readonly MediaColor Body = MediaColor.FromRgb(176, 190, 211);
    private static readonly MediaColor Detail = MediaColor.FromRgb(42, 52, 70);
    private static readonly MediaColor MuscleRest = MediaColor.FromRgb(226, 142, 128);
    private static readonly MediaColor MuscleHot = MediaColor.FromRgb(255, 52, 36);
    private static readonly MediaColor Accent = MediaColor.FromRgb(16, 178, 204);

    // Torso cross-sections from the top of the neck opening down to the waist: (y, half width, half depth).
    private static readonly (double Y, double W, double D)[] TorsoProfile =
    {
        (.30, 0, 0), (.29, .13, .11), (.25, .24, .19), (.16, .48, .26), (.06, .70, .32), (-.08, .80, .37),
        (-.40, .79, .41), (-.90, .71, .39), (-1.50, .63, .35), (-1.62, .52, .28), (-1.64, 0, 0)
    };

    private readonly ExerciseKind _kind;
    private readonly MatrixTransform3D _headTransform = new();
    private readonly MatrixTransform3D[] _girdleTransform = { new(), new() };
    private readonly MatrixTransform3D[] _scapulaTransform = { new(), new() };
    private readonly GeometryModel3D _neck;
    private readonly GeometryModel3D[] _trapSlope = new GeometryModel3D[2];
    private readonly List<Muscle> _muscles = new();
    private readonly List<Arrow> _arrows = new();
    private readonly Model3DGroup _lights = new();
    private readonly (double Yaw, double Elevation, double TargetY, double Distance) _defaultView;
    private double _viewYaw, _viewElevation;

    private Matrix3D _head = Matrix3D.Identity;
    private Point3D _headPivot = HeadPivotRest;
    private readonly Matrix3D[] _girdle = { Matrix3D.Identity, Matrix3D.Identity };
    private readonly Matrix3D[] _scapula = { Matrix3D.Identity, Matrix3D.Identity };

    public Model3DGroup Root { get; } = new();
    public PerspectiveCamera Camera { get; } = new() { FieldOfView = 46, UpDirection = new Vector3D(0, 1, 0) };
    public Model3DGroup Lights => _lights;

    public MannequinScene(ExerciseKind kind)
    {
        _kind = kind;
        var skin = Matte(Body);
        Root.Children.Add(new GeometryModel3D(Torso(), skin));

        _neck = new GeometryModel3D { Material = skin }; Root.Children.Add(_neck);
        var head = new Model3DGroup { Transform = _headTransform };
        head.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(0, .4, .02), new Vector3D(.35, .44, .41)), skin));
        head.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(0, .2, .1), new Vector3D(.28, .23, .3)), skin));
        head.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(0, .33, .42), new Vector3D(.05, .085, .08), 12, 16), skin));
        foreach (var s in Sides)
        {
            head.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(s * .345, .36, -.02), new Vector3D(.06, .11, .085), 12, 16), skin));
            head.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(s * .13, .45, .375), new Vector3D(.045, .045, .035), 10, 14), Matte(Detail)));
        }
        Root.Children.Add(head);

        foreach (var s in Sides)
        {
            var i = Index(s);
            var girdle = new Model3DGroup { Transform = _girdleTransform[i] };
            girdle.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(s * .79, -.04, 0), new Vector3D(.22, .26, .24)), skin));
            girdle.Children.Add(new GeometryModel3D(Mesh.Tube(new[] { new Point3D(s * .82, -.02, .0), new Point3D(s * .90, -.65, .0), new Point3D(s * .93, -1.18, .03) }, u => .17 - .045 * u), skin));
            Root.Children.Add(girdle);
            _trapSlope[i] = new GeometryModel3D { Material = skin }; Root.Children.Add(_trapSlope[i]);
            Root.Children.Add(new GeometryModel3D(Mesh.Ellipsoid(new Point3D(0, 0, 0), new Vector3D(.17, .24, .045), 14, 18), skin) { Transform = _scapulaTransform[i] });
        }

        BuildHighlights();
        foreach (var muscle in _muscles) Root.Children.Add(muscle.Model);
        BuildArrows();
        foreach (var arrow in _arrows) Root.Children.Add(arrow.Model); // transparent parts last so they blend over the body

        _defaultView = kind switch
        {
            ExerciseKind.ChinTuck => (78, 8, .45, 4.4),
            ExerciseKind.LookLeftRight => (14, 30, .6, 5.0),
            ExerciseKind.SideTilt => (6, 6, .55, 5.3),
            ExerciseKind.ShoulderRolls => (70, 14, .3, 5.8),
            _ => (162, 14, .05, 4.9)
        };
        ResetView();
        Update(0);
    }

    private static readonly int[] Sides = { -1, 1 };
    private static int Index(int side) => side < 0 ? 0 : 1;

    /// <summary>One seamless loop of the animation, used for previews: (start time, length) in seconds.</summary>
    public static (double Start, double Length) PreviewWindow(ExerciseKind kind) => kind switch
    {
        ExerciseKind.LookLeftRight or ExerciseKind.SideTilt => (0, 10),
        ExerciseKind.ShoulderRolls => (4, 8),
        _ => (0, 6)
    };

    // ---------------------------------------------------------------- camera and lighting

    public void ResetView() { _viewYaw = _defaultView.Yaw; _viewElevation = _defaultView.Elevation; ApplyView(); }

    public void Orbit(double yawDegrees, double elevationDegrees)
    {
        _viewYaw += yawDegrees; _viewElevation = Math.Clamp(_viewElevation + elevationDegrees, -25, 60); ApplyView();
    }

    private void ApplyView()
    {
        var target = new Point3D(0, _defaultView.TargetY, 0);
        Camera.Position = target + Direction(_viewYaw, _viewElevation) * _defaultView.Distance;
        Camera.LookDirection = target - Camera.Position;
        _lights.Children.Clear();
        _lights.Children.Add(new AmbientLight(MediaColor.FromRgb(74, 80, 96)));
        _lights.Children.Add(new DirectionalLight(MediaColor.FromRgb(236, 234, 228), -Direction(_viewYaw - 38, 38)));
        _lights.Children.Add(new DirectionalLight(MediaColor.FromRgb(86, 98, 124), -Direction(_viewYaw + 60, 5)));
        _lights.Children.Add(new DirectionalLight(MediaColor.FromRgb(150, 170, 205), -Direction(_viewYaw + 165, 25)));
    }

    private static Vector3D Direction(double yawDegrees, double elevationDegrees)
    {
        double yaw = yawDegrees * Math.PI / 180, el = elevationDegrees * Math.PI / 180;
        return new Vector3D(Math.Sin(yaw) * Math.Cos(el), Math.Sin(el), Math.Cos(yaw) * Math.Cos(el));
    }

    // ---------------------------------------------------------------- animation

    public void Update(double t)
    {
        double yaw = 0, roll = 0, pitch = 0, tuck = 0, scapulaIn = 0; var shoulder = new Vector3D();
        switch (_kind)
        {
            case ExerciseKind.ChinTuck:
            {
                var v = HoldCycle(t);
                tuck = -.17 * v; pitch = 5 * v;
                SetIntensity("flexor", .25 + .75 * v); SetIntensity("suboccipital", .25 + .75 * v);
                SetArrow("back", v);
                break;
            }
            case ExerciseKind.LookLeftRight:
            {
                var v = SwingCycle(t); yaw = 60 * v;
                // Turning toward one side is driven by the opposite sternocleidomastoid.
                SetIntensity("scm-1", .25 + .75 * Math.Max(0, v)); SetIntensity("scm1", .25 + .75 * Math.Max(0, -v));
                SetArrow("turn1", Math.Max(0, v)); SetArrow("turn-1", Math.Max(0, -v));
                break;
            }
            case ExerciseKind.SideTilt:
            {
                var v = SwingCycle(t); roll = 34 * v;
                // Positive roll drops the head toward the figure's right, stretching the left (+X) side.
                foreach (var s in Sides) { var k = .2 + .8 * Math.Max(0, s * v); SetIntensity($"trap{s}", k); SetIntensity($"scalene{s}", k); }
                SetArrow("tilt1", Math.Max(0, v)); SetArrow("tilt-1", Math.Max(0, -v));
                break;
            }
            case ExerciseKind.ShoulderRolls:
            {
                var amp = Smooth(Math.Min(1, t / 1.2)); var a = 2 * Math.PI * t / 4;
                shoulder = new Vector3D(0, .14 * Math.Sin(a), .10 * Math.Cos(a)) * amp;
                var lift = .3 + .7 * amp * (.5 + .5 * Math.Sin(a));
                foreach (var s in Sides) { SetIntensity($"trap{s}", lift); SetIntensity($"levator{s}", lift); }
                SetArrow("roll", amp);
                break;
            }
            case ExerciseKind.ShoulderBladeSqueeze:
            {
                var v = HoldCycle(t);
                shoulder = new Vector3D(0, -.035, -.11) * v; scapulaIn = .085 * v;
                foreach (var s in Sides) SetIntensity($"rhomboid{s}", .25 + .75 * v);
                SetArrow("squeeze", v);
                break;
            }
        }

        // Head: rotate about the top of the neck, and carry the pivot along an arc about the neck base so tilts and
        // nods bend the whole neck rather than spinning the head on a stick.
        var bend = Matrix3D.Identity;
        bend.RotateAt(new Quaternion(new Vector3D(0, 0, 1), roll * .45), NeckBase);
        bend.RotateAt(new Quaternion(new Vector3D(1, 0, 0), pitch * .45), NeckBase);
        _headPivot = bend.Transform(HeadPivotRest) + new Vector3D(0, 0, tuck);
        _head = Matrix3D.Identity;
        _head.Rotate(new Quaternion(new Vector3D(0, 0, 1), roll));
        _head.Rotate(new Quaternion(new Vector3D(1, 0, 0), pitch));
        _head.Rotate(new Quaternion(new Vector3D(0, 1, 0), yaw));
        _head.Translate((Vector3D)_headPivot);
        _headTransform.Matrix = _head;

        foreach (var s in Sides)
        {
            var i = Index(s);
            _girdle[i] = Matrix3D.Identity; _girdle[i].Translate(shoulder); _girdleTransform[i].Matrix = _girdle[i];
            // Shoulder blades glide over the ribs: they follow elevation fully but only part of the depth change.
            _scapula[i] = Matrix3D.Identity;
            _scapula[i].Rotate(new Quaternion(new Vector3D(0, 0, 1), s * -8));
            _scapula[i].Translate(new Vector3D(s * (.37 - scapulaIn), -.33 + shoulder.Y, BackZ(s * (.37 - scapulaIn), -.33 + shoulder.Y) + .015 + shoulder.Z * .2));
            _scapulaTransform[i].Matrix = _scapula[i];
            _trapSlope[i].Geometry = Mesh.Tube(Mesh.Through(new Point3D(s * .12, .12, -.05), new Point3D(s * .42, .09 + shoulder.Y * .5, -.04 + shoulder.Z * .5), _girdle[i].Transform(new Point3D(s * .72, .03, -.02)), 12), u => .12 + .04 * u, 16);
        }

        _neck.Geometry = Mesh.Tube(Mesh.Sample(NeckPoint, 14), u => NeckRadius - .015 * u, 20, caps: false);

        var pulse = .88 + .12 * Math.Sin(t * 2 * Math.PI / 1.4);
        foreach (var muscle in _muscles)
        {
            muscle.Model.Geometry = Mesh.Tube(muscle.Path(), u => muscle.Radius * (1 - muscle.Taper + muscle.Taper * Math.Sin(Math.PI * u)), 12);
            var k = Math.Clamp(muscle.Intensity, 0, 1);
            muscle.Diffuse.Color = Lerp(MuscleRest, MuscleHot, k);
            var glow = .6 * k * k * pulse; muscle.Glow.Color = MediaColor.FromRgb((byte)(255 * glow), (byte)(40 * glow), (byte)(24 * glow));
        }
    }

    /// <summary>Neutral → ease in → hold → ease out → rest, over six seconds.</summary>
    private static double HoldCycle(double t) => Keys(t % 6, (0, 0), (.8, 0), (2.0, 1), (4.0, 1), (5.2, 0), (6, 0));

    /// <summary>Centre → +1 → hold → centre → −1 → hold → centre, over ten seconds.</summary>
    private static double SwingCycle(double t) => Keys(t % 10, (0, 0), (.6, 0), (2.0, 1), (3.4, 1), (4.8, 0), (5.2, 0), (6.6, -1), (8.0, -1), (9.4, 0), (10, 0));

    private static double Keys(double t, params (double Time, double Value)[] keys)
    {
        for (var i = 0; i < keys.Length - 1; i++)
            if (t <= keys[i + 1].Time)
                return keys[i].Value + (keys[i + 1].Value - keys[i].Value) * Smooth((t - keys[i].Time) / (keys[i + 1].Time - keys[i].Time));
        return keys[^1].Value;
    }

    private static double Smooth(double x) { x = Math.Clamp(x, 0, 1); return x * x * x * (x * (6 * x - 15) + 10); }

    // ---------------------------------------------------------------- body geometry

    private static MeshGeometry3D Torso()
    {
        double top = TorsoProfile[0].Y, bottom = TorsoProfile[^1].Y;
        return Mesh.Loft(80, 40, TorsoExponent, s => { var y = top + (bottom - top) * s; var (w, d) = Profile(y); return (y, w, d); });
    }

    private static (double W, double D) Profile(double y)
    {
        var p = TorsoProfile; var i = 0;
        while (i < p.Length - 2 && y < p[i + 1].Y) i++;
        var t = Math.Clamp((p[i].Y - y) / (p[i].Y - p[i + 1].Y), 0, 1);
        double Spline(Func<(double Y, double W, double D), double> f)
        {
            double a = f(p[Math.Max(0, i - 1)]), b = f(p[i]), c = f(p[i + 1]), d = f(p[Math.Min(p.Length - 1, i + 2)]);
            return Math.Max(0, .5 * (2 * b + (c - a) * t + (2 * a - 5 * b + 4 * c - d) * t * t + (3 * b - a - 3 * c + d) * t * t * t));
        }
        return (Spline(x => x.W), Spline(x => x.D));
    }

    /// <summary>Z coordinate of the torso's back surface at (x, y).</summary>
    private static double BackZ(double x, double y)
    {
        var (w, d) = Profile(y); var f = Math.Min(1, Math.Abs(x) / Math.Max(w, 1e-3));
        return -d * Math.Pow(1 - Math.Pow(f, TorsoExponent), 1 / TorsoExponent);
    }

    private Vector3D HeadUp => _head.Transform(new Vector3D(0, 1, 0));
    private Vector3D HeadForward => _head.Transform(new Vector3D(0, 0, 1));

    private Point3D NeckPoint(double u) => Mesh.Bezier(NeckBase, NeckBase + new Vector3D(0, .24, 0), _headPivot - HeadUp * .2, _headPivot + HeadUp * .1, u);

    /// <summary>A point on the neck surface: <paramref name="front"/> and <paramref name="side"/> pick the direction around the neck.</summary>
    private Point3D NeckSurface(double u, double front, double side, double lift = .015)
    {
        var tangent = NeckPoint(Math.Min(1, u + .02)) - NeckPoint(Math.Max(0, u - .02)); tangent.Normalize();
        var forward = new Vector3D(0, 0, 1) * (1 - u) + HeadForward * u; forward -= tangent * Vector3D.DotProduct(forward, tangent); forward.Normalize();
        var lateral = Vector3D.CrossProduct(tangent, forward);
        var dir = forward * front + lateral * side; dir.Normalize();
        return NeckPoint(u) + dir * (NeckRadius - .015 * u + lift);
    }

    private Point3D OnHead(double x, double y, double z) => _head.Transform(new Point3D(x, y, z));

    /// <summary>Samples a path across the upper back and lays it just proud of the skin.</summary>
    private static List<Point3D> AcrossBack(Point3D from, Point3D to, int samples = 12) =>
        Mesh.Sample(u => { var x = from.X + (to.X - from.X) * u; var y = from.Y + (to.Y - from.Y) * u; return new Point3D(x, y, BackZ(x, y) - .03); }, samples);

    // ---------------------------------------------------------------- highlights and arrows

    private void BuildHighlights()
    {
        switch (_kind)
        {
            case ExerciseKind.ChinTuck:
                foreach (var s in Sides)
                {
                    var side = s;
                    AddMuscle("flexor", .045, () => Mesh.Sample(u => NeckSurface(.08 + .8 * u, .94, side * .34), 14));
                    AddMuscle("suboccipital", .05, () => Mesh.Through(OnHead(side * .1, .12, -.35), NeckSurface(.86, -.85, side * .5, .05), NeckSurface(.68, -.9, side * .45), 10));
                }
                break;
            case ExerciseKind.LookLeftRight:
                foreach (var s in Sides) { var side = s; AddMuscle($"scm{s}", .06, () => Mesh.Through(OnHead(side * .3, .16, -.1), NeckSurface(.5, .45, side * .9, .04), new Point3D(side * .08, .2, Profile(.2).D + .02))); }
                break;
            case ExerciseKind.SideTilt:
            case ExerciseKind.ShoulderRolls:
                foreach (var s in Sides)
                {
                    var side = s; var i = Index(s);
                    AddMuscle($"trap{s}", .068, () => Mesh.Through(OnHead(side * .1, .14, -.33), NeckSurface(.45, -.55, side * .83, .015), _girdle[i].Transform(new Point3D(side * .66, .17, -.08)), 16));
                    if (_kind == ExerciseKind.SideTilt)
                        AddMuscle($"scalene{s}", .05, () => Mesh.Through(NeckSurface(.82, .1, side, .01), NeckSurface(.45, .25, side * .97, .03), new Point3D(side * .3, .2, .06), 12));
                    else
                        AddMuscle($"levator{s}", .05, () => Mesh.Through(NeckSurface(.85, -.2, side, .01), NeckSurface(.45, -.25, side * .97, .06), _scapula[i].Transform(new Point3D(-side * .1, .24, .02)), 12));
                }
                break;
            case ExerciseKind.ShoulderBladeSqueeze:
                foreach (var s in Sides)
                {
                    var side = s; var i = Index(s);
                    // Middle trapezius runs level from the spine to the top of the blade; rhomboids run down and out to its inner edge.
                    AddMuscle($"rhomboid{s}", .042, () => AcrossBack(new Point3D(side * .02, -.17, 0), _scapula[i].Transform(new Point3D(side * .02, .17, 0))), .3);
                    AddMuscle($"rhomboid{s}", .045, () => AcrossBack(new Point3D(side * .02, -.25, 0), _scapula[i].Transform(new Point3D(-side * .07, .02, 0))), .3);
                    AddMuscle($"rhomboid{s}", .045, () => AcrossBack(new Point3D(side * .02, -.38, 0), _scapula[i].Transform(new Point3D(-side * .05, -.15, 0))), .3);
                }
                break;
        }
    }

    private void AddMuscle(string name, double radius, Func<List<Point3D>> path, double taper = .65)
    {
        var diffuse = new SolidColorBrush(MuscleRest); var glow = new SolidColorBrush(Colors.Black);
        var material = new MaterialGroup { Children = { new DiffuseMaterial(diffuse), new EmissiveMaterial(glow), new SpecularMaterial(new SolidColorBrush(MediaColor.FromArgb(90, 255, 235, 230)), 30) } };
        _muscles.Add(new Muscle(name, radius, taper, path, new GeometryModel3D { Material = material }, diffuse, glow));
    }

    private void SetIntensity(string name, double value) { foreach (var m in _muscles) if (m.Name == name) m.Intensity = value; }

    private void BuildArrows()
    {
        switch (_kind)
        {
            case ExerciseKind.ChinTuck:
                AddArrow("back", new[] { new Point3D(0, .76, 1.02), new Point3D(0, .76, .66) }); break;
            case ExerciseKind.LookLeftRight:
                foreach (var s in Sides) AddArrow($"turn{s}", Mesh.Sample(u => { var a = s * (0 + 95 * u) * Math.PI / 180; return HeadPivotRest + new Vector3D(.62 * Math.Sin(a), .98, .62 * Math.Cos(a)); }, 18));
                break;
            case ExerciseKind.SideTilt:
                foreach (var s in Sides) AddArrow($"tilt{s}", Mesh.Sample(u => { var a = s * (8 + 34 * u) * Math.PI / 180; return HeadPivotRest + new Vector3D(-.98 * Math.Sin(a), .98 * Math.Cos(a), .1); }, 18));
                break;
            case ExerciseKind.ShoulderRolls:
                AddArrow("roll", Mesh.Sample(u => { var a = (25 + 290 * u) * Math.PI / 180; return new Point3D(1.22, .02 + .34 * Math.Sin(a), .34 * Math.Cos(a)); }, 30)); break;
            case ExerciseKind.ShoulderBladeSqueeze:
                foreach (var s in Sides) AddArrow("squeeze", new[] { new Point3D(s * 1.0, -.25, -.62), new Point3D(s * .58, -.3, -.62) });
                break;
        }
    }

    private void AddArrow(string name, IReadOnlyList<Point3D> path)
    {
        var brush = new SolidColorBrush(Accent); var glow = new SolidColorBrush(MediaColor.FromRgb(6, 70, 82));
        var material = new MaterialGroup { Children = { new DiffuseMaterial(brush), new EmissiveMaterial(glow) } };
        var end = path[^1]; var dir = end - path[^2]; dir.Normalize();
        var shaft = path.Take(path.Count - 1).Append(end - dir * .14).ToList();
        var group = new Model3DGroup();
        group.Children.Add(new GeometryModel3D(Mesh.Tube(shaft, _ => .028, 10), material) { BackMaterial = material });
        group.Children.Add(new GeometryModel3D(Mesh.Tube(new[] { end - dir * .17, end + dir * .05 }, u => .08 * (1 - u), 16), material) { BackMaterial = material });
        _arrows.Add(new Arrow(name, group, brush, glow));
    }

    private void SetArrow(string name, double opacity)
    {
        foreach (var a in _arrows) if (a.Name == name) { a.Brush.Opacity = .9 * Math.Clamp(opacity, 0, 1); a.Glow.Opacity = a.Brush.Opacity; }
    }

    private static Material Matte(MediaColor color)
    {
        var material = new MaterialGroup { Children = { new DiffuseMaterial(new SolidColorBrush(color)), new SpecularMaterial(new SolidColorBrush(MediaColor.FromArgb(60, 255, 255, 255)), 22) } };
        material.Freeze(); return material;
    }

    private static MediaColor Lerp(MediaColor a, MediaColor b, double t) =>
        MediaColor.FromRgb((byte)(a.R + (b.R - a.R) * t), (byte)(a.G + (b.G - a.G) * t), (byte)(a.B + (b.B - a.B) * t));

    private sealed class Muscle(string name, double radius, double taper, Func<List<Point3D>> path, GeometryModel3D model, SolidColorBrush diffuse, SolidColorBrush glow)
    {
        public string Name { get; } = name;
        public double Radius { get; } = radius;
        public double Taper { get; } = taper;
        public Func<List<Point3D>> Path { get; } = path;
        public GeometryModel3D Model { get; } = model;
        public SolidColorBrush Diffuse { get; } = diffuse;
        public SolidColorBrush Glow { get; } = glow;
        public double Intensity { get; set; }
    }

    private sealed record Arrow(string Name, Model3D Model, SolidColorBrush Brush, SolidColorBrush Glow);
}
