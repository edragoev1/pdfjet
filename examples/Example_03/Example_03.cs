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

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.SetSize(10f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f2.SetSize(10f);

        Font f3 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);
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

        Text text = new Text(paragraphs);
        text.SetLocation(70f, 50f);
        text.SetWidth(500f);
        text.SetBorderColor(Color.blue);
        text.DrawOn(page);

        Dictionary<String, int> colorMap = new Dictionary<String, int>();
        colorMap["Physics"] = Color.red;
        colorMap["Extraordinary"] = Color.blue;
        paragraphs = Text.paragraphsFromFile(f1, "data/physics.txt");
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                p.GetTextLines()[0].SetFont(f2).SetFontSize(18f).SetTextColor(Color.navy);
            } else {
                p.SetColor(Color.darkgray);
                p.SetColorMap(colorMap);
            }
        }
        text = new Text(paragraphs);
        text.SetLocation(70f, 150f);
        text.SetWidth(500f);
        text.SetBorderColor(Color.blue);
        text.DrawOn(page);

        int paragraphNumber = 1;
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, paragraphNumber.ToString() + ".")
                        .SetLocation(p.xText - 15f, p.yText)
                        .DrawOn(page);
                new Line(p.x1 - 3f, p.y1, p.x1 - 3f, p.y2)
                        .SetColor(Color.navy)
                        .SetWidth(1f).DrawOn(page);
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
        TextUtils.PrintDuration("Example_03", time0, time1);
    }
}   // End of Example_03.cs
