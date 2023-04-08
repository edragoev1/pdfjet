using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_33.cs
 */
public class Example_33 {
    public Example_33() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_33.pdf", FileMode.Create)));

        Page page = new Page(pdf, A4.PORTRAIT);
        Image image = new Image(
                pdf,
                new FileStream("images/photoshop.jpg", FileMode.Open, FileAccess.Read),
                ImageType.JPG);
        image.SetLocation(10f, 10f);
        image.ScaleBy(0.25f);
        image.DrawOn(page);

        SVGImage icon = new SVGImage("images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetLocation(20f, 670f);
        float[] xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg-test/test-CS.svg");
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg-test/test-QQ.svg");
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg-test/menu-icon.svg");
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg-test/menu-icon-close.svg");
        icon.SetLocation(xy[0], 670);
        icon.SetScale(2.0f);
        xy = icon.DrawOn(page);

        icon = new SVGImage("images/svg-test/europe.svg");
        icon.SetLocation(0f, 0f);
        icon.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_33 => " + (time1 - time0));
    }
}   // End of Example_33.cs
