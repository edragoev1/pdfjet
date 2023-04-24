using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

using PDFjet.NET;


/**
 *  Example_43.java
 *
 */
public class Example_43 {

    public Example_43() {

        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_43.pdf", FileMode.Create)));

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Paragraph> paragraphs = new List<Paragraph>();
        Paragraph p1 = new Paragraph();
        TextLine tl1 = new TextLine(f1,
"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002.");
        p1.Add(tl1);

        Paragraph p2 = new Paragraph();
        TextLine tl2 = new TextLine(f2,
"Even so, unemployment has remained at less than half the EU average.");
        p2.Add(tl2);

        paragraphs.Add(p1);
        paragraphs.Add(p2);

        Text text = new Text(paragraphs);
        text.SetLocation(50f, 50f);
        text.SetWidth(500f);
        text.DrawOn(page);

        pdf.Complete();
    }


    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_43();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_43", time0, time1);
    }

}   // End of Example_43.cs
