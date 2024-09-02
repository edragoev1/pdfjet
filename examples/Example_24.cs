using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_24.cs
 */
public class Example_24 {
    public Example_24() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_24.pdf", FileMode.Create)));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        Image image_00 = new Image(pdf, "images/gr-map.jpg");
        // Image image_01 = new Image(pdf, "images/linux-logo.png");
        Image image_01 = new Image(pdf, "images/linux-logo.png.stream");
        Image image_02 = new Image(pdf, "images/ee-map.png");
        Image image_03 = new Image(pdf, "images/rgb24pal.bmp");

        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine textline_00 = new TextLine(font, "This is a JPEG image.");
        textline_00.SetTextDirection(0);
        textline_00.SetLocation(50f, 50f);
        float[] point = textline_00.DrawOn(page);
        image_00.SetLocation(50f, point[1]).ScaleBy(0.25f).DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textline_01 = new TextLine(font, "This is a PNG_STREAM image.");
        textline_01.SetTextDirection(0);
        textline_01.SetLocation(50f, 50f);
        point = textline_01.DrawOn(page);
        image_01.SetLocation(50f, point[1]).DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        TextLine textline_02 = new TextLine(font, "This is a PNG image.");
        textline_02.SetTextDirection(0);
        textline_02.SetLocation(50f, 50f);
        point = textline_02.DrawOn(page);
        image_02.SetLocation(50f, point[1]).ScaleBy(0.75f).DrawOn(page);

        TextLine textline_03 = new TextLine(font, "This is a BMP image.");
        textline_03.SetTextDirection(0);
        textline_03.SetLocation(50f, 620f);
        point = textline_03.DrawOn(page);
        image_03.SetLocation(50f, point[1]).ScaleBy(0.75f).DrawOn(page);

        new Image(pdf, "images/fruit.jpg");
        new Image(pdf, "images/linux-logo.png.stream");

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
