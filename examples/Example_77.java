package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_77.java
 *
 */
public class Example_77 {

    public Example_77() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_77.pdf")), Compliance.PDF_A_1B);

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
        image1.scaleBy(0.5f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(f1));
        row.add(new Cell(f2));
        tableData.add(row);

        row = new ArrayList<Cell>();
        row.add(new Cell(f1, "Hello, World!"));
        row.add(new Cell(f2));
        tableData.add(row);

        tableData.get(0).get(0).setImage(image1);

        Table table = new Table();
        table.setData(tableData);
        table.setLocation(50f, 50f);
        table.setColumnWidth(0, 270f);
        table.setColumnWidth(1, 270f);

        StringBuilder buf = new StringBuilder();
        buf.append("Name: 20200306_050741\n");
        buf.append("Recorded: 2018:09:28 18:28:43\n\n");
        buf.append("Lorem ipsum dolor sit amet, consectetur adipiscing elit. Nulla elementum interdum elit, quis vehicula urna interdum quis. Phasellus gravida ligula quam, nec blandit nulla. Sed posuere, lorem eget feugiat placerat, ipsum nulla euismod nisi, in semper mi nibh sed elit. Mauris libero est, sodales dignissim congue sed, pulvinar non ipsum. Sed risus nisi, ultrices nec eleifend at, viverra sed neque. Integer vehicula massa non arcu viverra ullamcorper. Ut id tellus id ante mattis commodo. Donec dignissim aliquam tortor, eu pharetra ipsum ullamcorper in. Vivamus ultrices imperdiet iaculis.\n");


        TextBox textBox = new TextBox(f1, buf.toString());
        textBox.setWidth(265f);
        textBox.setNoBorders();

        // tableData.get(0).get(1).setTextBox(textBox);
        tableData.get(0).get(1).setDrawable(textBox);


        List<Paragraph> paragraphs = new ArrayList<Paragraph>();
        Paragraph p1 = new Paragraph();
        TextLine tl1 = new TextLine(f2,
"The Swiss Confederation was founded in 1291 as a defensive alliance among three cantons. In succeeding years, other localities joined the original three. The Swiss Confederation secured its independence from the Holy Roman Empire in 1499. Switzerland's sovereignty and neutrality have long been honored by the major European powers, and the country was not involved in either of the two World Wars. The political and economic integration of Europe over the past half century, as well as Switzerland's role in many UN and international organizations, has strengthened Switzerland's ties with its neighbors. However, the country did not officially become a UN member until 2002.");
        p1.add(tl1);

        Paragraph p2 = new Paragraph();
        TextLine tl2 = new TextLine(f2,
"Even so, unemployment has remained at less than half the EU average.");
        p2.add(tl2);

        paragraphs.add(p1);
        paragraphs.add(p2);

        Text text = new Text(paragraphs);
        text.setWidth(265f);
        // float[] point = text.drawOn(null);

        tableData.get(1).get(1).setDrawable(text);

        table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_77();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_77 => " + (time1 - time0));
    }

}   // End of Example_77.java
