using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using PDFjet.NET;

/**
 * Example_30.cs
 */
public class Example_30 {
    public Example_30() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_30.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);

        Image image1 = new Image(pdf, "images/map407.png");
        image1.SetLocation(10f, 100f);

        Image image2 = new Image(pdf, "images/qrcode.png");
        image2.SetLocation(10f, 100f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(f2, "© OpenStreetMap contributors");
        textLine.SetFontSize(10f);
        textLine.SetLocation(10f, 655f);
        float[] pointXY = textLine.DrawOn(page);

        textLine = new TextLine(f2, "http://www.openstreetmap.org/copyright");
        textLine.SetFontSize(10f);
        textLine.SetURIAction("http://www.openstreetmap.org/copyright");
        textLine.SetLocation(10f, pointXY[1] + f2.GetBodyHeight(10f));
        textLine.DrawOn(page);

        OptionalContentGroup group = new OptionalContentGroup(pdf, "Open Street Map");
        group.Add(image1);
        group.SetVisible(true);
        group.SetPrintable(false);
        group.DrawOn(page);

        TextBox textBox = new TextBox(f1);
        textLine.SetFontSize(16f);
        textBox.SetText("Blue Layer Text");
        textBox.SetLocation(350f, 130f);

        Line line = new Line();
        line.SetPointA(350f, 150f);
        line.SetPointB(550f, 150f);
        line.SetWidth(2f);
        line.SetColor(Color.blue);

        group = new OptionalContentGroup(pdf, "Blue Layer");
        group.Add(textBox);
        group.Add(line);
        group.SetVisible(true);
        group.DrawOn(page);

        line = new Line();
        line.SetPointA(350f, 160f);
        line.SetPointB(550f, 160f);
        line.SetWidth(2f);
        line.SetColor(Color.red);

        group = new OptionalContentGroup(pdf, "Barcode");
        group.Add(image2);
        group.Add(line);
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
