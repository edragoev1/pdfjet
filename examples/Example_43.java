package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_43.java
 */
public class Example_43 {
    public Example_43() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_43.pdf")));
        pdf.setCompliance(Compliance.PDF_UA);

	    // Used for performance testing. Results in 2000+ pages PDF.
        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_10_Pages.csv";

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream");
        f1.setSize(10f);

        Font f2 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        f2.setSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
	    table.setNumberOfColumns(9);        // The order of the
	    table.setTableData(fileName, ",");  // these statements
        table.setLocation(0f, 0f);          // is
        table.setBottomMargin(20f);         // very
        table.complete();                   // important!

        List<Page> pages = table.getPages();
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_43();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_43", time0, time1);
    }
}
