using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_23.cs
 */
public class Example_23 {
    public Example_23() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_23.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(72f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.SetSize(24f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float x1 = 90f;
        float y1 = 50f;

        TextLine textLine = new TextLine(f2, "(x1, y1)");
        textLine.SetLocation(x1, y1 - 15f);
        textLine.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
            "Hello, World! This example shows the functionality of the TextBlock.");
        textBlock.SetLocation(x1, y1);
        textBlock.SetWidth(500f);
        textBlock.SetBorderColor(Color.lightgreen);
        textBlock.SetFillColor(Color.lightgreen);
        textBlock.SetTextColor(Color.black);
        float[] xy = textBlock.DrawOn(page);

        // Text on the left
        TextLine ascentText = new TextLine(f2, "Ascent");
        ascentText.SetFontSize(18f);
        ascentText.SetLocation(x1 - 85f, y1 + 40f);
        ascentText.DrawOn(page);

        TextLine descentText = new TextLine(f2, "Descent");
        descentText.SetFontSize(18f);
        descentText.SetLocation(x1 - 85f, y1 + f1.GetAscent() + 15f);
        descentText.DrawOn(page);

        // Line beside the text ascent
        Line blueLine = new Line(
            x1 - 10f,
            y1,
            x1 - 10f,
            y1 + f1.GetAscent());
        blueLine.SetStrokeColor(Color.blue);
        blueLine.SetStrokeWidth(3f);
        blueLine.DrawOn(page);

        // Line beside the text descent
        Line redLine = new Line(
            x1 - 10f,
            y1 + f1.GetAscent(),
            x1 - 10f,
            y1 + f1.GetAscent() + f1.GetDescent());
        redLine.SetStrokeColor(Color.red);
        redLine.SetStrokeWidth(3f);
        redLine.DrawOn(page);

        Line baseLine = new Line(
                x1,
                y1 + f1.GetAscent(),
                xy[0],
                y1 + f1.GetAscent());
        baseLine.DrawOn(page);

        Line descentLine = new Line(
                x1,
                y1 + (f1.GetAscent() + f1.GetDescent()),
                xy[0],
                y1 + (f1.GetAscent() + f1.GetDescent()));
        descentLine.DrawOn(page);

        Line ascentLine = new Line(
                x1,
                y1 + f1.GetBodyHeight() + f1.GetAscent(),
                xy[0],
                y1 + f1.GetBodyHeight() + f1.GetAscent());
        ascentLine.DrawOn(page);

        Point p1 = new Point(x1, y1);
        p1.SetRadius(5f);
        p1.DrawOn(page);

        Point p2 = new Point(xy[0], xy[1]);
        p2.SetRadius(5f);
        p2.DrawOn(page);

        f2.SetSize(24f);
        TextLine textLine3 = new TextLine(f2, "(x2, y2)");
        textLine3.SetLocation(xy[0] - 80f, xy[1] + 30f);
        textLine3.DrawOn(page);

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.SetBorderColor(Color.black);
        rect.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_23();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_23", time0, time1);
    }
}   // End of Example_23.cs
