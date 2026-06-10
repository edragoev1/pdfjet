using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_24.cs
 */
public class Example_24 {
    public Example_24() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_24.pdf", FileMode.Create)));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/gr-map.jpg");
        Image image2 = new Image(pdf, "images/ee-map.png");
        Image image3 = new Image(pdf, "images/rgb24pal.bmp");

        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine1 = new TextLine(font, "This is a JPEG image.");
        textLine1.SetTextDirection(0);
        textLine1.SetLocation(50f, 50f);
        float[] point = textLine1.DrawOn(page);
        image1.SetLocation(50f, point[1] + 5f).ScaleBy(0.25f).DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textLine2 = new TextLine(font, "This is a PNG image.");
        textLine2.SetTextDirection(0);
        textLine2.SetLocation(50f, 50f);
        point = textLine2.DrawOn(page);
        image2.SetLocation(50f, point[1] + 5f).ScaleBy(0.75f).DrawOn(page);

        TextLine textLine3 = new TextLine(font, "This is a BMP image.");
        textLine3.SetTextDirection(0);
        textLine3.SetLocation(50f, 620f);
        point = textLine3.DrawOn(page);
        image3.SetLocation(50f, point[1] + 5f).ScaleBy(0.75f).DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_24();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_24", time0, time1);
    }
}   // End of Example_24.cs
