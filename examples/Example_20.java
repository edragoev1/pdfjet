package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_20.java
 *
 */
class Example_20 {

    public Example_20() throws Exception {

        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_20.pdf")));

        BufferedInputStream bis = new BufferedInputStream(
                new FileInputStream("data/testPDFs/PDFjetLogo.pdf"));
        List<PDFobj> objects = pdf.read(bis);

        pdf.addResourceObjects(objects);

        FileInputStream stream = new FileInputStream("fonts/OpenSans/OpenSans-Regular.ttf.stream");
        Font f1 = new Font(pdf, stream, Font.STREAM);
        stream.close();
        f1.setSize(18f);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        PDFobj contents = pages.get(0).getContentsObject(objects);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float height = 105f;    // The logo height in points.
        float x = 50f;
        float y = 50f;
        float xScale = 0.5f;
        float yScale = 0.5f;

        page.drawContents(
                contents.getData(),
                height,
                x,
                y,
                xScale,
                yScale);

        page.setPenColor(Color.darkblue);
        page.setPenWidth(0f);
        page.drawRect(0f, 0f, 50f, 50f);

        Path path = new Path();

        path.add(new Point(13.0f,  0.0f));
        path.add(new Point(15.5f,  4.5f));

        path.add(new Point(18.0f,  3.5f));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(15.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(20.5f,  7.5f));

        path.add(new Point(21.0f,  9.5f));
        path.add(new Point(25.0f,  9.0f));
        path.add(new Point(24.0f, 13.0f));
        path.add(new Point(25.5f, 14.0f));
        path.add(new Point(19.0f, 19.0f));
        path.add(new Point(20.0f, 21.5f));
        path.add(new Point(13.5f, 20.5f));
        path.add(new Point(13.5f, 27.0f));
        path.add(new Point(12.5f, 27.0f));
        path.add(new Point(12.5f, 20.5f));
        path.add(new Point( 6.0f, 21.5f));
        path.add(new Point( 7.0f, 19.0f));
        path.add(new Point( 0.5f, 14.0f));
        path.add(new Point( 2.0f, 13.0f));
        path.add(new Point( 1.0f,  9.0f));
        path.add(new Point( 5.0f,  9.5f));

        path.add(new Point( 5.5f,  7.5f));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point(10.5f, 13.5f, Point.CONTROL_POINT));
        path.add(new Point( 8.0f,  3.5f));

        path.add(new Point(10.5f,  4.5f));
        path.setClosePath(true);
        path.setColor(Color.red);
        // path.setFillShape(true);
        path.setLocation(100f, 100f);
        path.scaleBy(10f);

        path.drawOn(page);

        page = new Page(pdf, Letter.PORTRAIT);

        TextLine line = new TextLine(f1, "Hello, World!");
        line.setLocation(50f, 50f);
        line.drawOn(page);

        QRCode qr = new QRCode(
                "https://kazuhikoarase.github.io",
                ErrorCorrectLevel.L);   // Low
        qr.setModuleLength(3f);
        qr.setLocation(50f, 200f);
        qr.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_20();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_20 => " + (t1 - t0));
    }

}   // End of Example_20.java
