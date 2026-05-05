using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NativeElements.Models;

namespace NativeElements.Services
{
    public static class DxfExportService
    {
        // Simple ASCII DXF exporter (R12-compatible minimal). Units: mm.
        private static string Header() =>
@"0
SECTION
2
HEADER
0
ENDSEC
0
SECTION
2
TABLES
0
ENDSEC
0
SECTION
2
BLOCKS
0
ENDSEC
0
SECTION
2
ENTITIES";

        private static string Footer() =>
@"0
ENDSEC
0
SECTION
2
OBJECTS
0
ENDSEC
0
EOF";

        private static string BeginLayer(string name, int color = 7)
        {
            return $"0\nLAYER\n2\n{name}\n70\n0\n62\n{color}\n6\nCONTINUOUS\n";
        }

        public static async Task<string> ExportPetalToDxfAsync(PetalOutput output, string fileName)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(Header());

                // Layers
                sb.AppendLine(BeginLayer("CUT", 1));
                sb.AppendLine(BeginLayer("SEAM", 3));
                sb.AppendLine(BeginLayer("ANNOTATION", 7));

                // Build full petal outline (both sides) from CurvePoints
                if (output?.CurvePoints != null && output.CurvePoints.Count > 0)
                {
                    var outlinePoints = new List<(double X, double Y)>();

                    // Add right side (forward)
                    foreach (var p in output.CurvePoints)
                    {
                        outlinePoints.Add((p.X * 10.0, p.Y * 10.0)); // cm -> mm
                    }

                    // Add left side (backward, mirrored X)
                    for (int i = output.CurvePoints.Count - 1; i >= 0; i--)
                    {
                        var p = output.CurvePoints[i];
                        outlinePoints.Add((-p.X * 10.0, p.Y * 10.0)); // Mirror X, cm -> mm
                    }

                    // LWPOLYLINE for petal outline
                    sb.AppendLine("0");
                    sb.AppendLine("LWPOLYLINE");
                    sb.AppendLine("90\n" + outlinePoints.Count);
                    sb.AppendLine("70\n1"); // closed polyline
                    sb.AppendLine("8\nCUT");

                    foreach (var p in outlinePoints)
                    {
                        sb.AppendLine("10\n" + p.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sb.AppendLine("20\n" + p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }
                }

                // Add annotation: width/height
                sb.AppendLine("0");
                sb.AppendLine("TEXT");
                sb.AppendLine("8\nANNOTATION");
                sb.AppendLine("10\n0.0");
                sb.AppendLine("20\n0.0");
                sb.AppendLine("40\n5.0");
                sb.AppendLine("1\nPetal template (units: mm)");

                sb.AppendLine(Footer());

                var docs = FileSystem.AppDataDirectory;
                var path = Path.Combine(docs, fileName + ".dxf");
                File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
                return path;
            });
        }

        public static async Task<string> ExportSegmentedRingToDxfAsync(SegmentedRingOutput output, string fileName)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(Header());
                sb.AppendLine(BeginLayer("CUT", 1));
                sb.AppendLine(BeginLayer("ANNOTATION", 7));

                double R_o = output.OuterRadius * 10.0;  // cm → mm
                double R_i = output.InnerRadius  * 10.0;
                double halfAngleDeg = output.MiterAngle;
                double halfAngleRad = halfAngleDeg * Math.PI / 180.0;
                double sinHalf = Math.Sin(halfAngleRad);
                double cosHalf = Math.Cos(halfAngleRad);

                // DXF coordinate system: Y up, ring centre at origin.
                // Segment points upward (outer arc at top, inner arc at bottom).
                //
                // Key points (all in mm):
                //   Outer-left:  (-R_o*sinHalf,  R_o*cosHalf)
                //   Outer-right: ( R_o*sinHalf,  R_o*cosHalf)
                //   Inner-left:  (-R_i*sinHalf,  R_i*cosHalf)
                //   Inner-right: ( R_i*sinHalf,  R_i*cosHalf)
                //
                // Arcs are drawn CCW (positive direction in DXF).
                // Standard DXF angles: CCW from +X axis.
                // Outer-right at angle = 90° - halfAngleDeg from +X.
                // Outer-left  at angle = 90° + halfAngleDeg from +X.

