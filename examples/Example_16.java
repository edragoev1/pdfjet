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

        /// Font f1 = new Font(pdf, CoreFont.HELVETICA);
        Font f1 = new Font(
                pdf,
                new FileInputStream("fonts/RedHatText/RedHatText-Regular.ttf.stream"),
                Font.STREAM);

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

        f1.setSize(72f);
        TextLine text = new TextLine(f1, "Hello, World");
        text.setLocation(50f, 300f);
        text.drawOn(page);

        File file = new File("data/latin.txt");
        FileInputStream fis = new FileInputStream(file);
        byte[] data = new byte[(int) file.length()];
        fis.read(data);
        fis.close();
        String latinText = new String(data, "UTF-8");

        f1.setSize(14f);
        TextBox textBox = new TextBox(f1, latinText);
        textBox.setLocation(50f, 50f);
        textBox.setWidth(400f);
        // If no height is specified the height will be calculated based on the text.
        // textBox.setHeight(400f);
        // textBox.setVerticalAlignment(Align.TOP);
        // textBox.setVerticalAlignment(Align.BOTTOM);
        // textBox.setVerticalAlignment(Align.CENTER);
        textBox.setBgColor(Color.whitesmoke);
        textBox.setTextColors(colors);
        float[] xy = textBox.drawOn(page);

        page.setGraphicsState(new GraphicsState()); // Reset GS

        Box box = new Box();
        box.setLocation(xy[0], xy[1]);
        box.setSize(20f, 20f);
        box.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_16();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_16 => " + (t1 - t0));
    }

}   // End of Example_16.java
