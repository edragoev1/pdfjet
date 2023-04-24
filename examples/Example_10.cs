using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_10.cs
 */
public class Example_10 {
    public Example_10() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_10.pdf", FileMode.Create)));
        pdf.SetTitle("Using TextColumn and Paragraph classes");
        pdf.SetSubject("Examples");
        pdf.SetAuthor("Innovatics Inc.");

        Image image1 = new Image(pdf, "images/sz-map.png");

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f4 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);

        f1.SetSize(10f);
        f2.SetSize(14f);
        f3.SetSize(12f);
        f4.SetSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        image1.SetLocation(90f, 35f);
        image1.ScaleBy(0.75f);
        image1.DrawOn(page);

        int rotate = 0;
        // int rotate = 90;
        // int rotate = 270;
        TextColumn column = new TextColumn(rotate);
        column.SetSpaceBetweenLines(5.0f);
        column.SetSpaceBetweenParagraphs(10.0f);

        Paragraph p1 = new Paragraph();
        p1.SetAlignment(Align.CENTER);
        p1.Add(new TextLine(f2, "Switzerland"));

        Paragraph p2 = new Paragraph();
        p2.Add(new TextLine(f2, "Introduction"));

        StringBuilder buf = new StringBuilder();
        buf.Append("The Swiss Confederation was founded in 1291 as a defensive ");
        buf.Append("alliance among three cantons. In succeeding years, other ");
        buf.Append("localities joined the original three. ");
        buf.Append("The Swiss Confederation secured its independence from the ");
        buf.Append("Holy Roman Empire in 1499. Switzerland's sovereignty and ");
        buf.Append("neutrality have long been honored by the major European ");
        buf.Append("powers, and the country was not involved in either of the ");
        buf.Append("two World Wars. The political and economic integration of ");
        buf.Append("Europe over the past half century, as well as Switzerland's ");
        buf.Append("role in many UN and international organizations, has ");
        buf.Append("strengthened Switzerland's ties with its neighbors. ");
        buf.Append("However, the country did not officially become a UN member ");
        buf.Append("until 2002.");

        Paragraph p3 = new Paragraph();
        // p3.SetAlignment(Align.LEFT);
        // p3.SetAlignment(Align.RIGHT);
        p3.SetAlignment(Align.JUSTIFY);
        TextLine text = new TextLine(f1, buf.ToString());
        text.SetFont(f1);
        p3.Add(text);

        buf = new StringBuilder();
        buf.Append("Switzerland remains active in many UN and international ");
        buf.Append("organizations but retains a strong commitment to neutrality.");

        text = new TextLine(f1, buf.ToString());
        text.SetColor(Color.red);
        p3.Add(text);

        Paragraph p4 = new Paragraph();
        p4.Add(new TextLine(f3, "Economy"));

        buf = new StringBuilder();
        buf.Append("Switzerland is a peaceful, prosperous, and stable modern ");
        buf.Append("market economy with low unemployment, a highly skilled ");
        buf.Append("labor force, and a per capita GDP larger than that of the ");
        buf.Append("big Western European economies. The Swiss in recent years ");
        buf.Append("have brought their economic practices largely into ");
        buf.Append("conformity with the EU's to enhance their international ");
        buf.Append("competitiveness. Switzerland remains a safehaven for ");
        buf.Append("investors, because it has maintained a degree of bank secrecy ");
        buf.Append("and has kept up the franc's long-term external value. ");
        buf.Append("Reflecting the anemic economic conditions of Europe, GDP ");
        buf.Append("growth stagnated during the 2001-03 period, improved during ");
        buf.Append("2004-05 to 1.8% annually and to 2.9% in 2006.");

        Paragraph p5 = new Paragraph();
        p5.SetAlignment(Align.JUSTIFY);
        text = new TextLine(f1, buf.ToString());
        p5.Add(text);

        text = new TextLine(f4,
                "Even so, unemployment has remained at less than half the EU average.");
        text.SetColor(Color.blue);
        p5.Add(text);

        Paragraph p6 = new Paragraph();
        p6.SetAlignment(Align.RIGHT);
        text = new TextLine(f1, "Source: The world fact book.");
        text.SetColor(Color.blue);
        text.SetURIAction(
                "https://www.cia.gov/library/publications/the-world-factbook/geos/sz.html");
        p6.Add(text);

        column.AddParagraph(p1);
        column.AddParagraph(p2);
        column.AddParagraph(p3);
        column.AddParagraph(p4);
        column.AddParagraph(p5);
        column.AddParagraph(p6);

        if (rotate == 0) {
            column.SetLocation(90f, 300f);
        }
        else if (rotate == 90) {
            column.SetLocation(90f, 780f);
        }
        else if (rotate == 270) {
            column.SetLocation(550f, 310f);
        }

        float columnWidth = 470f;
        column.SetSize(columnWidth, 100f);
        float[] xy = column.DrawOn(page);

        Line line = new Line(
                xy[0],
                xy[1],
                xy[0] + columnWidth,
                xy[1]);
        line.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_10();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_10", time0, time1);
    }
}   // End of Example_10.cs
