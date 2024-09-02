using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_22.cs
 */
public class Example_22 {
    public Example_22() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_22.pdf", FileMode.Create)),
                Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
/*
        PDF pdf = new PDF(new FileStream("Example_22.pdf", FileMode.Create));
        Font f1 = new Font(pdf, CoreFont.HELVETICA);
*/
        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine text = new TextLine(f1, "Page #1 -> Go to Destination #3.");
        text.SetGoToAction("dest#3");
        text.SetLocation(90f, 50f);
        page.AddDestination("dest#1", 0f, 0f);
        text.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #2 -> Go to Destination #3.");
        text.SetGoToAction("dest#3");
        text.SetLocation(90f, 550f);
        page.AddDestination("dest#2", text.GetDestinationY());
        text.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #3 -> Go to Destination #4.");
        text.SetGoToAction("dest#4");
        text.SetLocation(90f, 700f);
        page.AddDestination("dest#3", text.GetDestinationY());
        text.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #4 -> Go to Destination #1.");
        text.SetGoToAction("dest#1");
        text.SetLocation(90f, 100f);
        page.AddDestination("dest#4", text.GetDestinationY());
        text.DrawOn(page);

        text = new TextLine(f1, "Page #4 -> Go to Destination #2.");
        text.SetGoToAction("dest#2");
        text.SetLocation(90f, 200f);
        text.DrawOn(page);

        // Create a box with invisible borders
        Box box = new Box(20f, 20f, 20f, 20f);
        box.SetColor(Color.white);
        box.SetGoToAction("dest#1");
        box.DrawOn(page);

        // Create an up arrow and place it in the box
        PDFjet.NET.Path path = new PDFjet.NET.Path();
        path.Add(new Point(10f,  1f));
        path.Add(new Point(17f,  9f));
        path.Add(new Point(13f,  9f));
        path.Add(new Point(13f, 19f));
        path.Add(new Point( 7f, 19f));
        path.Add(new Point( 7f,  9f));
        path.Add(new Point( 3f,  9f));
        path.SetClosePath(true);
        path.SetColor(Color.oldgloryblue);
        path.SetColor(Color.deepskyblue);
        path.SetFillShape(true);
        path.PlaceIn(box);
        path.DrawOn(page);

        FileStream fis = new FileStream(
                "images/up-arrow.png", FileMode.Open, FileAccess.Read);
        Image image = new Image(pdf, fis, ImageType.PNG);
        image.SetLocation(40f, 40f);
        image.SetGoToAction("dest#1");
        image.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_22();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_22", time0, time1);
    }
}   // End of Example_22.cs
