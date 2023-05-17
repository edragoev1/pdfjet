using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_38.cs
 */
public class Example_38 {
    private Font font;
    public Example_38() {
        BufferedStream bos = new BufferedStream(
                new FileStream("Example_38.pdf", FileMode.Create));

        PDF pdf = new PDF(bos);
        font = new Font(pdf, CoreFont.COURIER);
        Page page = new Page(pdf, Letter.LANDSCAPE);

        Table table = new Table();
        table.SetData(CreateTableData());
        table.SetBottomMargin(10f);
        table.SetLocation(50f, 50f);
        table.DrawOn(page);

        pdf.Complete();
    }
    /**
     * This will return a 10x10 matrix. The HTML-Like table will be like:
     * <table border="solid">
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2">2x1</td>
     * </tr>
     * <tr>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td>1x1</td>
     * <td colspan="5">5x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td colspan="3">3x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td rowspan="2">1x2</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="4" rowspan="4">4x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * <td rowspan="3">1x3</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td rowspan="4">1x4</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td colspan="2">2x1</td>
     * <td colspan="2" rowspan="2">2x2</td>
     * <td rowspan="2">1x2</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * <tr>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * <td>1x1</td>
     * </tr>
     * </table>
     *
     * @return
     */
    private List<List<Cell>> CreateTableData() {
        List<List<Cell>> rows = new List<List<Cell>>();
        for (int i = 0; i < 10; i++) {
            List<Cell> row = new List<Cell>();
            switch (i) {
            case 0:
                row.Add(GetCell(font, 2, "2x2", true, false));
                row.Add(GetCell(font, 1,    "", true, false));
                row.Add(GetCell(font, 2, "2x1", true, true));
                row.Add(GetCell(font, 1,    "", true, false));
                row.Add(GetCell(font, 2, "2x1", true, true));
                row.Add(GetCell(font, 1,    "", true, false));
                row.Add(GetCell(font, 2, "2x1", true, true));
                row.Add(GetCell(font, 1,    "", true, false));
                row.Add(GetCell(font, 2, "2x1", true, true));
                row.Add(GetCell(font, 1,    "", true, false));
                break;
            case 1:
                row.Add(GetCell(font, 2,   "^", false, true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 2, "2x2", true,  false));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 5, "5x1", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                break;
            case 2:
                row.Add(GetCell(font, 1, "1x2", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 2,   "^", false, true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 2, "2x2", true,  false));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 3, "3x1", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                break;
            case 3:
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1, "1x3", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 2,   "^", false, true));
                row.Add(GetCell(font, 1,    "", true,  false));
                row.Add(GetCell(font, 1, "1x1", true, true));
                row.Add(GetCell(font, 2, "2x1", true,  true));
                row.Add(GetCell(font, 1,    "", true,  false));
                row.Add(GetCell(font, 1, "1x2", true,  false));
                break;
            case 4:
                row.Add(GetCell(font, 1, "1x2", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1,   "^", false, false));
                row.Add(GetCell(font, 2, "2x1", true,  true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 4, "4x4", true,  false));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,   "^", false, true));
                break;
            case 5:
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x3", true,  false));
                row.Add(GetCell(font, 1, "1x3", true,  false));
                row.Add(GetCell(font, 4,   "^", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1, "1x3", true,  false));
                break;
            case 6:
                row.Add(GetCell(font, 1, "1x2", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1, "1x4", true,  false));
                row.Add(GetCell(font, 1,   "^", false, false));
                row.Add(GetCell(font, 1,   "^", false, false));
                row.Add(GetCell(font, 4,   "^", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1,    "", false, false));
                row.Add(GetCell(font, 1,   "^", false, false));
                break;
            case 7:
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1,   "^", false, false));
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 4,   "^", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,   "^", false, true));
                break;
            case 8:
                row.Add(GetCell(font, 1, "1x2", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1,   "^", false, false));
                row.Add(GetCell(font, 2, "2x1", true,  true));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 2, "2x2", true,  false));
                row.Add(GetCell(font, 1,    "", true,  true));
                row.Add(GetCell(font, 1, "1x2", true,  false));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                break;
            case 9:
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 1, "1x1", true,  true));
                row.Add(GetCell(font, 2,   "^", false, true));
                row.Add(GetCell(font, 1,    "", false, true));
                row.Add(GetCell(font, 1,   "^", false, true));
                row.Add(GetCell(font, 1, "1x1", true, true));
                row.Add(GetCell(font, 1, "1x1", true, true));
                break;
            }
            rows.Add(row);
        }
        return rows;
    }

    private Cell GetCell(
            Font font,
            int colSpan,
            String text,
            bool topBorder,
            bool bottomBorder) {
        Cell cell = new Cell(font);
        cell.SetColSpan(colSpan);
        cell.SetWidth(50f);
        cell.SetText(text);
        cell.SetBorder(Border.TOP, topBorder);
        cell.SetBorder(Border.BOTTOM, bottomBorder);
        cell.SetTextAlignment(Align.CENTER);
        cell.SetBgColor(Color.lightblue);
        cell.SetLineWidth(0.5f);
        return cell;
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_38();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_38", time0, time1);
    }
}   // End of Example_38.cs
