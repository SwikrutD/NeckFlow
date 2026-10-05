using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace NeckFlow.Rendering;

/// <summary>Procedural mesh builders. All surfaces share vertices around their seams so WPF generates smooth normals.</summary>
internal static class Mesh
{
    private const int CapSteps = 4;

    /// <summary>Row-major grid whose columns wrap around. Rows must run from the shape's "start" to its "end" so faces point outward.</summary>
    public static MeshGeometry3D Surface(int rows, int cols, Func<int, int, Point3D> point)
    {
        var positions = new Point3DCollection(rows * cols);
        for (var r = 0; r < rows; r++) for (var c = 0; c < cols; c++) positions.Add(point(r, c));
        var indices = new Int32Collection((rows - 1) * cols * 6);
        for (var r = 0; r < rows - 1; r++)
            for (var c = 0; c < cols; c++)
            {
                var a = r * cols + c; var b = r * cols + (c + 1) % cols; var d = a + cols; var e = b + cols;
                indices.Add(a); indices.Add(d); indices.Add(b);
                indices.Add(b); indices.Add(d); indices.Add(e);
            }
        var mesh = new MeshGeometry3D { Positions = positions, TriangleIndices = indices };
        return mesh;
    }

    public static MeshGeometry3D Ellipsoid(Point3D center, Vector3D radii, int rows = 20, int cols = 28) =>
        Surface(rows, cols, (r, c) =>
        {
            var phi = Math.PI * r / (rows - 1); var theta = 2 * Math.PI * c / cols;
            return center + new Vector3D(radii.X * Math.Sin(phi) * Math.Sin(theta), radii.Y * Math.Cos(phi), radii.Z * Math.Sin(phi) * Math.Cos(theta));
        });

    /// <summary>Superellipse cross-sections stacked top to bottom; <paramref name="section"/> returns (y, halfWidth, halfDepth).</summary>
    public static MeshGeometry3D Loft(int rows, int cols, double exponent, Func<double, (double Y, double W, double D)> section) =>
        Surface(rows, cols, (r, c) =>
        {
            var (y, w, d) = section(r / (double)(rows - 1)); var theta = 2 * Math.PI * c / cols;
            return new Point3D(w * SuperCurve(Math.Sin(theta), exponent), y, d * SuperCurve(Math.Cos(theta), exponent));
        });

    public static double SuperCurve(double v, double exponent) => Math.Sign(v) * Math.Pow(Math.Abs(v), 2 / exponent);

    /// <summary>A tube swept along a path with rounded end caps. <paramref name="radius"/> receives 0..1 along the path.</summary>
    public static MeshGeometry3D Tube(IReadOnlyList<Point3D> path, Func<double, double> radius, int sides = 14, bool caps = true)
    {
        var n = path.Count; var tangents = new Vector3D[n];
        for (var i = 0; i < n; i++) { var t = path[Math.Min(n - 1, i + 1)] - path[Math.Max(0, i - 1)]; t.Normalize(); tangents[i] = t; }
        var rings = new List<(Point3D Center, Vector3D Tangent, double Radius)>(n + CapSteps * 2);
        double r0 = radius(0), r1 = radius(1);
        if (caps) for (var k = CapSteps; k >= 1; k--) { var a = k * Math.PI / 2 / CapSteps; rings.Add((path[0] - tangents[0] * (r0 * Math.Sin(a)), tangents[0], r0 * Math.Cos(a))); }
        for (var i = 0; i < n; i++) rings.Add((path[i], tangents[i], radius(i / (double)(n - 1))));
        if (caps) for (var k = 1; k <= CapSteps; k++) { var a = k * Math.PI / 2 / CapSteps; rings.Add((path[n - 1] + tangents[n - 1] * (r1 * Math.Sin(a)), tangents[n - 1], r1 * Math.Cos(a))); }

        // Parallel-transport a normal along the path so the tube never twists.
        var normals = new Vector3D[rings.Count]; var binormals = new Vector3D[rings.Count];
        var first = rings[0].Tangent; var reference = Math.Abs(first.Y) < .9 ? new Vector3D(0, 1, 0) : new Vector3D(1, 0, 0);
        var normal = reference - first * Vector3D.DotProduct(reference, first); normal.Normalize();
        for (var i = 0; i < rings.Count; i++)
        {
            var t = rings[i].Tangent; normal -= t * Vector3D.DotProduct(normal, t); normal.Normalize();
            normals[i] = normal; binormals[i] = Vector3D.CrossProduct(normal, t);
        }
        return Surface(rings.Count, sides, (r, c) =>
        {
            var theta = 2 * Math.PI * c / sides;
            return rings[r].Center + (normals[r] * Math.Cos(theta) + binormals[r] * Math.Sin(theta)) * rings[r].Radius;
        });
    }

    public static Point3D Bezier(Point3D a, Point3D b, Point3D c, Point3D d, double t)
    {
        var u = 1 - t;
        return new Point3D(
            u * u * u * a.X + 3 * u * u * t * b.X + 3 * u * t * t * c.X + t * t * t * d.X,
            u * u * u * a.Y + 3 * u * u * t * b.Y + 3 * u * t * t * c.Y + t * t * t * d.Y,
            u * u * u * a.Z + 3 * u * u * t * b.Z + 3 * u * t * t * c.Z + t * t * t * d.Z);
    }

    /// <summary>Quadratic curve that passes through <paramref name="mid"/> at its halfway point.</summary>
    public static List<Point3D> Through(Point3D start, Point3D mid, Point3D end, int samples = 14)
    {
        var control = new Point3D(2 * mid.X - (start.X + end.X) / 2, 2 * mid.Y - (start.Y + end.Y) / 2, 2 * mid.Z - (start.Z + end.Z) / 2);
        var points = new List<Point3D>(samples);
        for (var i = 0; i < samples; i++)
        {
            var t = i / (double)(samples - 1); var u = 1 - t;
            points.Add(new Point3D(u * u * start.X + 2 * u * t * control.X + t * t * end.X, u * u * start.Y + 2 * u * t * control.Y + t * t * end.Y, u * u * start.Z + 2 * u * t * control.Z + t * t * end.Z));
        }
        return points;
    }

    public static List<Point3D> Sample(Func<double, Point3D> curve, int samples)
    {
        var points = new List<Point3D>(samples);
        for (var i = 0; i < samples; i++) points.Add(curve(i / (double)(samples - 1)));
        return points;
    }
}
