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

                // Approximate segment polygon using chord lengths and segment angle.
                // SegmentedRingOutput does not expose radii, so derive them:
                // chord = 2 * R * sin(theta/2) => R = chord / (2 * sin(theta/2))
                double angleDeg = output.SegmentAngle;
                double angleRad = angleDeg * Math.PI / 180.0;
                double sinHalf = Math.Sin(angleRad / 2.0);
                if (Math.Abs(sinHalf) < 1e-9)
                    throw new ArgumentException("Invalid segmented ring angle.");
                double outerR = (output.OuterEdgeLength / (2.0 * sinHalf)) * 10.0; // cm -> mm
                double innerR = (output.InnerEdgeLength / (2.0 * sinHalf)) * 10.0; // cm -> mm

                // Build polygon points from inner to outer edges
                var points = new List<(double X, double Y)>();
                int steps = 36; // approximation
                for (int i = 0; i <= steps; i++)
                {
                    double t = i / (double)steps;
                    double a = -angleRad / 2.0 + t * angleRad;
                    double x = outerR * Math.Cos(a);
                    double y = outerR * Math.Sin(a);
                    points.Add((x, y));
                }
                for (int i = 0; i <= steps; i++)
                {
                    double t = i / (double)steps;
                    double a = angleRad / 2.0 - t * angleRad;
                    double x = innerR * Math.Cos(a);
                    double y = innerR * Math.Sin(a);
                    points.Add((x, y));
                }

                // LWPOLYLINE
                sb.AppendLine("0");
                sb.AppendLine("LWPOLYLINE");
                sb.AppendLine("90\n" + points.Count);
                sb.AppendLine("70\n1");
                sb.AppendLine("8\nCUT");
                foreach (var p in points)
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
