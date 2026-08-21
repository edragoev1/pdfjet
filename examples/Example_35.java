package examples;

import java.util.*;
import java.io.*;
import java.nio.file.*;
import java.time.Duration;
import java.time.Instant;
import com.pdfjet.*;

/**
 * Example_35.java
 */
public class Example_35 {
    public Example_35() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_35.pdf")));

        Page page = new Page(pdf, Letter.PORTRAIT);

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(14f);

        Font f2 = new Font(pdf, IBMPlexSans.Bold);
        f2.setSize(14f);

        // Base container
        Container container = new Container(400f, 400f);
        container.setLocation(100f, 100f);

        // Add a rectangle to container
        Rect rect = new Rect(0f, 0f, 400f, 400f);
        rect.setFillColor(Color.gray);
        container.add(rect);

        Stamp stamp = new Stamp(pdf).withSize(400f, 400f).withFont(f1).withFont(f2);

        // Draw path ...
        stamp.setFillColor(Color.lightblue)
            .setStrokeColor(Color.red)
            .setStrokeWidth(4f)
            .moveTo(0f, 0f)
            .lineTo(400f, 0f)
            .lineTo(400f, 400f)
            .lineTo(0f, 400f)
            .closeFillAndStrokePath();

        // Draw Rectangle
        stamp.setStrokeColor(Color.blue)
            .setStrokeWidth(1f)
            .drawRect(10f, 10f, 380f, 380f);

        // Fill Rectangle
        stamp.setFillColor(Color.green).fillRect(10f, 10f, 20f, 20f);

        // Draw some text
        TextParameters parameters = new TextParameters()
            .setFont(f1)
            .setFontSize(14f)
            .setTextLocation(25f, 25f)
            .setText("Hello, World!");
        stamp.drawText(parameters);

        // Change some parameters and draw the text again
        parameters.setFont(f2).setTextLocation(25f, 50f);
        stamp.setFillColor(Color.darkgreen).drawText(parameters);

        stamp.complete();   // The stamp is complete!

        stamp.setLocation(50f, 50f).drawOn(page);

        // Rotate the stamp counter clockwise and draw it again
        stamp.rotate(15).drawOn(page);

        // Rotate the stamp clockwise and draw it again
        stamp.rotate(-15).drawOn(page);

        // Add a text line to container
        TextLine title = new TextLine(f1, "Container");
        title.setLocation(150f, 20f);
        container.add(title);

        // Nested container #1
        Container nested1 = new Container(200f, 200f);
        nested1.setLocation(0f, 0f);
        nested1.setRotationCounterClockwise(30);
        nested1.setScaleFactor(0.8f);

        Rect innerRect = new Rect(0f, 0f, 200f, 200f);
        innerRect.setFillColor(Color.blue);
        nested1.add(innerRect);

        TextLine innerText = new TextLine(f1, "Nested 1");
        innerText.setLocation(50f, 100f);
        nested1.add(innerText);

        container.add(nested1);

        // Nested container #2
        Container nested2 = new Container(100f, 100f);
        nested2.setLocation(250f, 250f);
        nested2.setRotationCounterClockwise(45);

        Rect smallRect = new Rect(0f, 0f, 100f, 100f);
        smallRect.setFillColor(Color.red);
        nested2.add(smallRect);

        TextLine smallText = new TextLine(f1, "Nested 2");
        smallText.setLocation(10f, 50f);
        nested2.add(smallText);

        container.add(nested2);

        container.setRotationClockwise(45);
        // Draw the entire hierarchy on the page
        container.drawOn(page);

        Container container5 = new Container(200f, 20f);
        Rect rect5 = new Rect(0f, 0f, 200f, 20f);
        container5.add(rect5);

        Rect rect6 = new Rect(0f, 0f, 10f, 10f);
        rect6.setFillColor(Color.blue);
        container5.add(rect6);

        Rect rect7 = new Rect(190f, 10f, 10f, 10f);
        rect7.setBorderColor(Color.red);
        rect7.setBorderWidth(2f);
        container5.add(rect7);

        container5.setLocation(50f, 600f);
        container5.drawOn(page);

        container5.setRotation(-90);
        container5.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_35();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_35", time0, time1);
    }
}
