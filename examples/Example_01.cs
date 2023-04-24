using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;

using PDFjet.NET;

/**
 *  Example_01.cs
 *
 */
class Example_01 {
    public Example_01() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_01.pdf", FileMode.Create)));

        Font font1 = new Font(pdf, "fonts/Droid/DroidSans.ttf.stream");
        Font font2 = new Font(pdf, "fonts/Droid/DroidSansFallback.ttf.stream");

        font1.SetSize(12f);
        font2.SetSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine textLine = new TextLine(font1, "Happy New Year!");
        textLine.SetLocation(70f, 70f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "С Новым Годом!");
        textLine.SetLocation(70f, 100f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "Ευτυχισμένο το Νέο Έτος!");
        textLine.SetLocation(70f, 130f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "新年快樂！");
        textLine.SetFallbackFont(font2);
        textLine.SetLocation(300f, 70f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "新年快乐！");
        textLine.SetFallbackFont(font2);
        textLine.SetLocation(300f, 100f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "明けましておめでとうございます！");
        textLine.SetFallbackFont(font2);
        textLine.SetLocation(300f, 130f);
        textLine.DrawOn(page);

        textLine = new TextLine(font1, "새해 복 많이 받으세요!");
        textLine.SetFallbackFont(font2);
        textLine.SetLocation(300f, 160f);
        textLine.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new List<Paragraph>();
        Paragraph paragraph = null;

        StreamReader reader = new StreamReader(
                new FileStream("data/LCG.txt", FileMode.Open, FileAccess.Read));
        int i = 0;
        String line;
        while ((line = reader.ReadLine()) != null) {
            if (line.Equals("")) {
                continue;
            }
            paragraph = new Paragraph();
            textLine = new TextLine(font1, line);
            textLine.SetFallbackFont(font2);
            paragraph.Add(textLine);
            paragraphs.Add(paragraph);
            if (i == 0) {
                textLine = new TextLine(font1,
                        "Hello, World! This is a test to check if this line will be wrapped around properly.");
                textLine.SetColor(Color.blue);
                textLine.SetUnderline(true);
			    paragraph.Add(textLine);

                textLine = new TextLine(font1, "This is a test!");
                textLine.SetColor(Color.oldgloryred);
                textLine.SetUnderline(true);
			    paragraph.Add(textLine);
            }
            i++;
        }

        Text text = new Text(paragraphs);
        text.SetLocation(50f, 50f);
        text.SetWidth(500f);
        float[] xy = text.DrawOn(page);

        int paragraphNumber = 1;
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(font1, paragraphNumber.ToString() + ".")
                        .SetLocation(p.xText - 15f, p.yText)
                        .DrawOn(page);
                paragraphNumber++;
            }
        }

        Box box = new Box();
        box.SetLocation(xy[0], xy[1]);
        box.SetSize(20f, 20f);
        box.DrawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        paragraphs = new List<Paragraph>();

        reader = new StreamReader(
                new FileStream("data/CJK.txt", FileMode.Open, FileAccess.Read));

        while ((line = reader.ReadLine()) != null) {
            if (line.Equals("")) {
                continue;
            }
            paragraph = new Paragraph();
            textLine = new TextLine(font2, line);
            textLine.SetFallbackFont(font1);
            paragraph.Add(textLine);
            paragraphs.Add(paragraph);
        }
        text = new Text(paragraphs);
        text.SetLocation(50f, 50f);
        text.SetWidth(500f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_01();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_01", time0, time1);
    }
}   // End of Example_01.cs
