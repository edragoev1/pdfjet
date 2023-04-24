package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_10.java
 */
public class Example_10 {
    public Example_10() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_10.pdf")));
        pdf.setTitle("Using TextColumn and Paragraph classes");
        pdf.setSubject("Examples");
        pdf.setAuthor("Innovatics Inc.");

        Image image1 = new Image(pdf, "images/sz-map.png");

        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f2 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f4 = new Font(pdf, CoreFont.HELVETICA_OBLIQUE);

        f1.setSize(10f);
        f2.setSize(14f);
        f3.setSize(12f);
        f4.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        image1.setLocation(90f, 35f);
        image1.scaleBy(0.75f);
        image1.drawOn(page);

        int rotate = 0;
        // int rotate = 90;
        // int rotate = 270;
        TextColumn column = new TextColumn(rotate);
        column.setSpaceBetweenLines(5.0f);
        column.setSpaceBetweenParagraphs(10.0f);

        Paragraph p1 = new Paragraph();
        p1.setAlignment(Align.CENTER);
        p1.add(new TextLine(f2, "Switzerland"));

        Paragraph p2 = new Paragraph();
        p2.add(new TextLine(f2, "Introduction"));

        StringBuilder buf = new StringBuilder();
        buf.append("The Swiss Confederation was founded in 1291 as a defensive ");
        buf.append("alliance among three cantons. In succeeding years, other ");
        buf.append("localities joined the original three. ");
        buf.append("The Swiss Confederation secured its independence from the ");
        buf.append("Holy Roman Empire in 1499. Switzerland's sovereignty and ");
        buf.append("neutrality have long been honored by the major European ");
        buf.append("powers, and the country was not involved in either of the ");
        buf.append("two World Wars. The political and economic integration of ");
        buf.append("Europe over the past half century, as well as Switzerland's ");
        buf.append("role in many UN and international organizations, has ");
        buf.append("strengthened Switzerland's ties with its neighbors. ");
        buf.append("However, the country did not officially become a UN member ");
        buf.append("until 2002.");

        Paragraph p3 = new Paragraph();
        // p3.setAlignment(Align.LEFT);
        // p3.setAlignment(Align.RIGHT);
        p3.setAlignment(Align.JUSTIFY);
        TextLine text = new TextLine(f1, buf.toString());
        p3.add(text);

        buf = new StringBuilder();
        buf.append("Switzerland remains active in many UN and international ");
        buf.append("organizations but retains a strong commitment to neutrality.");

        text = new TextLine(f1, buf.toString());
        text.setColor(Color.red);
        p3.add(text);

        Paragraph p4 = new Paragraph();
        p4.add(new TextLine(f3, "Economy"));

        buf = new StringBuilder();
        buf.append("Switzerland is a peaceful, prosperous, and stable modern ");
        buf.append("market economy with low unemployment, a highly skilled ");
        buf.append("labor force, and a per capita GDP larger than that of the ");
        buf.append("big Western European economies. The Swiss in recent years ");
        buf.append("have brought their economic practices largely into ");
        buf.append("conformity with the EU's to enhance their international ");
        buf.append("competitiveness. Switzerland remains a safehaven for ");
        buf.append("investors, because it has maintained a degree of bank secrecy ");
        buf.append("and has kept up the franc's long-term external value. ");
        buf.append("Reflecting the anemic economic conditions of Europe, GDP ");
        buf.append("growth stagnated during the 2001-03 period, improved during ");
        buf.append("2004-05 to 1.8% annually and to 2.9% in 2006.");

        Paragraph p5 = new Paragraph();
        p5.setAlignment(Align.JUSTIFY);
        text = new TextLine(f1, buf.toString());
        p5.add(text);

        text = new TextLine(f4,
                "Even so, unemployment has remained at less than half the EU average.");
        text.setColor(Color.blue);
        p5.add(text);

        Paragraph p6 = new Paragraph();
        p6.setAlignment(Align.RIGHT);

        text = new TextLine(f1, "Source: The world fact book.");
        text.setColor(Color.blue);
        text.setURIAction(
                "https://www.cia.gov/library/publications/the-world-factbook/geos/sz.html");
        p6.add(text);

        column.addParagraph(p1);
        column.addParagraph(p2);
        column.addParagraph(p3);
        column.addParagraph(p4);
        column.addParagraph(p5);
        column.addParagraph(p6);

        if (rotate == 0) {
            column.setLocation(90f, 300f);
        } else if (rotate == 90) {
            column.setLocation(90f, 780f);
        } else if (rotate == 270) {
            column.setLocation(550f, 310f);
        }

        float columnWidth = 470f;
        column.setSize(columnWidth, 100f);
        float[] xy = column.drawOn(page);

        if (rotate == 0) {
            Line line = new Line(
                    xy[0],
                    xy[1],
                    xy[0] + columnWidth,
                    xy[1]);
            line.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_10();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_10", time0, time1);
    }
}   // End of Example_10.java
