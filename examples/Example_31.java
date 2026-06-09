package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_31.java
 */
public class Example_31 {
    public Example_31() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_31.pdf")));

        Font f1 = new Font(pdf,
                "fonts/NotoSansDevanagari/NotoSansDevanagari-Regular.ttf.stream");
        f1.setSize(15f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextBlock textBlock = new TextBlock(
                f1, Content.ofTextFile("data/languages/marathi.txt"));
        textBlock.setLocation(50f, 50f);
        textBlock.setWidth(500f);
//         textBlock.setBorder(Border.LEFT);
//         textBlock.setBorder(Border.RIGHT);
        textBlock.drawOn(page);

        String str = "असम के बाद UP में भी CM कैंडिडेट का ऐलान करेगी BJP?";
        TextLine textLine = new TextLine(f1, str);
        textLine.setLocation(50f, 175f);
        textLine.drawOn(page);

        page.setPenColor(Color.blue);
        page.setBrushColor(Color.blue);
        page.fillRect(50f, 200f, 200f, 200f);
//
//         GraphicsState gs = new GraphicsState();
//         gs.setAlphaStroking(0.5f);      // The stroking alpha constant
//         gs.setAlphaNonStroking(0.5f);   // The non-stroking alpha constant
//         page.setGraphicsState(gs);
//         page.setPenColor(Color.green);
//         page.setBrushColor(Color.green);
//         page.fillRect(100f, 250f, 200f, 200f);
//
//         page.setPenColor(Color.red);
//         page.setBrushColor(Color.red);
//         page.fillRect(150, 300, 200f, 200f);
//
//         // Reset the parameters to the default values
//         page.setGraphicsState(new GraphicsState());
//
//         page.setPenColor(Color.orange);
//         page.setBrushColor(Color.orange);
//         page.fillRect(200, 350, 200f, 200f);
//
//         page.setBrushColor(0x00003865);
//         page.fillRect(50, 550, 200f, 200f);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_31();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_31", time0, time1);
    }
}   // End of Example_31.java
