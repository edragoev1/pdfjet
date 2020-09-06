package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_72.java
 *
 */
public class Example_72 {

    public Example_72() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_72.pdf")),
                        Compliance.PDF_UA);

        Font f1 = new Font(pdf, getClass().getResourceAsStream(
                "../fonts/OpenSans/OpenSans-Regular.ttf.stream"), Font.STREAM);
        f1.setSize(14f);

        Font f2 = new Font(pdf, getClass().getResourceAsStream(
                "../fonts/OpenSans/OpenSans-Italic.ttf.stream"), Font.STREAM);
        f2.setSize(9f);
/*
        Font f1 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Regular.ttf"));
        f1.setSize(14f);

        Font f2 = new Font(pdf,
                getClass().getResourceAsStream("../fonts/OpenSans/OpenSans-Italic.ttf"));
        f2.setSize(9f);
*/
        StringBuilder buf = new StringBuilder();
        BufferedReader reader =
                new BufferedReader(new FileReader("data/calculus.txt"));
        String line = null;
        while ((line = reader.readLine()) != null) {
            buf.append(line);
            buf.append("\n");
        }
        reader.close();

        float w = 530f;
        float h = 13f;

        Page page = new Page(pdf, Letter.PORTRAIT);

        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(   0f, new String[] {"Company", "Smart Widget Designs"}));
        fields.add(new Field(   0f, new String[] {"Street Number", "120"}));
        fields.add(new Field(  w/8, new String[] {"Street Name", "Oak"}));
        fields.add(new Field(5*w/8, new String[] {"Street Type", "Street"}));
        fields.add(new Field(6*w/8, new String[] {"Direction", "West"}));
        fields.add(new Field(7*w/8, new String[] {"Suite/Floor/Apt.", "8W"})
                .setAltDescription("Suite/Floor/Apartment")
                .setActualText("Suite/Floor/Apartment"));
        fields.add(new Field(   0f, new String[] {"City/Town", "Toronto"}));
        fields.add(new Field(  w/2, new String[] {"Province", "Ontario"}));
        fields.add(new Field(7*w/8, new String[] {"Postal Code", "M5M 2N2"}));
        fields.add(new Field(   0f, new String[] {"Telephone Number", "(416) 331-2245"}));
        fields.add(new Field(  w/4, new String[] {"Fax (if applicable)", "(416) 124-9879"}));
        fields.add(new Field(  w/2, new String[] {"Email", "jsmith12345@gmail.ca"}));
        fields.add(new Field(   0f, new String[] {"Other Information", buf.toString()}, true));

        // TODO
        new Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(7f)
                .setValueFont(f2)
                .setValueFontSize(9f)
                .setLocation(50f, 60f)
                .setRowLength(w)
                .setRowHeight(h)
                .drawOn(page);

        pdf.complete();
    }


    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_72();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_72 => " + (t1 - t0));
    }

}   // End of Example_72.java
