package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_16.java
 */
public class Example_16 {
    public Example_16() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_16.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Text block with highlighted keywords");

        // Font f1 = new Font(pdf, SourceSerif4.Regular);
        // Font f1 = new Font(pdf, NotoSans.Regular);
        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("Everyone", Color.red);
        colors.put("pay", Color.green);
        colors.put("freedom", Color.blue);

        // page.saveGraphicsState();

        GraphicsState gs = new GraphicsState();
        gs.setAlphaStroking(0.5f);                  // Stroking alpha
        gs.setAlphaNonStroking(0.5f);               // Non-Stroking alpha
        page.setGraphicsState(gs);

        String englishText = Content.ofTextFile("data/languages/english.txt");
        // f1.setSize(14f);
        TextBlock textBlock = new TextBlock(f1, englishText);
        textBlock.setLocation(100f, 50f);
        textBlock.setWidth(400f);
        // With a height the text that does not fit is cut; without one the
        // block is as tall as its text.
        textBlock.setHeight(450f);
        textBlock.setVerticalAlignment(Alignment.TOP);
        // textBlock.setVerticalAlignment(Alignment.BOTTOM);
        // textBlock.setVerticalAlignment(Alignment.CENTER);
        // textBlock.setTextAlignment(Alignment.CENTER);
        textBlock.setBackgroundColor(Color.whitesmoke);
        textBlock.setHighlightColors(colors);
        textBlock.setBorderColor(Color.black);
        float[] xy = textBlock.drawOn(page);

        page.setGraphicsState(new GraphicsState()); // Reset GS
        // page.restoreGraphicsState();

        Rect rect = new Rect(xy[0], xy[1], 20f, 20f);
        rect.setBorderColor(Color.black);
        rect.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_16();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_16 => %4d ms%n", time1 - time0);
    }
}   // End of Example_16.java
