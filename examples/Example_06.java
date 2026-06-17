package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_06.java
 * We will draw the American flag using Box, Line and Point objects.
 */
public class Example_06 {
    public Example_06() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_06.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);

        EmbeddedFile file1 = new EmbeddedFile(pdf, "images/linux-logo.png", Compress.NO);
        EmbeddedFile file2 = new EmbeddedFile(pdf, "examples/Example_02.java", Compress.YES);

        Page page = new Page(pdf, Letter.PORTRAIT);

        // File attachment functionality
        FileAttachment attachment = new FileAttachment(pdf, file1);
        attachment.setLocation(100f, 600f);
        attachment.setIconPushPin();
        // attachment.setIconSize(25f);
        attachment.setTitle("Attached File: " + file1.getFileName());
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        attachment = new FileAttachment(pdf, file2);
        attachment.setLocation(200f, 600f);
        attachment.setIconPaperclip();
        // attachment.setIconSize(25f);
        attachment.setTitle("Attached File: " + file2.getFileName());
        attachment.setDescription(
                "Right mouse click on the icon to save the attached file.");
        attachment.drawOn(page);

        TextLine textLine = new TextLine(f1, "pdfjet.com");
        textLine.setLocation(300f, 618f);
        textLine.setURIAction("https://pdfjet.com");
        textLine.drawOn(page);

        TextAnnotation textAnnotation = new TextAnnotation();
        textAnnotation.setLocation(400f, 600f);
        textAnnotation.setSize(25f, 25f);
        textAnnotation.setTitle("Hello");
        textAnnotation.setContents("World");
        textAnnotation.drawOn(page);

        Container container = new Container(400f, 400f);
        container.setLocation(100f, 100f);
        container.setBorderColor(Color.black);
        container.setRotationClockwise(90);

        Rect rect = new Rect();
        rect.setSize(25f, 25f);
        container.add(rect);

        PolygonAnnotation polygonAnnotation = new PolygonAnnotation();
        polygonAnnotation.setLocation(0f, 0f);
        polygonAnnotation.setVertices(new float[] {0f, 0f, 50f, 0f, 0f, 50f, 0f, 0f});
        polygonAnnotation.setFillColor(Color.red);
        polygonAnnotation.setTransparency(0.5f);
        polygonAnnotation.setTitle("This is a test ...");
        polygonAnnotation.setContents("The quick brown cat caught the lazy mouse.");
        container.add(polygonAnnotation);

        SquareAnnotation squareAnnotation = new SquareAnnotation();
        squareAnnotation.setLocation(25f, 0f);
        squareAnnotation.setSize(50f, 50f);
        squareAnnotation.setFillColor(new float[] {0f, 0f, 1f});
        squareAnnotation.setTransparency(0.5f);
        squareAnnotation.setTitle("Hello, World!");
        squareAnnotation.setContents("The quick brown fox jumps over the lazy dog.");
        container.add(squareAnnotation);

        CircleAnnotation circleAnnotation = new CircleAnnotation();
        circleAnnotation.setLocation(50f, 0f);
        circleAnnotation.setSize(50f, 50f);
        circleAnnotation.setFillColor(new float[] {0f, 0f, 1f});
        circleAnnotation.setTransparency(0.5f);
        circleAnnotation.setTitle("Circle");
        circleAnnotation.setContents("Annotation");
        container.add(circleAnnotation);

        container.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_06();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_06", time0, time1);
    }
}   // End of Example_06.java
