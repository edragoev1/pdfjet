using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
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

        FileStream stream = new FileStream(
                "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg",
                FileMode.Open,
                FileAccess.Read);
        SVGImage icon = new SVGImage(stream);
        icon.SetLocation(20f, 670f);
        float[] xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg-test/test-CS.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg-test/test-QQ.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg-test/menu-icon.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg-test/menu-icon-close.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
                "images/svg-test/europe.svg",
                FileMode.Open,
                FileAccess.Read);
        icon = new SVGImage(stream);
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
