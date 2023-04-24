using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_05.cs
 */
public class Example_05 {
    public Example_05() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_05.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f1);
        text.SetLocation(300f, 300f);
        for (int i = 0; i < 360; i += 15) {
            text.SetTextDirection(i);
            text.SetUnderline(true);
            // text.SetStrikeLine(true);
            text.SetText("             Hello, World -- " + i + " degrees.");
            text.DrawOn(page);
        }

        text = new TextLine(f1, "WAVE AWAY");
        text.SetLocation(70f, 50f);
        text.DrawOn(page);

        f1.SetKernPairs(true);
        text.SetLocation(70f, 70f);
        text.DrawOn(page);

        f1.SetKernPairs(false);
        text.SetLocation(70f, 90f);
        text.DrawOn(page);

        f1.SetSize(8.0f);
        text = new TextLine(f1, "-- font.SetKernPairs(false);");
        text.SetLocation(150f, 50f);
        text.DrawOn(page);
        text.SetLocation(150f, 90f);
        text.DrawOn(page);
        text = new TextLine(f1, "-- font.SetKernPairs(true);");
        text.SetLocation(150f, 70f);
        text.DrawOn(page);

        Point point = new Point(300f, 300f);
        point.SetShape(Point.CIRCLE);
        point.SetFillShape(true);
        point.SetColor(Color.blue);
        point.SetRadius(37f);
        point.DrawOn(page);
        point.SetRadius(25f);
        point.SetColor(Color.white);
        point.DrawOn(page);

        page.SetPenWidth(1.0f);
        page.DrawEllipse(300f, 600f, 100f, 50f);

        f1.SetSize(14f);
        String unicode = "\u20AC\u0020\u201A\u0192\u201E\u2026\u2020\u2021\u02C6\u2030\u0160";
        text = new TextLine(f1, unicode);
        text.SetLocation(100f, 700f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_05();
        long time1 = sw.ElapsedMilliseconds;
        TextUtils.PrintDuration("Example_05", time0, time1);
    }
}   // End of Example_05.cs
