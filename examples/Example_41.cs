using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_41.java
 *
 */
public class Example_41 {

    public Example_41() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_41.pdf", FileMode.Create)));

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
                .Add(new TextLine(f2, "This text is bold!").SetColor(Color.blue));

        paragraphs.Add(paragraph);

        paragraph = new Paragraph()
                .Add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it.")
                        .SetUnderline(true))
                .Add(new TextLine(f3, "This text is using italic font.").SetColor(Color.green));

        paragraphs.Add(paragraph);

        Text text = new Text(paragraphs);
        text.SetLocation(70f, 90f);
        text.SetWidth(500f);
        float[] xy = text.DrawOn(page);

        List<float[]> beginParagraphPoints = text.GetBeginParagraphPoints();
        int paragraphNumber = 1;
        for (int i = 0; i < beginParagraphPoints.Count; i++) {
            float[] point = beginParagraphPoints[i];
            new TextLine(f1, paragraphNumber.ToString() + ".")
                    .SetLocation(point[0] - 30f, point[1])
                    .DrawOn(page);
            paragraphNumber++;
        }

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_41();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine("Example_41 => " + (time1 - time0));
    }

}   // End of Example_41.cs
