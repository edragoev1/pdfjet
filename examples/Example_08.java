package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_08.java
 */
public class Example_08 {
    public Example_08() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_08.pdf")));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        // Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);

        Font f1 = new Font(pdf, "fonts/NotoSans/NotoSans-SemiBold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/NotoSans/NotoSans-SemiBoldItalic.ttf.stream");

        f1.setSize(7f);
        f2.setSize(7f);
        f3.setSize(7f);

        Image image = new Image(pdf, "images/TeslaX.png");
        image.scaleBy(0.20f);

        Barcode barcode = new Barcode(Barcode.CODE_128, "Hello, World!");
        barcode.setModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the barcode.
        barcode.setFont(f1);

        Table table = new Table(f1, f2, "data/Electric_Vehicle_Population_550.csv");
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

        table.setLocationFirstPage(50f, 100f);
        table.setLocation(50f, 0f);
        table.setBottomMargin(15f);
        table.setTextColorInRow(12, Color.blue);
        table.setTextColorInRow(13, Color.red);
        table.setFontInRow(14, f3);
        // table.getCellAt(13, 0).getTextBox().setURIAction("http://pdfjet.com");

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
