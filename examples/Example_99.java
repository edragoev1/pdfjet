package examples;

import java.io.*;

import com.pdfjet.*;


/**
 *  Example_99.java
 */
public class Example_99 {
    public Example_99() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_99.pdf")));

        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/Arimo/Arimo-Regular.ttf.stream"),
                Font.STREAM);

        Page page = new Page(pdf, Letter.LANDSCAPE);

        f1.setSize(14f);
        TextLine textLine = new TextLine(f1,
                "Предшественниками математического анализа были античный метод исчерпывания и метод неделимых.");
        textLine.setLocation(50f, 50f);
        textLine.setColor(Color.oldgloryblue);
        textLine.drawOn(page);

        textLine = new TextLine(f1,
                "Λογισμός είναι η μαθηματική μελέτη της συνεχούς μεταβολής των τιμών.");
        textLine.setLocation(50f, 100f);
        textLine.setColor(Color.oldgloryblue);
        textLine.drawOn(page);

        textLine = new TextLine(f1,
                "Calculus is the mathematical study of continuous change.");
        textLine.setLocation(50f, 150f);
        textLine.setColor(Color.oldgloryblue);
        textLine.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_99();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_99 => " + (time1 - time0));
    }

}   // End of Example_99.java
