package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_06.java
 */
public class Example_06 {
    public Example_06() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_06.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", false);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02.java", true);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(file1);
        attachment.setLocation(100f, 600f);
        attachment.setIconPushpin();
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        attachment = new FileAttachment(file2);
        attachment.setLocation(200f, 600f);
        attachment.setIconPaperclip();
        attachment.setTitle("Attached File: " + file2.getFileName());
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        TextLine textLine = new TextLine(f1, "pdfjet.com");
        textLine.setLocation(300f, 618f);
        textLine.setURIAction("https://pdfjet.com");
        textLine.drawOn(page);

        TextAnnotation textAnnotation = new TextAnnotation();
        textAnnotation.setLocation(400f, 600f);
        textAnnotation.setSize(24f, 24f);
        textAnnotation.setTitle("Hello");
        textAnnotation.setContents("World");
        textAnnotation.drawOn(page);

        Container container = new Container(400f, 400f);
        container.setLocation(100f, 100f);
        container.setBorderColor(Color.black);
        container.setRotation(90);

        Rect rect = new Rect(0f, 0f, 25f, 25f);
        rect.setBorderColor(Color.black);
        rect.setBorderWidth(1f);
        container.add(rect);

        PolygonAnnotation polygonAnnotation = new PolygonAnnotation();
        polygonAnnotation.setLocation(0f, 0f);
        polygonAnnotation.setVertices(new float[] {0f, 0f, 50f, 0f, 0f, 50f, 0f, 0f});
        polygonAnnotation.setFillColor(Color.red);
        polygonAnnotation.setOpacity(0.5f);
        polygonAnnotation.setTitle("Polygon");
        polygonAnnotation.setContents("Polygon Annotation");
        container.add(polygonAnnotation);

        SquareAnnotation squareAnnotation = new SquareAnnotation();
        squareAnnotation.setLocation(25f, 0f);
        squareAnnotation.setSize(50f, 50f);
        squareAnnotation.setFillColor(new float[] {0f, 0f, 1f});
        squareAnnotation.setOpacity(0.5f);
        squareAnnotation.setTitle("Square");
        squareAnnotation.setContents("Square Annotation");
        container.add(squareAnnotation);

        CircleAnnotation circleAnnotation = new CircleAnnotation();
        circleAnnotation.setLocation(50f, 0f);
        circleAnnotation.setSize(50f, 50f);
        circleAnnotation.setFillColor(new float[] {0f, 0f, 1f});
        circleAnnotation.setOpacity(0.5f);
        circleAnnotation.setTitle("Circle");
        circleAnnotation.setContents("Circle Annotation");
        container.add(circleAnnotation);

        container.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_06();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_06 => " + (time1 - time0) + " ms");
    }
}   // End of Example_06.java
