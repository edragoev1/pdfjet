package examples;

import java.io.*;
import java.util.*;

import com.pdfjet.*;


/**
 *  Example_45.java
 *
 */
public class Example_45 {

    public Example_45() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_45.pdf")),
                        Compliance.PDF_UA);
        pdf.setLanguage("en-US");

        Font f1 = new Font(pdf, new FileInputStream(
            "fonts/Droid/DroidSerif-Regular.ttf.stream"), Font.STREAM);
        Font f2 = new Font(pdf, new FileInputStream(
            "fonts/Droid/DroidSerif-Italic.ttf.stream"), Font.STREAM);
        Font f3 = new Font(pdf, new FileInputStream(
            "fonts/SourceCodePro/SourceCodePro-Regular.ttf.stream"), Font.STREAM);

        f1.setSize(14f);
        f2.setSize(14f);
        f3.setSize(10f);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float w = 530f;
        float h = 13f;

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
        fields.add(new Field(  w/2, new String[] {"Email","jsmith12345@gmail.ca"}));
        fields.add(new Field(   0f, new String[] {
                "Other Information","We don't work on weekends.", "Please send us an Email."}));

        new Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(7f)
                .setValueFont(f2)
                .setValueFontSize(9f)
                .setLocation(50f, 50f)
                .setRowLength(w)
                .setRowHeight(h)
                .drawOn(page);

        Map<String, Integer> colors = new HashMap<String, Integer>();
        colors.put("new", Color.red);
        colors.put("ArrayList", Color.blue);
        colors.put("List", Color.blue);
        colors.put("String", Color.blue);
        colors.put("Field", Color.blue);
        colors.put("Form", Color.blue);
        colors.put("Smart", Color.green);
        colors.put("Widget", Color.green);
        colors.put("Designs", Color.green);

        float x = 50;
        float y = 280;
        float dy = f3.getBodyHeight();
        List<String> lines = Text.readLines("data/form-code-java.txt");
        for (String line : lines) {
            page.drawString(f3, line, x, y, colors);
            y += dy;
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_45();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_45 => " + (t1 - t0));
    }

}   // End of Example_45.java
