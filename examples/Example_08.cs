using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_08.cs
 *
 */
public class Example_08 {
    private Image image1;
    private BarCode barCode;

    public Example_08() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_08.pdf", FileMode.Create)));

        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        // Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Bold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-BoldItalic.ttf.stream");

        f1.SetSize(7f);
        f2.SetSize(7f);
        f3.SetSize(7f);

        image1 = new Image(pdf, "images/fruit.jpg");
        image1.ScaleBy(0.20f);

        barCode = new BarCode(BarCode.CODE128, "Hello, World!");
        barCode.SetModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the barcode.
        // barCode.SetFont(f1);

        Table table = new Table();
        List<List<Cell>> tableData = GetData(
        		"data/world-communications.txt", "|", Table.DATA_HAS_2_HEADER_ROWS, f1, f2);
        table.SetData(tableData, Table.DATA_HAS_2_HEADER_ROWS);
        table.RemoveLineBetweenRows(0, 1);
        table.SetLocation(100f, 0f);
        table.SetRightMargin(20f);
        table.SetBottomMargin(10f);
        table.SetCellBordersWidth(0f);
        table.SetTextColorInRow(12, Color.blue);
        table.SetTextColorInRow(13, Color.red);
        table.GetCellAt(13, 0).GetTextBlock().SetURIAction("http://pdfjet.com");
        table.SetFontInRow(14, f3);
        table.GetCellAt(21, 0).SetColSpan(6);
        table.GetCellAt(21, 6).SetColSpan(2);

        // Set the column widths manually:
        // table.SetColumnWidth(0, 70f);
        // table.SetColumnWidth(1, 50f);
        // table.SetColumnWidth(2, 70f);
        // table.SetColumnWidth(3, 70f);
        // table.SetColumnWidth(4, 70f);
        // table.SetColumnWidth(5, 70f);
        // table.SetColumnWidth(6, 50f);
        // table.SetColumnWidth(7, 50f);

        // Auto adjust the column widths to be just wide enough to fit the text without truncation.
        // Columns with colspan > 1 will not be adjusted.
        // table.AutoAdjustColumnWidths();

        // Auto adjust the column widths in a way that allows the table to fit perfectly on the page.
        // Columns with colspan > 1 will not be adjusted.
        table.FitToPage(Letter.PORTRAIT);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            // page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
            pdf.AddPage(page);
        }

        pdf.Complete();
    }

    public List<List<String>> GetTextData(String fileName, String delimiter) {
        List<List<String>> tableTextData = new List<List<String>>();

        StreamReader reader = new StreamReader(fileName);
        String line = null;
        while ((line = reader.ReadLine()) != null) {
            String[] cols = null;
            if (delimiter.Equals("|")) {
                cols = line.Split(new Char[] {'|'});
            }
            else if (delimiter.Equals("\t")) {
                cols = line.Split(new Char[] {'\t'});
            }
            else {
                throw new Exception("Only pipes and tabs can be used as delimiters");
            }
            tableTextData.Add(new List<String>(cols));
        }
        reader.Close();

        return tableTextData;
    }

    public List<List<Cell>> GetData(
            String fileName,
            String delimiter,
            int numOfHeaderRows,
            Font f1,
            Font f2) {
        List<List<Cell>> tableData = new List<List<Cell>>();

        List<List<String>> tableTextData = GetTextData(fileName, delimiter);
        int currentRow = 0;
        foreach (List<String> rowData in tableTextData) {        	
        	List<Cell> row = new List<Cell>();
            for (int i = 0; i < rowData.Count; i++) {
            	String text = rowData[i].Trim();
                Cell cell;
                if (currentRow < numOfHeaderRows) {
                    cell = new Cell(f1, text);
                } else {
                    cell = new Cell(f2);
                    if (i == 0 && currentRow == 5) {
                        cell.SetImage(image1);
                    }
                    if (i == 0 && currentRow == 6) {
                        cell.SetBarcode(barCode);
                        cell.SetTextAlignment(Align.CENTER);
                        cell.SetColSpan(8);
                    } else {
                        TextBlock textBlock = new TextBlock(f2, text);
                        if (i == 0) {
                            textBlock.SetTextAlignment(Align.LEFT);
                        } else {
                            textBlock.SetTextAlignment(Align.RIGHT);
                        }
                        cell.SetTextBlock(textBlock);
                    }
                }
                row.Add(cell);
            }
            tableData.Add(row);
            currentRow++;
        }

        return tableData;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_08();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_08", time0, time1);
    }
}   // End of Example_08.cs
