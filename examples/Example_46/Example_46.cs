using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

/**
 * Example_46.cs
 */
public class Example_46 {
    public Example_46() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_46.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/map407.png");
        image1.SetLocation(10f, 100f);

        Image image2 = new Image(pdf, "images/qrcode.png");
        image2.SetLocation(10f, 100f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f2, "© OpenStreetMap contributors");
        textLine.SetLocation(10f, 655f);
        float[] xy = textLine.DrawOn(page);

        textLine = new TextLine(f2, "http://www.openstreetmap.org/copyright");
        textLine.SetURIAction("http://www.openstreetmap.org/copyright");
        textLine.SetLocation(10f, xy[1] + f2.GetHeight());
        textLine.DrawOn(page);

        OptionalContentGroup group = new OptionalContentGroup(pdf, "Map");
        group.Add(image1);
        group.SetVisible(true);
        group.SetPrintable(true);
        group.DrawOn(page);

        TextBox textBox = new TextBox(f1);
        // textBox.SetFontSize(16f);
        textBox.SetText("Blue Layer Text");
        textBox.SetLocation(10f, 130f);

        Line line = new Line();
        line.SetStartPoint(300f, 150f);
        line.SetEndPoint(500f, 150f);
        line.SetStrokeWidth(2f);
        line.SetStrokeColor(Color.blue);

        group = new OptionalContentGroup(pdf, "Blue Line");
        group.Add(textBox);
        group.Add(line);
        group.SetVisible(true);
        group.DrawOn(page);

        line = new Line();
        line.SetStartPoint(300f, 160f);
        line.SetEndPoint(500f, 160f);
        line.SetStrokeWidth(2f);
        line.SetStrokeColor(Color.red);

        group = new OptionalContentGroup(pdf, "Barcode");
        group.Add(image2);
        group.Add(line);
        group.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_46();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_46", time0, time1);
    }
}   // End of Example_46.cs
