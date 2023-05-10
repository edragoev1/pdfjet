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

        SVGImage image = new SVGImage("images/svg-test/europe.svg");
        image.SetLocation(-150f, 0f);
        float[] xy = image.DrawOn(page);
        
        image = new SVGImage("images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg");
        image.SetLocation(20f, 670f);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg");
        image.SetLocation(xy[0], 670f);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg");
        image.SetLocation(xy[0], 670f);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg");
        image.SetLocation(xy[0], 670f);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        image.SetLocation(xy[0], 670);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg-test/test-CS.svg");
        image.SetLocation(xy[0], 670);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg-test/test-QQ1.svg");
        image.SetLocation(xy[0], 670);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg-test/menu-icon.svg");
        image.SetLocation(xy[0], 670);
        xy = image.DrawOn(page);

        image = new SVGImage("images/svg-test/menu-icon-close.svg");
        image.SetLocation(xy[0], 670);
        image.ScaleBy(2.0f);
        xy = image.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_33", time0, time1);
    }
}   // End of Example_33.cs
