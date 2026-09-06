using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_36.cs
 */
public class Example_36 {
    public Example_36() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_36.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Image image1 = new Image(pdf, "images/ee-map.png");
        Image image2 = new Image(pdf, "images/spain-admin.jpg");

        Page page1 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        TextLine text = new TextLine(f1, "The map below is an embedded PNG image");
        text.SetLocation(90f, 30f);
        float[] xy1 = text.DrawOn(page1);

        image1.SetLocation(90f, xy1[1] + 10f);
        image1.ScaleBy(0.3f);
        image1.DrawOn(page1);

        Page page2 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        text.SetText("This page was created after the second one but it was drawn first!");
        text.SetLocation(90f, 30f);
        float[] xy7 = text.DrawOn(page2);

        image2.SetLocation(90f, xy7[1] + 10f);
        image2.ScaleBy(0.1f);
        image2.DrawOn(page2);

        pdf.AddPage(page2);
        pdf.AddPage(page1);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_36();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_36", time0, time1);
    }
}   // End of Example_36.cs
