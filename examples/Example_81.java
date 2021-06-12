package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;

/**
 *  Example_81.cs
 */
public class Example_81 {

    public Example_81() throws Exception {

        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_81.pdf")), Compliance.PDF_UA);
        pdf.setTitle("PDF/UA compliant document");

        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf.stream"),
                Font.STREAM);
        f1.setSize(14f);

        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Italic.ttf.stream"),
                Font.STREAM);
        f2.setSize(14f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        LinkedList<TextLine> paragraphs = new LinkedList<TextLine>();

        paragraphs.add(new TextLine(f1,
"The centres also offer free one-on-one consultations with business advisors who can review your business plan and make recommendations to improve it. The small business centres offer practical resources, from step-by-step info on setting up your business to sample business plans to a range of business-related articles and books in our resource libraries."));
        paragraphs.add(new TextLine(f2,
"This text is blue color and is written using italic font.").setColor(Color.blue));

        float height = 82f;

        Line line = new Line(70f, 150f, 70f, 150f + height);
        line.drawOn(page);

        TextFrame frame = new TextFrame(paragraphs);
        frame.setLocation(70f, 150f);
        frame.setWidth(500f);
        frame.setHeight(height);
        frame.drawOn(page);

        if (frame.isNotEmpty()) {
            frame.setLocation(70f, 350f);
            frame.setWidth(500f);
            frame.setHeight(90f);
            frame.drawOn(page);
        }

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_81();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_81 => " + (t1 - t0));
    }

}   // End of Example_81.java
