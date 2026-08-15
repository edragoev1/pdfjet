package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 * Example_42.java
 */
public class Example_42 {
    public Example_42() throws Exception {

        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_42.pdf")));

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        float w = 500f; // The width of the form

        List<Field> fields = new ArrayList<Field>();
        fields.add(new Field(   0f, "Company", "Smart Widgets Inc."));
        fields.add(new Field(   0f, "Street #", "120"));
        fields.add(new Field(  w/8, "Street Name", "Oak"));
        fields.add(new Field(4*w/8, "Street Type", "Street"));
        fields.add(new Field(5*w/8, "Direction", "West"));
        fields.add(new Field(6*w/8, "Suite/Floor/Apartment", "8W"));
        fields.add(new Field(   0f, "City/Town", "Toronto"));
        fields.add(new Field(4*w/8, "Province", "Ontario"));
        fields.add(new Field(7*w/8, "Postal Code", "M5M 2N2"));
        fields.add(new Field(   0f, "Telephone #", "(416) 331-2245"));
        fields.add(new Field(2*w/8, "Fax #", "(416) 124-9879"));
        fields.add(new Field(4*w/8, "Email","jsmith12345@gmail.ca"));
        fields.add(new Field(   0f, "Other Information",
            "Smart Widgets Inc. designs intelligent IoT widgets that connect everyday appliances to cloud ecosystems,"));
        fields.add(new Field(   0f, "", "enabling remote control and predictive maintenance."));

        float[] xy = (new Form(fields)
                .setLabelFont(f1)
                .setLabelFontSize(9f)
                .setValueFont(f2)
                .setValueFontSize(10f)
                .setLocation(50f, 50f)
                .setFormWidth(w)
                .setLineWidth(0.2f)
                .drawOn(page));

	    Rect rect = new Rect(xy[0], xy[1], 10f, 10f);
	    rect.setBorderWidth(0.2f);
	    rect.setBorderColor(Color.blue);
	    rect.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_42();
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_42", time0, time1);
    }
}   // End of Example_42.java
