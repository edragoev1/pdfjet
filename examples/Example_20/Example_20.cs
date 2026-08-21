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

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(18f);

        List<PDFobj> pages = pdf.GetPageObjects(objects);
        PDFobj content = pages[0].GetContentObject(objects);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float height = 105f;    // The logo height in points.
        float x = 350f;
        float y = 50f;
        float xScale = 0.5f;
        float yScale = 0.5f;

        page.DrawContents(
                content.GetData(),
                height,
                x,
                y,
                xScale,
                yScale);

        page.SetPenColor(Color.darkblue);
        page.SetPenWidth(0f);
        page.DrawRect(300f, 0f, 50f, 50f);

        PDFjet.NET.Path path = new PDFjet.NET.Path();

        path.Add(new Point(12.5f,  0.0f));
        path.Add(new Point(15.0f,  4.5f));
        path.Add(new Point(17.5f,  3.5f));
        path.Add(new Point(15.0f, 13.5f, Point.ControlPointC));
        path.Add(new Point(15.0f, 13.5f, Point.ControlPointC));
        path.Add(new Point(20.0f,  7.5f));
        path.Add(new Point(20.5f,  9.5f));
        path.Add(new Point(24.5f,  9.0f));
        path.Add(new Point(23.5f, 13.0f));
        path.Add(new Point(25.0f, 14.0f));
        path.Add(new Point(18.5f, 19.0f));
        path.Add(new Point(19.5f, 21.5f));
        path.Add(new Point(13.0f, 20.5f));
        path.Add(new Point(13.0f, 27.0f));
        path.Add(new Point(12.0f, 27.0f));
        path.Add(new Point(12.0f, 20.5f));
        path.Add(new Point( 5.5f, 21.5f));
        path.Add(new Point( 6.5f, 19.0f));
        path.Add(new Point( 0.0f, 14.0f));
        path.Add(new Point( 1.5f, 13.0f));
        path.Add(new Point( 0.5f,  9.0f));
        path.Add(new Point( 4.5f,  9.5f));
        path.Add(new Point( 5.0f,  7.5f));
        path.Add(new Point(10.0f, 13.5f, Point.ControlPointC));
        path.Add(new Point(10.0f, 13.5f, Point.ControlPointC));
        path.Add(new Point( 7.5f,  3.5f));
        path.Add(new Point(10.0f,  4.5f));
        path.Add(new Point(12.5f,  0.0f));

        path.SetLocation(50f, 50f);
        path.SetStrokeColor(Color.red);
        path.SetStrokeWidth(0.6);       // !! Width narrower than 0.6 may prevent some PDF !!
                                        // !! viewers from displaying the rotated Path !!
        path.SetRotationClockwise(45);  // !! See SetStrokeWidth !!
        path.SetScaleFactor(10f);
        path.DrawOn(page);

        Point point = new Point();
        // point.SetShape(Point.CIRCLE);
        // point.SetShape(Point.STAR);
        // point.SetShape(Point.UP_ARROW);
        point.SetShape(Point.PLUS);
        point.SetRadius(50f);
        point.SetLocation(350f, 350f);
        point.DrawOn(page);

        path = new PDFjet.NET.Path();
        path.Add(new Point(50f, 700f));
        path.Add(new Point(50f, 500f, Point.ControlPointV));
        path.Add(new Point(250f, 700f));
        path.SetStrokeColor(Color.black);
        path.SetStrokeWidth(3f);
        path.DrawOn(page);

        path = new PDFjet.NET.Path();
        path.Add(new Point(50f, 700f));
        path.Add(new Point(250f, 500f, Point.ControlPointY));
        path.Add(new Point(250f, 700f));
        path.SetStrokeColor(Color.blue);
        path.SetStrokeWidth(3f);
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
