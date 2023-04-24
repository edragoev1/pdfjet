package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_22.java
 */
public class Example_22 {
    public Example_22() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_22.pdf")),
                        Compliance.PDF_UA);

        Font f1 = new Font(pdf, "fonts/OpenSans/OpenSans-Regular.ttf.stream");

        Page page = new Page(pdf, Letter.PORTRAIT);
        TextLine text = new TextLine(f1, "Page #1 -> Go to Destination #3.");
        text.setGoToAction("dest#3");
        text.setLocation(90f, 50f);
        page.addDestination("dest#0", 0f);
        page.addDestination("dest#1", text.getDestinationY());
        text.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #2 -> Go to Destination #3.");
        text.setGoToAction("dest#3");
        text.setLocation(90f, 550f);
        page.addDestination("dest#2", text.getDestinationY());
        text.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #3 -> Go to Destination #4.");
        text.setGoToAction("dest#4");
        text.setLocation(90f, 700f);
        page.addDestination("dest#3", text.getDestinationY());
        text.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);
        text = new TextLine(f1, "Page #4 -> Go to Destination #0.");
        text.setGoToAction("dest#0");
        text.setLocation(90f, 100f);
        page.addDestination("dest#4", text.getDestinationY());
        text.drawOn(page);

        text = new TextLine(f1, "Page #4 -> Go to Destination #2.");
        text.setGoToAction("dest#2");
        text.setLocation(90f, 200f);
        text.drawOn(page);

        // Create a box with invisible borders
        Box box = new Box(20f, 20f, 20f, 20f);
        box.setColor(Color.white);
        box.setGoToAction("dest#0");
        box.drawOn(page);

        // Create an up arrow and place it in the box
        Path path = new Path();
        path.add(new Point(10f,  1f));
        path.add(new Point(17f,  9f));
        path.add(new Point(13f,  9f));
        path.add(new Point(13f, 19f));
        path.add(new Point( 7f, 19f));
        path.add(new Point( 7f,  9f));
        path.add(new Point( 3f,  9f));
        path.setClosePath(true);
        path.setColor(Color.oldgloryblue);
        path.setColor(Color.deepskyblue);
        path.setFillShape(true);
        path.placeIn(box);
        path.drawOn(page);

        Image image = new Image(pdf, "images/up-arrow.png");
        image.setLocation(40f, 40f);
        image.setGoToAction("dest#0");
        image.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_22();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_22", time0, time1);
    }
}   // End of Example_22.java
