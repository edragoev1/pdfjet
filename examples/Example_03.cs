using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;
using System.Reflection;

/**
 *  Example_03.cs
 *
 */
public class Example_03 {
    public Example_03() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_03.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/ee-map.png");
        Image image2 = new Image(pdf, "images/fruit.jpg");
        Image image3 = new Image(pdf, "images/mt-map.bmp");

        Page page = new Page(pdf, A4.PORTRAIT);

        float[] xy = page.AddHeader(new TextLine(f1, "This is a header!"));

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(30f, 30f);
        box.DrawOn(page);

        page.AddFooter(new TextLine(f1, "And this is a footer."));

        TextLine text = new TextLine(f1,
                "The map below is an embedded PNG image");
        text.SetLocation(90f, 30f);
        text.SetURIAction("https://en.wikipedia.org/wiki/European_Union");
        xy = text.DrawOn(page);

        image1.SetLocation(90f, xy[1] + f1.GetDescent());
        image1.ScaleBy(2f/3f);
        image1.DrawOn(page);

        text.SetText(
                "JPG image file embedded once and drawn 3 times");
        text.SetLocation(90f, 550f);
        xy = text.DrawOn(page);

        image2.SetLocation(90f, xy[1] + f1.GetDescent());
        image2.ScaleBy(0.5f);
        image2.DrawOn(page);

        image2.SetLocation(260f, xy[1] + f1.GetDescent());
        image2.RotateClockwise(90);
        image2.ScaleBy(0.5f);
        image2.DrawOn(page);

        image2.SetLocation(350f, xy[1] + f1.GetDescent());
        image2.RotateClockwise(0);
        image2.ScaleBy(0.5f);
        image2.DrawOn(page);

        text.SetText(
                "The map on the right is an embedded BMP image");
        text.SetUnderline(true);
        text.SetVerticalOffset(3f);
        text.SetStrikeout(true);
        text.SetTextDirection(15);
        text.SetLocation(90f, 800f);
        text.DrawOn(page);

        image3.SetLocation(390f, 630f);
        image3.ScaleBy(0.5f);
        image3.DrawOn(page);

        Page page2 = new Page(pdf, A4.PORTRAIT);
        xy = image1.DrawOn(page2);

        box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page2);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        AssemblyName assembly = typeof(PDF).Assembly.GetName();
        // Console.WriteLine("{0} Version={1}", assembly.Name, assembly.Version);

        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_03();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_03", time0, time1);
    }
}   // End of Example_03.cs
