package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_23.java
 */
public class Example_23 {
    public Example_23() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_23.pdf")), Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");

        // What is this?
        f3.setSize(7f * 0.583f);

        Image image1 = new Image(pdf, "images/mt-map.png");
        image1.scaleBy(0.75f);

        List<List<Cell>> tableData = new ArrayList<List<Cell>>();

        List<Cell> row = new ArrayList<Cell>();
        row.add(new Cell(f1, "Hello"));
        row.add(new Cell(f1, "World"));
        row.add(new Cell(f1, "Next Column"));
        row.add(new Cell(f1, "CompositeTextLine"));
        tableData.add(row);

        row = new ArrayList<Cell>();
        row.add(new Cell(f2, "This is a test:"));
        TextBox textBox = new TextBox(f2,
                "Here we are going to test the wrapAroundCellTextmethod.\n\nWe will create a table and place it near the bottom of the page. When we draw this table the text will wrap around the column edge and stay within the column.\n\nSo - let's  see how this is working?");
        textBox.setTextAlignment(Align.RIGHT);
        Cell cell = new Cell(f2, "Yahoo! AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA Hello World!");
        cell.setBgColor(Color.aliceblue);
        // cell.setTextBox(textBox);
        cell.setColSpan(2);
        row.add(cell);
        row.add(new Cell(f2));  // We need an empty cell here because the previous cell had colSpan == 2
        row.add(new Cell(f2, "Test 456"));
        tableData.add(row);
/*
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
*/
        Table table = new Table();
        table.setData(tableData, Table.DATA_HAS_1_HEADER_ROWS);
        table.setLocation(50f, 50f);
        table.setFirstPageTopMargin(650f);
        table.setBottomMargin(15f);
        table.setColumnWidth(0, 100f);
        table.setColumnWidth(1, 100f);
        table.setColumnWidth(2, 100f);
        table.setColumnWidth(3, 150f);

        List<Page> pages = new ArrayList<Page>();
        Page page = null;
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            page = pages.get(i);
            pdf.addPage(page);
        }

        // tableData = new ArrayList<List<Cell>>();
        // row = new ArrayList<Cell>();
        // row.add(new Cell(f1));
        // row.add(new Cell(f2));
        // tableData.add(row);

        // row = new ArrayList<Cell>();
        // row.add(new Cell(f1, "Hello, World!"));
        // row.add(new Cell(f2, "This is a test."));
        // tableData.add(row);

        // tableData.get(0).get(0).setImage(image1);

        // table = new Table();
        // table.setData(tableData);
        // table.setLocation(50f, 350f);
        // table.setColumnWidth(0, 260f);
        // table.setColumnWidth(1, 260f);
        // table.setRightBorderOnLastColumn();
        // table.setBottomBorderOnLastRow();

        // StringBuilder buf = new StringBuilder();
        // buf.append("Name: 20200306_050741\n");
        // buf.append("Recorded: 2018:09:28 18:28:43\n");

        // textBox = new TextBox(f1, buf.toString());
        // textBox.setWidth(400f);
        // textBox.setBorder(Border.NONE);
        // tableData.get(0).get(1).setTextBox(textBox);
        // table.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_23();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_23", time0, time1);
    }
}   // End of Example_23.java
