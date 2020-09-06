package examples;

import java.io.*;
import com.pdfjet.*;


/**
 *  Example_71.java
 *
 */
public class Example_71 {

    public Example_71() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_71.pdf")));

        Font f1 = new Font(
                pdf,
                getClass().getResourceAsStream("../fonts/Droid/DroidSerif-Bold.ttf.stream"),
                Font.STREAM);
        f1.setSize(12f);

        Font f2 = new Font(
                pdf,
                getClass().getResourceAsStream("../fonts/Droid/DroidSerif-Italic.ttf.stream"),
                Font.STREAM);
        f2.setSize(12f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        CalendarMonth calendar = new CalendarMonth(f1, f2, 2018, 9);
        calendar.setLocation(0f, 0f);
        float[] point = calendar.drawOn(page);

        CalendarMonth calendar2 = new CalendarMonth(f1, f2, 2018, 10);
        calendar2.setLocation(0f, point[1]);
        calendar2.drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_71();
        long time1 = System.currentTimeMillis();
        System.out.println("Example_71 => " + (time1 - time0));
    }

}   // End of Example_71.java
