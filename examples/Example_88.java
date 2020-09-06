package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_88.java
 *
 */
public class Example_88 {
    private Image image1;
    private BarCode barCode;

    public Example_88() throws Exception {

        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_88.pdf")), Compliance.PDF_A_1B);

        Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        f1.setSize(7f);

        Font f2 = new Font(pdf, CoreFont.HELVETICA);
        f2.setSize(7f);

        Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);
        f3.setSize(7f);

        image1 = new Image(
                pdf,
                getClass().getResourceAsStream("../images/fruit.jpg"),
                ImageType.JPG);
        image1.scaleBy(0.20f);

        // barCode = new BarCode(BarCode.CODE128, "H\u0228\u0246\u0252llo, World!");
        barCode = new BarCode(BarCode.CODE128, "H\u0228\u0246\u0252llo, World!");
        barCode.setModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the barcode.
        // barCode.setFont(f1);
        
        Table table = new Table();
        List<List<Cell>> tableData = getData(
        		"data/world-communications.txt", "|", Table.DATA_HAS_2_HEADER_ROWS, f1, f2);
        table.setData(tableData, Table.DATA_HAS_1_HEADER_ROWS);
        table.removeLineBetweenRows(0, 1);
        table.setLocation(100f, 0f);
        table.setRightMargin(20f);
        table.setBottomMargin(0f);
        table.setCellBordersWidth(0f);
        table.setTextColorInRow(12, Color.blue);
        table.setTextColorInRow(13, Color.red);
        table.getCellAt(13, 0).getTextBlock().setURIAction("http://pdfjet.com");
        table.setFontInRow(14, f3);
        table.getCellAt(21, 0).setColSpan(5);
        table.getCellAt(21, 6).setColSpan(2);

        // Set the column widths manually:
        // table.setColumnWidth(0, 70f);
        // table.setColumnWidth(1, 50f);
        // table.setColumnWidth(2, 70f);
        // table.setColumnWidth(3, 70f);
        // table.setColumnWidth(4, 70f);
        // table.setColumnWidth(5, 70f);
        // table.setColumnWidth(6, 50f);
        // table.setColumnWidth(7, 50f);

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
            // page.addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()));
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
    
    private List<Cell> getHeaderCells(Font font) {
    	List<Cell> headerCells = new ArrayList<Cell>();
    	headerCells.add(new Cell(font, "Column 1"));
    	headerCells.add(new Cell(font, "Column 2"));
    	headerCells.add(new Cell(font, "Column 3"));
    	headerCells.add(new Cell(font, "Column 4"));
    	headerCells.add(new Cell(font, "Column 5"));
    	return headerCells;
    }
    
    private List<List<Cell>> getDataCells(Font font) {
    	List<List<Cell>> rows = new ArrayList<List<Cell>>();
    	
    	List<Cell> row1 = new ArrayList<Cell>();
    	Cell firstCell = new Cell(font, "Column 1 with colSpan = 3");
    	firstCell.setColSpan(3);
    	row1.add(firstCell);
    	row1.add(new Cell(font, ""));
    	row1.add(new Cell(font, ""));
    	Cell secondCell = new Cell(font, "Column 4 with colSpan = 2");
    	secondCell.setColSpan(2);
    	row1.add(secondCell);
    	row1.add(new Cell(font, ""));
    	rows.add(row1);
    	
    	return rows;
    }

    public List<List<Cell>> getData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2) throws Exception {
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
                }
                else {
                    cell = new Cell(f2);
                    if (i == 0 && currentRow == 5) {
                        cell.setImage(image1);
                    }
                    if (i == 0 && currentRow == 6) {
                        cell.setBarcode(barCode);
                        cell.setTextAlignment(Align.CENTER);
                        cell.setColSpan(8);
                    }
                    else {
                        TextBlock textBlock = new TextBlock(f2, text);
                        if (i == 0) {
                            textBlock.setTextAlignment(Align.LEFT);
                        }
                        else {
                            textBlock.setTextAlignment(Align.RIGHT);
                        }
                        cell.setTextBlock(textBlock);
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
        new Example_88();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_88 => " + (time1 - time0));
    }

}   // End of Example_88.java
