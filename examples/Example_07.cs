using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_07.cs
 *
 */
public class Example_07 {

    public Example_07(String fontType) {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_07.pdf", FileMode.Create)),
                Compliance.PDF_UA);
        pdf.SetTitle("This is a PDF/UA compliant PDF.");

        Font f1 = new Font(pdf,
                new FileStream(
                        "fonts/OpenSans/OpenSans-Regular.ttf.stream",
                        FileMode.Open,
                        FileAccess.Read),
                Font.STREAM);
        f1.SetSize(18f);

        Page page = new Page(pdf, A4.LANDSCAPE);

        f1.SetSize(72f);
        page.AddWatermark(f1, "This is a Draft");
        f1.SetSize(18f);

        float xPos = 20f;
        float yPos = 10f;
        StringBuilder buf = new StringBuilder();
        TextLine textLine = new TextLine(f1);
        int j = 0;
        for (int i = 0x410; i < 0x46F; i++) {
            if (j % 64 == 0) {
                textLine.SetText(buf.ToString());
                textLine.SetLocation(xPos, yPos);
                textLine.DrawOn(page);
                buf.Length = 0;
                yPos += 24f;
            }
            buf.Append((char) i);
            j++;
        }
        textLine.SetText(buf.ToString());
        textLine.SetLocation(xPos, yPos);
        textLine.DrawOn(page);

        yPos = 70f;
        buf.Length = 0;
        j = 0;
        for (int i = 0x20; i < 0x7F; i++) {
            if (j % 64 == 0) {
                textLine.SetText(buf.ToString());
                textLine.SetLocation(xPos, yPos);
                textLine.DrawOn(page);
                buf.Length = 0;
                yPos += 24f;
            }
            buf.Append((char) i);
            j++;
        }
        textLine.SetText(buf.ToString());
        textLine.SetLocation(xPos, yPos);
        textLine.DrawOn(page);

        page = new Page(pdf, A4.LANDSCAPE);
        textLine.SetText("Hello, World!");
        textLine.SetLocation(xPos, 34f);
        textLine.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_07("stream");
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_07 => " + (time1 - time0));
    }

}   // End of Example_07.cs
