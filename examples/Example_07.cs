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
                new FileStream("Example_07.pdf", FileMode.Create)));
        // pdf.SetPageLayout(PageLayout.SINGLE_PAGE);
        // pdf.SetPageMode(PageMode.FULL_SCREEN);

        Font f1 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSerif-Regular.ttf.stream",
                FileMode.Open,
                FileAccess.Read),
                Font.STREAM);
        f1.SetSize(15f);

        Font f2 = new Font(pdf, new FileStream(
                "fonts/Droid/DroidSerif-Italic.ttf.stream",
                FileMode.Open,
                FileAccess.Read),
                Font.STREAM);
        f2.SetSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        f1.SetSize(72f);
        page.AddWatermark(f1, "This is a Draft");
        f1.SetSize(15f);

        float x_pos = 70f;
        float y_pos = 70f;
        TextLine text = new TextLine(f1);
        text.SetLocation(x_pos, y_pos);
        StringBuilder buf = new StringBuilder();
        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        y_pos += 24f;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2
                    || i == 0x3CF
                    || i == 0x3D0
                    || i == 0x3D3
                    || i == 0x3D4
                    || i == 0x3D5
                    || i == 0x3D7
                    || i == 0x3D8
                    || i == 0x3D9
                    || i == 0x3DA
                    || i == 0x3DB
                    || i == 0x3DC
                    || i == 0x3DD
                    || i == 0x3DE
                    || i == 0x3DF
                    || i == 0x3E0
                    || i == 0x3EA
                    || i == 0x3EB
                    || i == 0x3EC
                    || i == 0x3ED
                    || i == 0x3EF) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.Append((char) 0x0020);
            }
            else {
                buf.Append((char) i);
            }
        }

        y_pos += 24f;
        buf = new StringBuilder();
        for (int i = 0x410; i < 0x46F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        x_pos = 370f;
        y_pos = 70f;
        text = new TextLine(f2);
        text.SetLocation(x_pos, y_pos);
        buf = new StringBuilder();
        for (int i = 0x20; i < 0x7F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

        y_pos += 24f;
        buf = new StringBuilder();
        for (int i = 0x390; i < 0x3EF; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            if (i == 0x3A2
                    || i == 0x3CF
                    || i == 0x3D0
                    || i == 0x3D3
                    || i == 0x3D4
                    || i == 0x3D5
                    || i == 0x3D7
                    || i == 0x3D8
                    || i == 0x3D9
                    || i == 0x3DA
                    || i == 0x3DB
                    || i == 0x3DC
                    || i == 0x3DD
                    || i == 0x3DE
                    || i == 0x3DF
                    || i == 0x3E0
                    || i == 0x3EA
                    || i == 0x3EB
                    || i == 0x3EC
                    || i == 0x3ED
                    || i == 0x3EF) {
                // Replace .notdef with space to generate PDF/A compliant PDF
                buf.Append((char) 0x0020);
            }
            else {
                buf.Append((char) i);
            }
        }

        y_pos += 24f;
        buf = new StringBuilder();
        for (int i = 0x410; i < 0x46F; i++) {
            if (i % 16 == 0) {
                text.SetText(buf.ToString());
                text.SetLocation(x_pos, y_pos += 24f);
                text.DrawOn(page);
                buf = new StringBuilder();
            }
            buf.Append((char) i);
        }

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
