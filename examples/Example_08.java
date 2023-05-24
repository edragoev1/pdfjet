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

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Semibold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-BoldItalic.ttf.stream");

        f1.setSize(7f);
        f2.setSize(7f);
        f3.setSize(7f);

        Image image = new Image(pdf, "images/fruit.jpg");
        image.scaleBy(0.20f);

        Barcode barcode = new Barcode(Barcode.CODE128, "Hello, World!");
        barcode.setModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the
        // barcode.
        barcode.setFont(f2);

        // Table table = new Table(f1, f2, "data/world-communications-1.txt");
        Table table = new Table(f1, f2, "data/Electric_Vehicle_Population_1000.csv");
        table.getCellAt(4, 0).setImage(image);
        // table.getCellAt(5, 0).setTextBox(new TextBox(f2, "table.getCellAt(6, 0).getText() Hello, World!"));
        table.getCellAt(5, 0).setColSpan(8);
        table.getCellAt(5, 0).setBarcode(barcode);
        table.setFontInRow(14, f3);
        table.getCellAt(20, 0).setColSpan(6);
        table.getCellAt(20, 6).setColSpan(2);
        table.setColumnWidths();
        table.setColumnWidth(0, image.getWidth() + 4f);
        table.setColumnWidth(3, table.getColumnWidth(3) + 10f);
        table.setColumnWidth(5, table.getColumnWidth(5) + 10f);
        table.rightAlignNumbers();

        table.removeLineBetweenRows(0, 1);
        table.setLocation(100f, 0f);
        table.setFirstPageTopMargin(100f);
        table.setBottomMargin(15f);
        table.setCellBordersWidth(0f);
        table.setTextColorInRow(12, Color.blue);
        table.setTextColorInRow(13, Color.red);
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

    private String getDelimiterRegex(String str) {
        int comma = 0;
        int pipe = 0;
        int tab = 0;
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch == ',') {
                comma++;
            } else if (ch == '|') {
                pipe++;
            } else if (ch == '\t') {
                tab++;
            }
        }
        if (comma >= pipe) {
            if (comma >= tab) {
                return ",";
            }
            return "\t";
        } else {
            if (pipe >= tab) {
                return "\\|";
            }
            return "\t";
        }
    }

    public List<List<Cell>> getTableData(String fileName, char delimiter, Font f1, Font f2) {
        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        BufferedReader reader = null;
        try {
            reader = new BufferedReader(new FileReader(fileName));
            String delimiterRegex = null;
            int lineNumber = 0;
            String line;
            while ((line = reader.readLine()) != null) {
                if (lineNumber == 0) {
                    delimiterRegex = getDelimiterRegex(line);
                }
                List<Cell> row = new ArrayList<Cell>();
                String[] fields = line.split(delimiterRegex);
                for (String field : fields) {
                    if (lineNumber == 0) {
                        Cell cell = new Cell(f1);
                        cell.setTextBox(new TextBox(f1, field));
                        row.add(cell);
                    } else {
                        row.add(new Cell(f2, field));
                    }
                }
                tableData.add(row);
                lineNumber++;
            }
        } catch (Exception e) {
            e.printStackTrace();
        } finally {
            try {
                reader.close();
            } catch (IOException ioe) {
                ioe.printStackTrace();
            }
        }
        return tableData;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_08();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_08", time0, time1);
    }
} // End of Example_08.java
