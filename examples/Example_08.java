package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.barcodes.*;

/**
 * Example_08.java
 */
public class Example_08 {
    public Example_08() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_08.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.SemiBold);
        f1.setSize(7.0);

        Font f2 = new Font(pdf, IBMPlexSans.Regular);
        f2.setSize(7.0);

        Font f3 = new Font(pdf, IBMPlexSans.BoldItalic);
        f3.setSize(7.0);

        Image image = new Image(pdf, "images/TeslaX.png");
        image.scaleBy(0.20f);

        Barcode barcode = new Barcode(Barcode.CODE_128, "Hello, World!");
        barcode.setModuleLength(0.75f);
	    // Comment out the line below if you don't want to print the text underneath the barcode.
        barcode.setFont(f1);

        Table table = new Table(f1, f2, "data/Electric_Vehicle_Population_10_Pages.csv");
        table.setVisibleColumns(1, 2, 3, 4, 5, 6, 7, 9);
        table.getCellAt(4, 0).setImage(image);
        table.getCellAt(5, 0).setColSpan(8);
        table.getCellAt(5, 0).setBarcode(barcode);
        table.getCellAt(20, 0).setColSpan(6);
        table.getCellAt(20, 6).setColSpan(2);
        table.setColumnWidths();
        table.setColumnWidth(0, image.getWidth() + 4f);
        table.setColumnWidth(3, table.getColumnWidth(3) + 10f);
        table.setColumnWidth(5, table.getColumnWidth(5) + 10f);
        table.rightAlignNumbers();

        table.setLocation(30f, 30f);
        // table.setFirstPageTopMargin(150f);
        table.setBottomMargin(15f);
        table.setTextColorInRow(12, Color.blue);
        table.setTextColorInRow(13, Color.red);
        table.setFontInRow(14, f3);

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_08();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_08", time0, time1);
    }
} // End of Example_08.java
