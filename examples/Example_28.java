package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_28.java
 * Example that shows how to use the NotoSansSymbols font.
 */
public class Example_28 {
    public Example_28() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_28.pdf")));

        Font f1 = new Font(pdf, "fonts/NotoSansSymbols/NotoSansSymbols-Regular.ttf.stream");
        f1.setSize(24f);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        float x = 50f;
        float y = 55f;
        float dy = 30f;

        drawLineOfText(page, f1, x, y, 0x0041, 0x005A);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x0061, 0x007A);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x24B6, 0x24CF);
        y += dy;

        drawLineOfText(page, f1, x, y, 0x24D0, 0x24E9);
        y += dy;

        pdf.complete();
    }

    private void drawLineOfText(
            Page page, Font f1, float x, float y, int c1, int c2) throws Exception {
        StringBuilder buf = new StringBuilder();
        for (int i = c1; i <= c2; i++) {
            buf.append((char) i);
        }
        TextLine text = new TextLine(f1);
        text.setText(buf.toString());
        text.setLocation(x, y);
        text.drawOn(page);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_28();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_28", time0, time1);
    }
}   // End of Example_28.java
