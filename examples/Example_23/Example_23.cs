using System;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Reflection;
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

        TextLine ascentText = new TextLine(f2, "Ascent");
        ascentText.SetFontSize(18f);
        ascentText.SetLocation(x1 - 85f, y1 + 40f);
        ascentText.DrawOn(page);

        TextLine descentText = new TextLine(f2, "Descent");
        descentText.SetFontSize(18f);
        descentText.SetLocation(x1 - 85f, y1 + f1.GetAscent(f1.GetSize()) + 15f);
        descentText.DrawOn(page);

        Line blueLine = new Line(
            x1 - 10f,
            y1,
            x1 - 10f,
            y1 + f1.GetAscent());
        blueLine.SetColor(Color.blue);
        blueLine.SetWidth(3f);
        blueLine.DrawOn(page);

        Line redLine = new Line(
                x1 - 10f,
                y1 + f1.GetAscent(f1.GetSize()),
                x1 - 10f,
                y1 + f1.GetBodyHeight(f1.GetSize()));
        redLine.SetColor(Color.red);
        redLine.SetWidth(3f);
        redLine.DrawOn(page);

        Line baseLine = new Line(
                x1,
                y1 + f1.GetAscent(f1.GetSize()),
                xy[0],
                y1 + f1.GetAscent(f1.GetSize()));
        baseLine.DrawOn(page);

        Line descentLine1 = new Line(
                x1,
                y1 + f1.GetBodyHeight(f1.GetSize()),
                xy[0],
                y1 + f1.GetBodyHeight(f1.GetSize()));
        descentLine1.DrawOn(page);

        Line ascentLine = new Line(
                x1,
                y1 + f1.GetBodyHeight(f1.GetSize()) + f1.GetAscent(f1.GetSize()),
                xy[0],
                y1 + f1.GetBodyHeight(f1.GetSize()) + f1.GetAscent(f1.GetSize()));
        ascentLine.DrawOn(page);

        Point p1 = new Point(x1, y1);
        p1.SetRadius(5f);
        p1.DrawOn(page);

        Point p2 = new Point(xy[0], xy[1]);
        p2.SetRadius(5f);
        p2.DrawOn(page);

        TextLine textLine3 = new TextLine(f2, "(x2, y2)");
        textLine3.SetFontSize(24f);
        textLine3.SetLocation(xy[0] - 80f, xy[1] + 30f);
        textLine3.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

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
