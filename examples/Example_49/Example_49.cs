using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_49.cs
 */
public class Example_49 {
    public Example_49() {
        PDF pdf = new PDF(new BufferedStream(
            new FileStream("Example_49.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(14f);

        Font f2 = new Font(pdf, IBMPlexSans.Italic);
        f2.SetSize(16f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Paragraph paragraph1 = new Paragraph()
                .Add(new TextLine(f1, "Hello"))
                .Add(new TextLine(f1, "W").SetTextColor(Color.black))
                .Add(new TextLine(f1, "o").SetTextColor(Color.red))
                .Add(new TextLine(f1, "r").SetTextColor(Color.green))
                .Add(new TextLine(f1, "l").SetTextColor(Color.blue))
                .Add(new TextLine(f1, "d").SetTextColor(Color.black))
                .Add(new TextLine(f1, "$").SetVerticalOffset(f1.GetAscent() - f2.GetAscent()))
                .Add(new TextLine(f2, "29.95").SetTextColor(Color.blue))
                .SetAlignment(Align.RIGHT);

        Paragraph paragraph2 = new Paragraph()
                .Add(new TextLine(f1, "Hello"))
                .Add(new TextLine(f1, "World"))
                .Add(new TextLine(f1, "$"))
                .Add(new TextLine(f2, "29.95").SetTextColor(Color.blue))
                .SetAlignment(Align.RIGHT);

        TextColumn column = new TextColumn();
        column.AddParagraph(paragraph1);
        column.AddParagraph(paragraph2);
        column.SetLocation(70f, 150f);
        column.SetWidth(500f);
        column.DrawOn(page);


        List<Paragraph> paragraphs = new List<Paragraph>();
        paragraphs.Add(paragraph1);
        paragraphs.Add(paragraph2);

        Text text = new Text(paragraphs);
        text.SetLocation(70f, 200f);
        text.SetWidth(500f);
        text.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_49();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_49", time0, time1);
    }
}   // End of Example_49.cs
