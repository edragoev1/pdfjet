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

        String fileName = "data/Electric_Vehicle_Population_Data.csv";
        // String fileName = "data/Electric_Vehicle_Population_550.csv";

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-SemiBold.ttf.stream");
        f1.setSize(9f);
        Font f2 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf.stream");
        f2.setSize(9f);

        BigTable table = new BigTable(pdf, f1, f2, Letter.LANDSCAPE);
        List<Float> widths = table.getColumnWidths(fileName);
        // Optionally you can fine tune the widths of the columns:
        widths.set(8, 60f); // Override the calculated width
        widths.set(9, 70f); // Override the calculated width
        table.setColumnSpacing(7f);
        table.setLocation(20f, 15f);
        table.setBottomMargin(15f);
        table.setColumnWidths(widths);
        // You can override that auto column alignments if required:
        // final int LEFT = 0;                  // Align Left
        // final int RIGHT = 1;                 // Align Right
        // table.setTextAlignment(1, RIGHT);    // Override the auto alignment
        // table.setTextAlignment(5, LEFT);     // Override the auto alignment
        BufferedReader reader = new BufferedReader(new FileReader(fileName));
        boolean headerRow = true;
        String line = null;
        while ((line = reader.readLine()) != null) {
            String[] fields = line.split(",");
            drawRow(table, fields, headerRow);
            headerRow = false;
        }
        table.complete();
        reader.close();

        List<Page> pages = table.getPages();
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }

        pdf.complete();
    }

    private void drawRow(BigTable table, String[] fields, boolean headerRow) throws Exception {
        List<String> row = new ArrayList<String>();
        for (int i = 0; i < 10; i++) {
            String field = fields[i];
            if (i == 8) {
                if (headerRow) {
                    row.add("Vehicle Type");
                } else {
                    if (field.charAt(0) == 'B') {
                        row.add("BEV");
                    } else if (field.charAt(0) == 'P') {
                        row.add("PHEV");
                    } else {
                        row.add(field);
                    }
                }
            } else if (i == 9) {
                if (headerRow) {
                    row.add("Green Vehicle");
                } else {
                    if (field.charAt(0) == 'C') {
                        row.add("YES");
                    } else if (field.charAt(0) == 'N') {
                        row.add("NO");
                    } else {
                        row.add("UNKNOWN");
                    }
                }
            } else {
                row.add(field);
            }
        }
        if (fields[6].equals("TOYOTA")) {
            table.drawRow(row, Color.red);
        } else if (fields[6].equals("JEEP")) {
            table.drawRow(row, Color.green);
        } else if (fields[6].equals("FORD")) {
            table.drawRow(row, Color.blue);
        } else {
            table.drawRow(row, Color.black);
        }
    }   
    
    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_43();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_43", time0, time1);
    }
}
