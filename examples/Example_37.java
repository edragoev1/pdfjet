package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_37.java
 */
class Example_37 {
    public Example_37() throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(new FileOutputStream("Example_37.pdf")));

        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/wirth.pdf"));
        BufferedInputStream bis = new BufferedInputStream(new FileInputStream("../../eBooks/UniversityPhysicsVolume1.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/Smalltalk-and-OO.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/InsideSmalltalk1.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/InsideSmalltalk2.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/Greenbook.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/Bluebook.pdf"));
        // BufferedInputStream bis = new BufferedInputStream(new FileInputStream("data/testPDFs/Orangebook.pdf"));
        List<PDFobj> objects = pdf.read(bis);
        bis.close();

        FileInputStream stream = new FileInputStream("fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f1 = new Font(objects, stream, Font.STREAM);
        stream.close();
        f1.setSize(72f);

        TextLine text = new TextLine(f1, "This is a test!");
        text.setLocation(50f, 350f);
        text.setColor(Color.peru);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        for (PDFobj pageObj : pages) {
            GraphicsState gs = new GraphicsState();
            gs.setAlphaStroking(0.75f);         // Stroking alpha
            gs.setAlphaNonStroking(0.75f);      // Nonstroking alpha
            pageObj.setGraphicsState(gs, objects);

            Page page = new Page(pdf, pageObj);

            page.addResource(f1, objects);
            page.setBrushColor(Color.blue);
            page.drawString(f1, "Hello, World!", 50f, 200f);

            text.drawOn(page);

            page.complete(objects); // The graphics stack is unwinded automatically
        }
        pdf.addObjects(objects);

/*
        Font f1 = new Font(pdf, CoreFont.HELVETICA);
        f1.setSize(72f);

        Map<Integer, Image> images = new TreeMap<Integer, Image>();
        for (PDFobj obj : objects.values()) {
            if (obj.getValue("/Subtype").equals("/Image")) {
                float w = Float.valueOf(obj.getValue("/Width"));
                float h = Float.valueOf(obj.getValue("/Height"));
                if (w > 500f && h > 500f) {
                    images.put(obj.getNumber(), new Image(pdf, obj));
                }
            }
        }

        Page page = null;
        for (Image image : images.values()) {
            page = new Page(pdf, A4.PORTRAIT);

            GraphicsState gs = new GraphicsState();
            gs.set_CA(0.7f);    // Stroking alpha
            gs.set_ca(0.7f);    // Nonstroking alpha
            page.setGraphicsState(gs);

            image.resizeToFit(page, true);

            // image.flipUpsideDown(true);
            // image.setLocation(0f, -image.getHeight());

            // image.setRotate(ClockWise._180_degrees);
            // image.setLocation(0f, 0f);

            image.drawOn(page);

            TextLine text = new TextLine(f1, "Hello, World!");
            text.setColor(Color.blue);
            text.setLocation(50f, 200f);
            text.drawOn(page);

            page.setGraphicsState(new GraphicsState());
        }
*/
        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_37();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_37 => " + (time1 - time0));
    }
}   // End of Example_37.java
