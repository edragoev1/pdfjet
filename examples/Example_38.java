package examples;

import java.io.BufferedOutputStream;
import java.io.FileOutputStream;
import java.util.LinkedList;
import java.util.List;
import com.pdfjet.*;

public class Example_38 {
    private Font font;

    public Example_38() throws Exception {
        BufferedOutputStream bos =
                new BufferedOutputStream(new FileOutputStream("Example_38.pdf"));

        PDF pdf = new PDF(bos);
        font = new Font(pdf, CoreFont.COURIER);
        Page page = new Page(pdf, Letter.LANDSCAPE);

        Table table = new Table();
        table.setData(createTableData());
        table.setBottomMargin(10f);
        table.setLocation(50f, 50f);
        // table.mergeOverlaidBorders();
        table.drawOn(page);

        pdf.complete();
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
     * @throws Exception
     */
    private List<List<Cell>> createTableData() throws Exception {
        List<List<Cell>> rows = new LinkedList<List<Cell>>();
        for (int i = 0; i < 10; i++) {
            List<Cell> row = new LinkedList<Cell>();
            switch (i) {
            case 0:
                row.add(getCell(font, 2, "2x2", true, false));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                row.add(getCell(font, 2, "2x1", true, true));
                row.add(getCell(font, 1,    "", true, false));
                break;
            case 1:
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 5, "5x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                break;
            case 2:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 3, "3x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                break;
            case 3:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", true,  false));
                row.add(getCell(font, 1, "1x2", true,  false));
                break;
            case 4:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 4, "4x4", true,  false));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                break;
            case 5:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 1, "1x3", true,  false));
                row.add(getCell(font, 4,   "^", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1, "1x3", true,  false));
                break;
            case 6:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x4", true,  false));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 4,   "^", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,    "", false, false));
                row.add(getCell(font, 1,   "^", false, false));
                break;
            case 7:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 4,   "^", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                break;
            case 8:
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, false));
                row.add(getCell(font, 2, "2x1", true,  true));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 2, "2x2", true,  false));
                row.add(getCell(font, 1,    "", true,  true));
                row.add(getCell(font, 1, "1x2", true,  false));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                break;
            case 9:
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 1, "1x1", true,  true));
                row.add(getCell(font, 2,   "^", false, true));
                row.add(getCell(font, 1,    "", false, true));
                row.add(getCell(font, 1,   "^", false, true));
                row.add(getCell(font, 1, "1x1", true, true));
                row.add(getCell(font, 1, "1x1", true, true));
                break;
            }
            rows.add(row);
        }

        return rows;
    }

    private Cell getCell(
            Font font,
            int colSpan,
            String text,
            boolean topBorder,
            boolean bottomBorder) throws Exception {
        Cell cell = new Cell(font);
        cell.setColSpan(colSpan);
        cell.setWidth(50f);
        cell.setText(text);
        cell.setBorder(Border.TOP, topBorder);
        cell.setBorder(Border.BOTTOM, bottomBorder);
        cell.setTextAlignment(Align.CENTER);
        cell.setBgColor(Color.lightblue);
        cell.setLineWidth(0.5f);
        return cell;
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_38();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_38", time0, time1);
    }
}   // End of Example_38.java
