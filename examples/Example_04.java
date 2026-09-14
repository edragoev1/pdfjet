package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Draws Chinese, Japanese and Korean text with the CJK fonts, and Latin text
 * with Courier. None of these fonts is embedded: the PDF names them and the
 * viewer supplies them.
 * <p>
 * The advantage is size and speed. A CJK font holds tens of thousands of
 * glyphs, and this document carries none of them, so it is a few kilobytes
 * and is written in a moment. The disadvantages: the viewer must have the
 * Adobe Asian font packs, or a substitute, and the text takes the shapes and
 * widths of whatever font it finds, so the document does not look the same
 * everywhere; the core font Courier is limited to the WinAnsi characters; and
 * a document with a font that is not embedded cannot claim PDF/A or PDF/UA
 * compliance. To ship the glyphs with the document, use an embedded font like
 * IBM Plex Sans JP, KR, SC or TC, as Example_02 and 19 do.
 * </p>
 *
 * @see Font
 * @see CJKFont
 */
public class Example_04 {
    public Example_04() throws Exception {
        // Create a new PDF document
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_04.pdf")));

        Font f0 = new Font(pdf, CoreFont.COURIER);
        f0.setSize(14f);

        // Create font for Traditional Chinese text
        // Uses Adobe's Ming Standard Light font (明體)
        Font f1 = new Font(pdf, CJKFont.ADOBE_MING_STD_LIGHT);
        f1.setSize(14f);

        // Create font for Simplified Chinese text
        // Uses Adobe's Heiti SC Light font (黑体-简)
        Font f2 = new Font(pdf, CJKFont.ST_HEITI_SC_LIGHT);
        f2.setSize(14f);

        // Create font for Japanese text
        // Uses Kozuka Mincho Pro VI Regular font (小塚明朝)
        Font f3 = new Font(pdf, CJKFont.KOZ_MIN_PRO_VI_REGULAR);
        f3.setSize(14f);

        // Create font for Korean text
        // Uses Adobe's Myungjo Standard Medium font (명조체)
        Font f4 = new Font(pdf, CJKFont.ADOBE_MYUNGJO_STD_MEDIUM);
        f4.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        String fileName = "data/happy-new-year.txt";
        float x_pos = 100f;
        float y_pos = 100f;
        BufferedReader reader = new BufferedReader(
                new InputStreamReader(new FileInputStream(fileName), "UTF-8"));
        TextLine text = new TextLine(f0);
        String line = null;
        while ((line = reader.readLine()) != null) {
            text.setText(line);
            text.setLocation(x_pos, y_pos);
            text.drawOn(page);
            if (line.contains("Traditional")) {
                text.setFont(f1);
            } else if (line.contains("Simplified")) {
                text.setFont(f2);
            } else if (line.contains("Japanese")) {
                text.setFont(f3);
            } else if (line.contains("Korean")) {
                text.setFont(f4);
            } else {
                text.setFont(f0);
            }
            y_pos += 25f;
        }
        reader.close();

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_04();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_04 => " + (time1 - time0) + " ms");
    }
}   // End of Example_04.java
