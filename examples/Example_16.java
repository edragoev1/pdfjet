package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_16.java
 *
 */
public class Example_16 {
    public Example_16() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_16.pdf")));
        pdf.setCompliance(Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/IBMPlexSans/IBMPlexSans-Regular.ttf");
        // Font f1 = new Font(pdf, "fonts/SourceSerif4/SourceSerif4-Regular.ttf");
        // Font f1 = new Font(pdf, "fonts/NotoSans/NotoSans-Regular.ttf");

        f1.setSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("Lorem", Color.blue);
        colors.put("ipsum", Color.red);
        colors.put("dolor", Color.green);
        colors.put("ullamcorper", Color.gray);

        GraphicsState gs = new GraphicsState();
        gs.setAlphaStroking(0.5f);                  // Stroking alpha
        gs.setAlphaNonStroking(0.5f);               // Nonstroking alpha
        page.setGraphicsState(gs);

        // String latinText = Contents.ofTextFile("data/latin.txt");
        String englishText = Contents.ofTextFile("data/languages/english.txt");
        // f1.setSize(14f);
        TextBox textBox = new TextBox(f1, englishText);
        // textBox.setLocation(50f, 50f);
        // textBox.setLocation(50f, 100f);
        textBox.setLocation(100f, 50f);
        textBox.setWidth(400f);
        // If no height is specified the height will be calculated based on the text.
        textBox.setHeight(450f);
        // textBox.setTextDirection(Direction.LEFT_TO_RIGHT);
        // textBox.setTextDirection(Direction.BOTTOM_TO_TOP);
        // textBox.setTextDirection(Direction.TOP_TO_BOTTOM);

        textBox.setVerticalAlignment(Align.TOP);
        // textBox.setVerticalAlignment(Align.BOTTOM);
        // textBox.setVerticalAlignment(Align.CENTER);

        // textBox.setTextAlignment(Align.CENTER);
        // textBox.setHeight(400f);

        textBox.setBgColor(Color.whitesmoke);
        textBox.setTextColors(colors);
        textBox.setBorders(true);
        float[] xy = textBox.drawOn(page);

        page.setGraphicsState(new GraphicsState()); // Reset GS

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_16();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_16", time0, time1);
    }
}   // End of Example_16.java
