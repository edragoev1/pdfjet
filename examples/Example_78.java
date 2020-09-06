package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_78.java
 *
 */
public class Example_78 {

    public Example_78() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_78.pdf")), Compliance.PDF_A_1B);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setSize(12f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setSize(12f);

/*
        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Bold.ttf.stream"),
                Font.STREAM);
        f1.setSize(12f);

        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);
        f2.setSize(12f);
*/

        Image image1 = new Image(
                pdf,
                getClass().getResourceAsStream("../images/chess.jpg"),
                ImageType.JPG);
        image1.scaleBy(0.1f);


        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(f1));
        row.add(new Cell(f2));
        tableData.add(row);

        row = new ArrayList<Cell>();
        row.add(new Cell(f1, "Hello, World!"));
        row.add(new Cell(f2, "This is a test."));
        tableData.add(row);

        Table table = new Table();
        table.setData(tableData);
        table.setLocation(50f, 50f);
        table.setColumnWidth(0, 270f);
        table.setColumnWidth(1, 270f);


        TextColumn column = new TextColumn(0);
        column.setSpaceBetweenLines(5.0f);
        column.setSpaceBetweenParagraphs(10.0f);

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

        Paragraph p1 = new Paragraph();
        p1.setAlignment(Align.JUSTIFY);
        TextLine textLine = new TextLine(f1, buf.toString());
        p1.add(textLine);

        buf = new StringBuilder();
        buf.append("Switzerland remains active in many UN and international ");
        buf.append("organizations but retains a strong commitment to neutrality.");

        textLine = new TextLine(f1, buf.toString());
        textLine.setColor(Color.red);
        p1.add(textLine);

        Paragraph p2 = new Paragraph();
        p2.add(new TextLine(f2, "Economy"));

        buf = new StringBuilder();
        buf.append("Switzerland is a peaceful, prosperous, and stable modern ");
        buf.append("market economy with low unemployment, a highly skilled ");
        buf.append("labor force, and a per capita GDP larger than that of the ");
        buf.append("big Western European economies. The Swiss in recent years ");
        buf.append("have brought their economic practices largely into ");
        buf.append("conformity with the EU's to enhance their international ");
        buf.append("competitiveness. ");

        Paragraph p3 = new Paragraph();
        p3.setAlignment(Align.JUSTIFY);
        textLine = new TextLine(f1, buf.toString());
        p3.add(textLine);

        textLine = new TextLine(f1,
                "Even so, unemployment has remained at less than half the EU average.");
        textLine.setColor(Color.blue);
        p3.add(textLine);

        Paragraph p4 = new Paragraph();
        p4.setAlignment(Align.RIGHT);

        textLine = new TextLine(f1, "Source: The world fact book.");
        textLine.setColor(Color.blue);
        textLine.setURIAction(
                "https://www.cia.gov/library/publications/the-world-factbook/geos/sz.html");
        p4.add(textLine);

        column.addParagraph(p1);
        column.addParagraph(p2);
        column.addParagraph(p3);
        column.addParagraph(p4);
        column.setWidth(265f);

        tableData.get(0).get(0).setImage(image1);
        tableData.get(0).get(1).setDrawable(column);

        table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_78();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_78 => " + (time1 - time0));
    }

}   // End of Example_78.java
