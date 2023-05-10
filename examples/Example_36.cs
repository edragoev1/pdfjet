using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_36.cs
 */
public class Example_36 {
    public Example_36() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_36.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Page page1 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);

        Image image1 = new Image(pdf, "images/ee-map.png");
        Image image2 = new Image(pdf, "images/fruit.jpg");
        Image image3 = new Image(pdf, "images/palette.bmp");

        TextLine text = new TextLine(f1,
                "The map below is an embedded PNG image");
        text.SetLocation(90f, 30f);
        text.DrawOn(page1);

        image1.SetLocation(90f, 40f);
        image1.ScaleBy(2f/3f);
        image1.DrawOn(page1);

        text.SetText(
                "JPG image file embedded once and drawn 3 times");
        text.SetLocation(90f, 550f);
        text.DrawOn(page1);

        image2.SetLocation(90f, 560f);
        image2.ScaleBy(0.5f);
        image2.DrawOn(page1);

        image2.SetLocation(260f, 560f);
        image2.ScaleBy(0.5f);
        image2.RotateClockwise(90);
        image2.DrawOn(page1);

        image2.SetLocation(350f, 560f);
        image2.RotateClockwise(0);
        image2.ScaleBy(0.5f);
        image2.DrawOn(page1);

        image3.SetLocation(390f, 630f);
        image3.ScaleBy(0.5f);
        image3.DrawOn(page1);

        Page page2 = new Page(pdf, A4.PORTRAIT, Page.DETACHED);
        image1.DrawOn(page2);

        text.SetText("Hello, World!!");
        text.SetLocation(90f, 800f);
        text.DrawOn(page2);

        text.SetText(
                "The map on the right is an embedded BMP image");
        text.SetUnderline(true);
        text.SetStrikeout(true);
        text.SetTextDirection(15);
        text.SetLocation(90f, 800f);
        text.DrawOn(page1);

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
