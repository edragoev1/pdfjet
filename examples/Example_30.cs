using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_30.cs
 */
public class Example_30 {
    public Example_30() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_30.pdf", FileMode.Create)));

        Font font = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/map407.png");
        image1.SetLocation(10f, 100f);

        Image image2 = new Image(pdf, "images/qrcode.png");
        image2.SetLocation(10f, 100f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(font);
        textLine.SetText("© OpenStreetMap contributors");
        textLine.SetLocation(430f, 655f);
        float[] xy = textLine.DrawOn(page);

        textLine = new TextLine(font);
        textLine.SetText("http://www.openstreetmap.org/copyright");
        textLine.SetURIAction("http://www.openstreetmap.org/copyright");
        textLine.SetLocation(380f, xy[1] + font.GetHeight());
        textLine.DrawOn(page);

        OptionalContentGroup group = new OptionalContentGroup("Map");
        group.Add(image1);
        group.SetVisible(true);
        // group.SetPrintable(true);
        group.DrawOn(page);

        TextBox textBox = new TextBox(font);
        textBox.SetText("Hello Blue Layer Text");
        textBox.SetLocation(300f, 200f);

        Line line = new Line();
        line.SetPointA(300f, 250f);
        line.SetPointB(500f, 250f);
        line.SetWidth(2f);
        line.SetColor(Color.blue);

        group = new OptionalContentGroup("Blue");
        group.Add(textBox);
        group.Add(line);
        // group.SetVisible(true);
        group.DrawOn(page);

        line = new Line();
        line.SetPointA(300f, 260f);
        line.SetPointB(500f, 260f);
        line.SetWidth(2f);
        line.SetColor(Color.red);

        group = new OptionalContentGroup("Barcode");
        group.Add(image2);
        group.Add(line);
        group.SetVisible(true);
        group.SetPrintable(true);
        group.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_30();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_30", time0, time1);
    }
}   // End of Example_30.cs
