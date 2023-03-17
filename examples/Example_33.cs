using System;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;
using System.Collections.Generic;


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
            FileMode.Open, FileAccess.Read);
        SVGImage icon = new SVGImage(stream);
        icon.SetLocation(20f, 670f);
        float[] xy = icon.DrawOn(page);

        stream = new FileStream(
            "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
            FileMode.Open, FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
            "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
            FileMode.Open, FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
            "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
            FileMode.Open, FileAccess.Read);
        icon = new SVGImage(stream);
        icon.SetLocation(xy[0], 670f);
        xy = icon.DrawOn(page);

        stream = new FileStream(
            "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
            FileMode.Open, FileAccess.Read);
        icon = new SVGImage(stream);
        // icon.SetFillPath(false);
        icon.SetLocation(xy[0], 670);
        xy = icon.DrawOn(page);

        stream = new FileStream(
            "images/svg-test/test-CS.svg",
            FileMode.Open, FileAccess.Read);
        icon = new SVGImage(stream);
        // icon.SetFillPath(false);
        icon.SetLocation(xy[0], 670);
        icon.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
/*
        StreamWriter writer = new StreamWriter("test.svg");
        writer.Write("<svg xmlns=\"http://www.w3.org/2000/svg\" height=\"48\" width=\"48\">\n");
        writer.Write("  <path d=\"");
        List<String> paths = SVG.GetSVGPaths("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        List<PathOp> svgPathOps = SVG.GetSVGPathOps(paths);
        List<PathOp> pdfPathOps = SVG.GetPDFPathOps(svgPathOps);
        foreach (PathOp op in pdfPathOps) {
            writer.Write(op.cmd + " ");
            foreach (String argument in op.args) {
                writer.Write(argument + " ");
            }
        }
        writer.Write("\"/>\n");
        writer.Write("</svg>\n");
        writer.Flush();
        writer.Close();
*/
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_33 => " + (time1 - time0));
    }

}   // End of Example_33.cs
