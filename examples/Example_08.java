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

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-BoldItalic.ttf.stream");

        f1.setSize(7f);
        f2.setSize(7f);
        f3.setSize(7f);

        Image image = new Image(pdf, "images/fruit.jpg");
        image.scaleBy(0.20f);

        BarCode barCode = new BarCode(BarCode.CODE128, "Hello, World!");
        barCode.setModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the barcode.
        // barCode.setFont(f1);
        Table table = new Table();
        List<List<Cell>> tableData = getData(
        		"data/world-communications.txt",
                "|",
                Table.DATA_HAS_2_HEADER_ROWS,
                f1,
                f2,
                image,
                barCode);
        table.setData(tableData, Table.DATA_HAS_2_HEADER_ROWS);
        table.removeLineBetweenRows(0, 1);
        table.setLocation(100f, 0f);
        table.setRightMargin(20f);
        table.setBottomMargin(10f);
        table.setCellBordersWidth(0f);
        table.setTextColorInRow(12, Color.blue);
        table.setTextColorInRow(13, Color.red);
        table.getCellAt(13, 0).getTextBox().setURIAction("http://pdfjet.com");
        table.setFontInRow(14, f3);
        table.getCellAt(21, 0).setColSpan(6);
        table.getCellAt(21, 6).setColSpan(2);

        // Set the column widths manually:
        // float[] widths = {70f, 50f, 70f, 70f, 70f, 70f, 50f, 50f};
        // for (int i = 0; i < widths.length; i++) {
        //     table.setColumnWidth(i, widths[i]);            
        // }

        // Auto adjust the column widths to be just wide enough to fit the text without truncation.
        // Columns with colspan > 1 will not be adjusted.
        // table.autoAdjustColumnWidths();

        // Auto adjust the column widths in a way that allows the table to fit perfectly on the page.
        // Columns with colspan > 1 will not be adjusted.
        table.fitToPage(Letter.PORTRAIT);

        List<Page> pages = new ArrayList<Page>();
        table.drawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.size(); i++) {
            Page page = pages.get(i);
            page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
            pdf.addPage(page);
        }
        pdf.complete();
    }

    public List<List<String>> getTextData(String fileName, String delimiter) throws Exception {
        List<List<String>> tableTextData = new ArrayList<List<String>>();
        BufferedReader reader = new BufferedReader(new FileReader(fileName));
        String line;
        while ((line = reader.readLine()) != null) {
            String[] cols;
            if (delimiter.equals("|")) {
                cols = line.split("\\|", -1);
            } else if (delimiter.equals("\t")) {
                cols = line.split("\t", -1);
            } else {
                throw new Exception("Only pipes and tabs can be used as delimiters");
            }
            tableTextData.add(Arrays.asList(cols));
        }
        reader.close();
        return tableTextData;
    }

    public List<List<Cell>> getData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2,
            Image image,
            BarCode barCode) throws Exception {
        List<List<Cell>> tableData = new ArrayList<List<Cell>>();
        List<List<String>> tableTextData = getTextData(fileName, delimiter);
        int currentRow = 0;
        for (List<String> rowData : tableTextData) {        	
        	List<Cell> row = new ArrayList<Cell>();
            for (int i = 0; i < rowData.size(); i++) {
            	String text = rowData.get(i).trim();
                Cell cell;
                if (currentRow < numOfHeaderRows) {
                    cell = new Cell(f1, text);
                } else {
                    cell = new Cell(f2);
                    if (i == 0 && currentRow == 5) {
                        cell.setImage(image);
                    }
                    if (i == 0 && currentRow == 6) {
                        cell.setBarcode(barCode);
                        cell.setTextAlignment(Align.CENTER);
                        cell.setColSpan(8);
                    } else {
                        TextBox textBox = new TextBox(f2, text);
                        textBox.setTextAlignment(i == 0 ? Align.LEFT : Align.RIGHT);
                        cell.setTextBox(textBox);
                    }
                }
                row.add(cell);
            }
            tableData.add(row);
            currentRow++;
        }
        return tableData;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_08();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_08", time0, time1);
    }
}   // End of Example_08.java
