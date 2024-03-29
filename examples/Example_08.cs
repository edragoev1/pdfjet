using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_08.cs
 */
public class Example_08 {
    private Image image1;
    private Barcode barcode;

    public Example_08() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_08.pdf", FileMode.Create)));
        // Font f1 = new Font(pdf, CoreFont.HELVETICA_BOLD);
        // Font f2 = new Font(pdf, CoreFont.HELVETICA);
        // Font f3 = new Font(pdf, CoreFont.HELVETICA_BOLD_OBLIQUE);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Semibold.ttf.stream");
        Font f2 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f3 = new Font(pdf, "fonts/OpenSans/OpenSans-BoldItalic.ttf.stream");

        f1.SetSize(7f);
        f2.SetSize(7f);
        f3.SetSize(7f);

        image1 = new Image(pdf, "images/fruit.jpg");
        image1.ScaleBy(0.20f);

        barcode = new Barcode(Barcode.CODE128, "Hello, World!");
        barcode.SetModuleLength(0.75f);
        // Uncomment the line below if you want to print the text underneath the barcode.
        // barcode.SetFont(f1);

        Table table = new Table(f1, f2, "data/Electric_Vehicle_Population_1000.csv");
        // List<List<Cell>> tableData = GetData(
        // 		"data/world-communications.txt", "|", Table.WITH_2_HEADER_ROWS, f1, f2);
        // table.SetData(tableData, Table.WITH_2_HEADER_ROWS);
        table.SetColumnWidths();
        table.RemoveLineBetweenRows(0, 1);
        table.SetLocation(100f, 0f);
        table.SetBottomMargin(15f);
        table.SetCellBordersWidth(0f);
        table.SetTextColorInRow(12, Color.blue);
        table.SetTextColorInRow(13, Color.red);
        // TODO:
        // table.GetCellAt(13, 0).GetTextBox().SetURIAction("http://pdfjet.com");
        table.SetFontInRow(14, f3);
        table.GetCellAt(21, 0).SetColSpan(6);
        table.GetCellAt(21, 6).SetColSpan(2);

        List<Page> pages = new List<Page>();
        table.DrawOn(pdf, pages, Letter.PORTRAIT);
        for (int i = 0; i < pages.Count; i++) {
            Page page = pages[i];
            page.AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count));
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
            } else if (delimiter.Equals("\t")) {
                cols = line.Split(new Char[] {'\t'});
            } else {
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
                    } else if (i == 0 && currentRow == 6) {
                        cell.SetBarcode(barcode);
                        cell.SetTextAlignment(Align.CENTER);
                        cell.SetColSpan(8);
                    } else {
                        TextBox textBox = new TextBox(f2, text);
                        textBox.SetTextAlignment((i == 0) ? Align.LEFT : Align.RIGHT);
                        cell.SetTextBox(textBox);
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
