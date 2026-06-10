using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_25.cs
 */
public class Example_25 {
    public Example_25() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_25.pdf", FileMode.Create)));

        // Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        // Font f2 = new Font(pdf, IBMPlexSans.Bold);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);

        Page page = new Page(pdf, Letter.PORTRAIT);

        CompositeTextLine composite = new CompositeTextLine(50f, 50f);
        composite.SetFontSize(24f);

        TextLine text1 = new TextLine(f1, "C");
        text1.SetTextColor(Color.dodgerblue);

        TextLine text2 = new TextLine(f2, "6");
        text2.SetTextEffect(Effect.SUBSCRIPT);

        TextLine text3 = new TextLine(f1, "H");
        text3.SetTextColor(Color.dodgerblue);

        TextLine text4 = new TextLine(f2, "12");
        text4.SetTextEffect(Effect.SUBSCRIPT);

        TextLine text5 = new TextLine(f1, "O");
        text5.SetTextColor(Color.dodgerblue);

        TextLine text6 = new TextLine(f2, "6");
        text6.SetTextEffect(Effect.SUBSCRIPT);

        composite.AddComponent(text1);
        composite.AddComponent(text2);
        composite.AddComponent(text3);
        composite.AddComponent(text4);
        composite.AddComponent(text5);
        composite.AddComponent(text6);

        float[] xy = composite.DrawOn(page);

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        CompositeTextLine composite2 = new CompositeTextLine(50f, 200f);
        composite2.SetFontSize(24f);

        text1 = new TextLine(f1, "SO");
        text2 = new TextLine(f2, "4");
        text2.SetTextEffect(Effect.SUBSCRIPT);
        text3 = new TextLine(f2, "2-"); // Use bold font here
        text3.SetTextEffect(Effect.SUPERSCRIPT);

        composite2.AddComponent(text1);
        composite2.AddComponent(text2);
        composite2.AddComponent(text3);

        composite2.DrawOn(page);

        float[] yy = composite2.GetMinMax();
        Line line1 = new Line(50f, yy[0], 200f, yy[0]);
        Line line2 = new Line(50f, yy[1], 200f, yy[1]);
        line1.DrawOn(page);
        line2.DrawOn(page);

        DonutChart chart = new DonutChart(f1, f2, false);
        chart.SetLocation(300f, 350f);
        chart.SetR1AndR2(200f, 100f);
        chart.AddSlice(new Slice(10f, Color.red));
        chart.AddSlice(new Slice(20f, Color.green));
        chart.AddSlice(new Slice(30f, Color.blue));
        chart.AddSlice(new Slice(40f, Color.peachpuff));
        // chart.AddSlice(new Slice(75f, Color.red));
        // chart.AddSlice(new Slice(25f, Color.blue));
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_25();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_25", time0, time1);
    }
}   // End of Example_25.cs
