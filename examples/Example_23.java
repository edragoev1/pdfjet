package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_23.java
 *
 */
public class Example_23 {

    public Example_23() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_23.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f3.setSize(7f * 0.583f);

        Image image1 = new Image(
                pdf,
                getClass().getResourceAsStream("../images/mt-map.png"),
                ImageType.PNG);
        image1.scaleBy(0.75f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = new ArrayList<Cell>();
        Cell cell = new Cell(f1, "Hello");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);

        cell = new Cell(f1, "World");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);

        cell = new Cell(f1, "Next Column");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);

        cell = new Cell(f1, "CompositeTextLine");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);

        row.add(cell);

        tableData.add(row);

        row = new ArrayList<Cell>();
        cell = new Cell(f2, "This is a test:");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);
        cell = new Cell(f2,
                "Here we are going to test the wrapAroundCellText method.\n\nWe will create a table and place it near the bottom of the page. When we draw this table the text will wrap around the column edge and stay within the column.\n\nSo - let's  see how this is working?");
        cell.setColSpan(2);
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);
        row.add(new Cell(f2));  // We need an empty cell here because the previous cell had colSpan == 2
        cell = new Cell(f2, "Test 456");
        cell.setTopPadding(5f);
        cell.setBottomPadding(5f);
        row.add(cell);
        tableData.add(row);

        row = new ArrayList<Cell>();
        row.add(new Cell(f2,
                "Another row.\n\n\nMake sure that this line of text will be wrapped around correctly too."));
        row.add(new Cell(f2, "Yahoo!"));
        row.add(new Cell(f2, "Test 789"));

        CompositeTextLine composite = new CompositeTextLine(0f, 0f);
        composite.setFontSize(12f);
        TextLine line1 = new TextLine(f2, "Composite Text Line");
        TextLine line2 = new TextLine(f3, "Superscript");
        TextLine line3 = new TextLine(f3, "Subscript");
        line2.setTextEffect(Effect.SUPERSCRIPT);
        line3.setTextEffect(Effect.SUBSCRIPT);
        composite.addComponent(line1);
        composite.addComponent(line2);
        composite.addComponent(line3);

        cell = new Cell(f2);
        cell.setCompositeTextLine(composite);
        cell.setBgColor(Color.peachpuff);
        row.add(cell);

        tableData.add(row);

        Table table = new Table();
        table.setData(tableData, Table.DATA_HAS_1_HEADER_ROWS);
        table.setLocation(50f, 50f);
        table.setColumnWidth(0, 100f);
        table.setColumnWidth(1, 100f);
        table.setColumnWidth(2, 100f);
        table.setColumnWidth(3, 150f);
        table.wrapAroundCellText();

        int numOfPages = table.getNumberOfPages(page);
        while (true) {
            table.drawOn(page);
            if (!table.hasMoreData()) break;
            page = new Page(pdf, Letter.PORTRAIT);
            table.setLocation(50f, 50f);
        }


        tableData = new ArrayList<List<Cell>>();

        row = new ArrayList<Cell>();
        row.add(new Cell(f1));
        row.add(new Cell(f2));
        tableData.add(row);

        row = new ArrayList<Cell>();
        row.add(new Cell(f1, "Hello, World!"));
        row.add(new Cell(f2, "This is a test."));
        tableData.add(row);

        tableData.get(0).get(0).setImage(image1);

        table = new Table();
        table.setData(tableData);
        table.setLocation(50f, 450f);
        table.setColumnWidth(0, 260f);
        table.setColumnWidth(1, 260f);

        StringBuilder buf = new StringBuilder();
        buf.append("Name: 20200306_050741\n");
        buf.append("Recorded: 2018:09:28 18:28:43\n");

        TextBox textBox = new TextBox(f1, buf.toString());
        textBox.setWidth(400f);
        textBox.setNoBorders();

        tableData.get(0).get(1).setDrawable(textBox);

        table.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_23();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_23 => " + (t1 - t0));
    }

}   // End of Example_23.java
