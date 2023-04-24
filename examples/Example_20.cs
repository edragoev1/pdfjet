using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;


/**
 * Example_20.cs
 *
 * This example shows how to use existing logo saved as PDF file in new PDF document.
 */
class Example_20 {

    public Example_20() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_20.pdf", FileMode.Create)));

        BufferedStream bis = new BufferedStream(
                new FileStream("data/testPDFs/PDFjetLogo.pdf", FileMode.Open));
        List<PDFobj> objects = pdf.Read(bis);

        pdf.AddResourceObjects(objects);

        Font f1 = new Font(pdf, new FileStream(
                "fonts/OpenSans/OpenSans-Regular.ttf.stream",
                FileMode.Open,
                FileAccess.Read), Font.STREAM);
        f1.SetSize(18f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        PDFobj contents = pages[0].GetContentsObject(objects);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float height = 105f;    // The logo height in points.
        float x = 50f;
        float y = 50f;
        float xScale = 0.5f;
        float yScale = 0.5f;

        page.DrawContents(
                contents.GetData(),
                height,
                x,
                y,
                xScale,
                yScale);

        page.SetPenColor(Color.darkblue);
        page.SetPenWidth(0f);
        page.DrawRect(0f, 0f, 50f, 50f);

        PDFjet.NET.Path path = new PDFjet.NET.Path();

        path.Add(new Point(13.0f,  0.0f));
        path.Add(new Point(15.5f,  4.5f));

        path.Add(new Point(18.0f,  3.5f));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(20.5f,  7.5f));

        path.Add(new Point(21.0f,  9.5f));
        path.Add(new Point(25.0f,  9.0f));
        path.Add(new Point(24.0f, 13.0f));
        path.Add(new Point(25.5f, 14.0f));
        path.Add(new Point(19.0f, 19.0f));
        path.Add(new Point(20.0f, 21.5f));
        path.Add(new Point(13.5f, 20.5f));
        path.Add(new Point(13.5f, 27.0f));
        path.Add(new Point(12.5f, 27.0f));
        path.Add(new Point(12.5f, 20.5f));
        path.Add(new Point( 6.0f, 21.5f));
        path.Add(new Point( 7.0f, 19.0f));
        path.Add(new Point( 0.5f, 14.0f));
        path.Add(new Point( 2.0f, 13.0f));
        path.Add(new Point( 1.0f,  9.0f));
        path.Add(new Point( 5.0f,  9.5f));

        path.Add(new Point( 5.5f,  7.5f));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.Add(new Point( 8.0f,  3.5f));

        path.Add(new Point(10.5f,  4.5f));
        path.SetClosePath(true);
        path.SetColor(Color.red);
        // path.SetFillShape(true);
        path.SetLocation(100f, 100f);
        path.ScaleBy(10f);

        path.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        TextLine line = new TextLine(f1, "Hello, World!");
        line.SetLocation(50f, 50f);
        line.DrawOn(page);

        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io",
                ErrorCorrectLevel.L);   // Low
        qr.SetModuleLength(3f);
        qr.SetLocation(50f, 200f);
        qr.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_20();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_20", time0, time1);
    }

}   // End of Example_20.cs
