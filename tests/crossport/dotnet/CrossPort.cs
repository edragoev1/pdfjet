/**
 * CrossPort.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Globalization;
using System.IO;
using System.Text;

namespace PDFjet.NET {
/// <summary>
/// The C# port's side of the cross-port check,
/// tests/crossport/check-crossport.sh: the shapes of tests/crossport/go, drawn
/// with the same random numbers, and the content of each page written.
///
///     dotnet CrossPort.dll SEED CASES OUT
/// </summary>
public class CrossPort {
    // SplitMix64, which every port computes the same.
    private ulong state;

    private CrossPort(ulong seed) {
        this.state = seed;
    }

    private ulong Next() {
        unchecked {
            state += 0x9E3779B97F4A7C15UL;
            ulong z = state;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    // A number from lo up to hi, of 24 bits, which a float holds exactly.
    private float Between(float lo, float hi) {
        float unit = (float) (Next() >> 40) / 16777216f;
        return lo + unit * (hi - lo);
    }

    // A whole number from 0 up to n.
    private int Below(int n) {
        return (int) (Next() % (ulong) n);
    }

    private int Color() {
        return Below(0x1000000);
    }

    public static void Main(string[] args) {
        ulong seed = ulong.Parse(args[0], CultureInfo.InvariantCulture);
        int cases = int.Parse(args[1], CultureInfo.InvariantCulture);
        using (Stream output = new BufferedStream(new FileStream(args[2], FileMode.Create))) {
            for (int i = 0; i < cases; i++) {
                byte[] header = Encoding.ASCII.GetBytes("case " + i.ToString(CultureInfo.InvariantCulture) + "\n");
                output.Write(header, 0, header.Length);
                byte[] content = new CrossPort(seed + (ulong) i).DrawCase();
                output.Write(content, 0, content.Length);
            }
        }
    }

    // Gives the arc a stroke width, a dash pattern or none, a fill color or
    // none, and a rotation.
    private void StyleArc(Arc arc) {
        arc.SetStrokeWidth(Between(0f, 10f));
        if (Below(2) == 0) {
            int on = 1 + Below(9);
            int off = 1 + Below(9);
            arc.SetStrokeDashPattern("[" + on.ToString(CultureInfo.InvariantCulture) + " "
                    + off.ToString(CultureInfo.InvariantCulture) + "] 0");
        }
        if (Below(2) == 0) {
            arc.SetFillColor(Color());
        }
        arc.SetRotation(Between(-360f, 360f));
    }

    // Draws the shapes of one case on a page of a document of its own, and
    // returns the content of the page, or "error" when the document fails.
    private byte[] DrawCase() {
        try {
            PDF pdf = new PDF(new MemoryStream());
            Font font = new Font(pdf, CoreFont.HELVETICA);
            Page page = new Page(pdf, Letter.PORTRAIT);
            int shapes = 1 + Below(4);
            for (int k = 0; k < shapes; k++) {
                switch (Below(5)) {
                case 0: {
                    Arc arc = new Arc();
                    arc.SetLocation(Between(0f, 612f), Between(0f, 792f));
                    arc.SetRadiusX(Between(0f, 300f));
                    arc.SetRadiusY(Between(0f, 300f));
                    arc.SetStartAngle(Between(-720f, 720f));
                    arc.SetSweep(Between(-720f, 720f));
                    StyleArc(arc);
                    arc.DrawOn(page);
                    break;
                }
                case 1: {
                    // The whole of an ellipse, which a random sweep never is
                    Ellipse ellipse = new Ellipse();
                    ellipse.SetLocation(Between(0f, 612f), Between(0f, 792f));
                    ellipse.SetRadiusX(Between(0f, 300f));
                    ellipse.SetRadiusY(Between(0f, 300f));
                    StyleArc(ellipse);
                    ellipse.DrawOn(page);
                    break;
                }
                case 2: {
                    DonutChart chart = new DonutChart(font, font);
                    chart.SetLocation(Between(0f, 612f), Between(0f, 792f));
                    float outer = Between(1f, 200f);
                    chart.SetRadii(outer, Between(0f, outer));
                    int slices = 1 + Below(6);
                    for (int s = 0; s < slices; s++) {
                        chart.AddSlice(new Slice(Between(0f, 100f), Color(), "s" + s.ToString(CultureInfo.InvariantCulture)));
                    }
                    chart.DrawOn(page);
                    break;
                }
                case 3: {
                    Path path = new Path();
                    int points = 2 + Below(6);
                    for (int p = 0; p < points; p++) {
                        float x = Between(0f, 612f);
                        float y = Between(0f, 792f);
                        if (p < points - 1 && Below(3) == 0) {
                            path.Add(new Point(x, y, Point.CONTROL_POINT_C));
                        } else {
                            path.Add(new Point(x, y));
                        }
                    }
                    path.SetClosed(Below(2) == 0);
                    path.SetFillShape(Below(2) == 0);
                    path.SetStrokeWidth(Between(0f, 5f));
                    path.DrawOn(page);
                    break;
                }
                default: {
                    TextLine line = new TextLine(font, "Text " + k.ToString(CultureInfo.InvariantCulture));
                    line.SetFontSize(Between(4f, 40f));
                    line.SetLocation(Between(0f, 612f), Between(0f, 792f));
                    line.SetTextRotation(Below(361) - 180);
                    line.DrawOn(page);
                    break;
                }
                }
            }
            byte[] content = page.GetContent();
            pdf.Complete();
            byte[] withNewline = new byte[content.Length + 1];
            Array.Copy(content, withNewline, content.Length);
            withNewline[content.Length] = (byte) '\n';
            return withNewline;
        } catch (Exception) {
            return Encoding.ASCII.GetBytes("error\n");
        }
    }
}
}   // End of namespace PDFjet.NET