                double arcStartDeg = 90.0 - halfAngleDeg;
                double arcEndDeg   = 90.0 + halfAngleDeg;

                // Outer arc
                DxfArc(sb, 0, 0, R_o, arcStartDeg, arcEndDeg, "CUT");

                // Inner arc
                DxfArc(sb, 0, 0, R_i, arcStartDeg, arcEndDeg, "CUT");

                // Right straight side (outer-right → inner-right)
                DxfLine(sb,
                     R_o * sinHalf,  R_o * cosHalf,
                     R_i * sinHalf,  R_i * cosHalf, "CUT");

                // Left straight side (inner-left → outer-left)
                DxfLine(sb,
                    -R_i * sinHalf,  R_i * cosHalf,
                    -R_o * sinHalf,  R_o * cosHalf, "CUT");

                // Annotations
                int n = (int)Math.Round(360.0 / output.SegmentAngle);

                // Lo above outer arc
                DxfText(sb, 0, R_o + 8,
                    $"Lo = {output.OuterEdgeLength:F2} cm", 5.0, "ANNOTATION");

                // Li below inner arc
                DxfText(sb, 0, R_i * cosHalf - 10,
                    $"Li = {output.InnerEdgeLength:F2} cm", 5.0, "ANNOTATION");

                // W on right side (mid-radial)
                DxfText(sb, R_o * sinHalf + 4, (R_o * cosHalf + R_i * cosHalf) / 2,
                    $"W = {output.RadialEdgeLength:F2} cm", 5.0, "ANNOTATION");

                // θ angle at upper-left corner
                DxfText(sb, -R_o * sinHalf - 4, R_o * cosHalf,
                    $"θ = {halfAngleDeg:F1}°", 5.0, "ANNOTATION");

                // Title
                DxfText(sb, 0, R_o + 18,
                    $"Ring Segment · {n} pieces · Scale 1:1", 6.0, "ANNOTATION");

                sb.AppendLine(Footer());

