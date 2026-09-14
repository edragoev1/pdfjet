using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_03.java
 */
public class Example_03 {
    public Example_03() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_03.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.SetSize(10f);

        Font f3 = new Font(pdf, IBMPlexSans.Italic);
        f3.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new List<Paragraph>();
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
                        .SetUnderline(true))
                .Add(new TextLine(f2, "This text is bold!").SetTextColor(Color.blue));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .SetUnderline(true))
                .Add(new TextLine(f3, "This text is using italic font.").SetTextColor(Color.green));
        paragraphs.Add(paragraph);

        TextFrame text = new TextFrame(paragraphs);
        text.SetLocation(70f, 50f);
        text.SetWidth(500f);
        text.SetBorders(true);
        text.SetBorderColor(Color.blue);
        text.DrawOn(page);

        int paragraphNumber = 1;
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, paragraphNumber.ToString() + ".")
                        .SetLocation(p.GetTextX() - 15f, p.GetTextY())
                        .DrawOn(page);
                paragraphNumber++;
            }
        }

        Dictionary<String, int> colorMap = new Dictionary<String, int>();
        colorMap["Physics"] = Color.red;
        colorMap["physics"] = Color.red;
        colorMap["Experimentation"] = Color.orange;
        colorMap["science"] = Color.blue;
        paragraphs = Paragraph.ParagraphsFromFile(f1, "data/physics.txt");
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                p.GetTextLines()[0].SetFont(f2).SetFontSize(24f);
                p.GetTextLines()[0].SetTextColor(Color.navy);
            } else {
                p.SetTextColor(Color.gray);
                p.SetHighlightColors(colorMap);
            }
        }

        text = new TextFrame(paragraphs);
        text.SetLocation(70f, 150f);
        text.SetWidth(500f);
        text.SetBorders(true);
        text.SetBorderColor(Color.blue);
        text.DrawOn(page);

        paragraphNumber = 1;
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, paragraphNumber.ToString() + ".")
                        .SetLocation(p.GetTextX() - 15f, p.GetTextY())
                        .DrawOn(page);
                new Line(p.GetX1() - 3f, p.GetY1(), p.GetX1() - 3f, p.GetY2())
                        .SetStrokeColor(Color.navy)
                        .SetStrokeWidth(1f).DrawOn(page);
                paragraphNumber++;
            }
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_03();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_03 => " + (time1 - time0) + " ms");
    }
}   // End of Example_03.cs
