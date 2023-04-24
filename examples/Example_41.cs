using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_41.java
 */
public class Example_41 {
    public Example_41() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_41.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);

        f1.SetSize(10f);
        f2.SetSize(10f);
        f3.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new List<Paragraph>();
        Paragraph paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries.")
                        .SetUnderline(true))
                .Add(new TextLine(f2, "This text is bold!").SetColor(Color.blue));
        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .SetUnderline(true))
                .Add(new TextLine(f3, "This text is using italic font.").SetColor(Color.green));
        paragraphs.Add(paragraph);

        Text text = new Text(paragraphs);
        text.SetLocation(70f, 50f);
        text.SetWidth(500f);
        text.SetBorder(true);
        text.DrawOn(page);


        paragraphs = Text.paragraphsFromFile(f1, "data/physics.txt");
        int paragraphNumber = 1;
        Dictionary<String, int> colorMap = new Dictionary<String, int>();
        colorMap["Physics"] = Color.red;
        colorMap["physics"] = Color.red;
        colorMap["Experimentation"] = Color.orange;
        paragraphs = Text.paragraphsFromFile(f1, "data/physics.txt");
        float f2size = f2.GetSize();
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                f2.SetSize(24.0);
                p.GetTextLines()[0].SetFont(f2);
                p.GetTextLines()[0].SetColor(Color.navy);
            } else {
                p.SetColor(Color.gray);
                p.SetColorMap(colorMap);
            }
        }
        f2.SetSize(f2size);

        text = new Text(paragraphs);
        text.SetLocation(70f, 150f);
        text.SetWidth(500f);
        // text.SetBorder(true);
        text.DrawOn(page);

        paragraphNumber = 1;
        foreach (Paragraph p in paragraphs) {
            if (p.StartsWith("**")) {
                paragraphNumber = 1;
            } else {
                new TextLine(f2, paragraphNumber.ToString() + ".")
                        .SetLocation(p.xText - 15f, p.yText)
                        .DrawOn(page);
                paragraphNumber++;
            }
        }

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_41();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_41", time0, time1);
    }
}   // End of Example_41.cs