                var path = Path.Combine(FileSystem.AppDataDirectory, fileName + ".dxf");
                File.WriteAllText(path, sb.ToString(), System.Text.Encoding.ASCII);
                return path;
            });
        }

        private static void DxfArc(StringBuilder sb, double cx, double cy, double radius,
                                    double startAngle, double endAngle, string layer)
        {
            sb.AppendLine("0\nARC");
            sb.AppendLine($"8\n{layer}");
            sb.AppendLine($"10\n{Fmt(cx)}");
            sb.AppendLine($"20\n{Fmt(cy)}");
            sb.AppendLine("30\n0.0");
            sb.AppendLine($"40\n{Fmt(radius)}");
            sb.AppendLine($"50\n{Fmt(startAngle)}");
            sb.AppendLine($"51\n{Fmt(endAngle)}");
        }

        private static void DxfLine(StringBuilder sb, double x1, double y1, double x2, double y2, string layer)
        {
            sb.AppendLine("0\nLINE");
            sb.AppendLine($"8\n{layer}");
            sb.AppendLine($"10\n{Fmt(x1)}");
            sb.AppendLine($"20\n{Fmt(y1)}");
            sb.AppendLine("30\n0.0");
            sb.AppendLine($"11\n{Fmt(x2)}");
            sb.AppendLine($"21\n{Fmt(y2)}");
            sb.AppendLine("31\n0.0");
        }

        private static void DxfText(StringBuilder sb, double x, double y, string text, double height, string layer)
        {
            sb.AppendLine("0\nTEXT");
            sb.AppendLine($"8\n{layer}");
            sb.AppendLine($"10\n{Fmt(x)}");
            sb.AppendLine($"20\n{Fmt(y)}");
            sb.AppendLine("30\n0.0");
            sb.AppendLine($"40\n{Fmt(height)}");
            sb.AppendLine($"1\n{text}");
        }

        private static string Fmt(double v) => v.ToString("F4", System.Globalization.CultureInfo.InvariantCulture);

        public static async Task<string> ExportCushionToDxfAsync(CushionOutput output, string fileName)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(Header());
                sb.AppendLine(BeginLayer("CUT", 1));
                sb.AppendLine(BeginLayer("SEAM", 3));
                sb.AppendLine(BeginLayer("ANNOTATION", 7));

                // Draw rectangle for outer fabric layout (using layout W/H)
                double wmm = (output.LayoutWidth > 0 ? output.LayoutWidth : output.Input.FinishedWidth) * 10.0;
                double hmm = (output.LayoutHeight > 0 ? output.LayoutHeight : output.Input.FinishedDepth) * 10.0;

                // Rectangle polyline
                var pts = new List<(double X, double Y)>
                {
                    (0,0), (wmm,0), (wmm,hmm), (0,hmm)
                };

                sb.AppendLine("0");
                sb.AppendLine("LWPOLYLINE");
                sb.AppendLine("90\n" + pts.Count);
                sb.AppendLine("70\n1");
                sb.AppendLine("8\nCUT");
                foreach (var p in pts)
                {
                    sb.AppendLine("10\n" + p.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendLine("20\n" + p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                // Add seam allowance rectangle inset
                double seam = output.Input.SeamAllowance * 10.0;
                var pts2 = new List<(double X, double Y)>
                {
                    (seam,seam), (wmm-seam,seam), (wmm-seam,hmm-seam), (seam,hmm-seam)
                };
                sb.AppendLine("0");
                sb.AppendLine("LWPOLYLINE");
                sb.AppendLine("90\n" + pts2.Count);
                sb.AppendLine("70\n1");
                sb.AppendLine("8\nSEAM");
                foreach (var p in pts2)
                {
                    sb.AppendLine("10\n" + p.X.ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendLine("20\n" + p.Y.ToString(System.Globalization.CultureInfo.InvariantCulture));
                }

                sb.AppendLine(Footer());
                var docs = FileSystem.AppDataDirectory;
                var path = Path.Combine(docs, fileName + ".dxf");
                File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
                return path;
            });
        }

        public static async Task<string> ExportCuttingLayoutToDxfAsync(NativeElements.Core.Models.CuttingLayoutOutput layout, string fileName)
        {
            return await Task.Run(() =>
            {
                var sb = new StringBuilder();
                sb.AppendLine(Header());
                sb.AppendLine(BeginLayer("CUT", 1));
                sb.AppendLine(BeginLayer("ANNOTATION", 7));

                int textId = 0;
                foreach (var piece in layout.Pieces)
                {
                    // For each piece, write LWPOLYLINE in mm (convert cm->mm)
                    var pts = piece.Points ?? new List<(double X, double Y)>();
                    if (pts.Count == 0)
                    {
                        // fallback rectangle if width/height provided
                        double wmm = piece.WidthCm * 10.0;
                        double hmm = piece.HeightCm * 10.0;
                        pts = new List<(double X, double Y)> { (0,0), (wmm,0), (wmm,hmm), (0,hmm) };
                    }

                    sb.AppendLine("0");
                    sb.AppendLine("LWPOLYLINE");
                    sb.AppendLine("90\n" + pts.Count);
                    sb.AppendLine("70\n1");
                    sb.AppendLine("8\nCUT");
                    foreach (var p in pts)
                    {
                        sb.AppendLine("10\n" + (p.X * 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                        sb.AppendLine("20\n" + (p.Y * 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    }

                    // Annotation: TEXT with piece name and quantity
                    sb.AppendLine("0");
                    sb.AppendLine("TEXT");
                    sb.AppendLine("8\nANNOTATION");
                    sb.AppendLine("10\n" + ((pts[0].X + pts[1].X) / 2.0 * 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendLine("20\n" + ((pts[0].Y + pts[2].Y) / 2.0 * 10.0).ToString(System.Globalization.CultureInfo.InvariantCulture));
                    sb.AppendLine("40\n5.0");
                    sb.AppendLine("1\n" + System.Security.SecurityElement.Escape($"{piece.Name} x{piece.Quantity}"));

                    textId++;
                }

                sb.AppendLine(Footer());
                var docs = FileSystem.AppDataDirectory;
                var path = Path.Combine(docs, fileName + ".dxf");
                File.WriteAllText(path, sb.ToString(), Encoding.ASCII);
                return path;
            });
        }
    }
}
