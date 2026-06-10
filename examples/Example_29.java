package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_29.java
 */
public class Example_29 {
    public Example_29() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_29.pdf")));

        Font font = new Font(pdf, IBMPlexSans.Regular);
        font.setSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Paragraph paragraph1 = new Paragraph();
        paragraph1.add(new TextLine(font, Content.ofTextFile("data/languages/english.txt")));

        Paragraph paragraph2 = new Paragraph();
        paragraph2.add(new TextLine(font, Content.ofTextFile("data/languages/greek.txt")));

        TextColumn column = new TextColumn();
        column.setLocation(50f, 50f);
        column.setWidth(400f);
        column.addParagraph(paragraph1);
        column.addParagraph(paragraph2);
        // column.drawOn(page);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(font, "Hello"));
        row.add(new Cell(font, "World"));
        row.get(1).setTextColumn(column);
        tableData.add(row);

        Table table = new Table(font, font);
        table.setData(tableData);
        table.setLocation(50f, 50f);
        table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_29();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_29", time0, time1);
    }
}   // End of Example_29.java
