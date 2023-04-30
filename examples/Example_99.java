package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_99.java
 */
public class Example_99 {
    public Example_99() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_99.pdf")));
        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        // Font f1 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Semibold.otf.stream");
        // Font f2 = new Font(pdf, "fonts/SourceSansPro/SourceSansPro-Regular.otf.stream");
        // Font f1 = new Font(pdf, "fonts/Andika/Andika-Bold.ttf.stream");
        // Font f2 = new Font(pdf, "fonts/Andika/Andika-Regular.ttf.stream");
        Font f1 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-SemiBold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream");

        // f1.setSize(9f);
        // f2.setSize(9f);
        f1.setSize(7f);
        f2.setSize(7f);

        int L = 0;
        int R = 1;
        int[] widths = {15, 15, 18, 7, 12, 12, 15, 15, 25};
        int[] align = {  L,  L,  L, L,  R,  R,  L,  L,  L};
   
        BigTable table = new BigTable(pdf, f1, f2, Letter.PORTRAIT);
        table.setLocation(20f, 15f);
        table.setBottomMargin(15f);
        table.setColumnWidths(80, 80, 35, 60, 60, 75, 110, 90);
        table.setTextAlignment(L,  L,  L,  R,  R,  L,   L,  L);
        table.setColumnSpacing(2f);
        table.setDrawVerticalLines(false);
        // table.setHeaderRowColor(Color.darkolivegreen);

        BufferedReader br = new BufferedReader(
                new FileReader("../datasets/Electric_Vehicle_Population_Data.csv"));
        String line = null;
        while ((line = br.readLine()) != null) {
            String[] fields = line.split(",");

            // String textLine = table.getTextLine(fields, widths, align);
            // table.add(textLine);
            // if (textLine.contains("FORD")) {
            //     table.drawRow(Color.blue);
            // } else if (textLine.contains("VOLKSWAGEN")) {
            //     table.drawRow(Color.red);
            // } else {
            //     table.drawRow(Color.black);
            // }

            table.add(fields[0]);
            table.add(fields[2]);
            table.add(fields[3]);
            table.add(fields[4]);
            table.add(fields[5]);
            table.add(fields[6]);
            table.add(fields[7]);
            table.add(fields[8].charAt(0) == 'B' ? "BEV" : fields[8].charAt(0) == 'P' ? "PHEV" : fields[8]);
            if (fields[6].equals("FORD")) {
                table.drawRow(Color.blue);
            } else if (fields[6].equals("VOLKSWAGEN")) {
                table.drawRow(Color.red);
            } else {
                table.drawRow(Color.black);
            }
        }
        br.close();

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
        new Example_99();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_99", time0, time1);
    }
}
