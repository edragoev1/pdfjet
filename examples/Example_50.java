package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;

/**
 *  Example_50.java
 */
class Example_50 {
    public Example_50(String fileNumber, String fileName) throws Exception {
        PDF pdf = new PDF(new BufferedOutputStream(
                new FileOutputStream("Example_" + fileNumber + ".pdf")));

        List<PDFobj> objects = pdf.read(
                new BufferedInputStream(new FileInputStream(fileName)));

        FileInputStream stream = new FileInputStream("images/qrcode.png");
        Image image = new Image(objects, stream, ImageType.PNG);
        image.setLocation(495f, 65f);
        image.scaleBy(0.40f);

        stream = new FileInputStream("fonts/Droid/DroidSans.ttf.stream");
        Font f1 = new Font(objects, stream, Font.STREAM);
        f1.setSize(12f);

        stream = new FileInputStream("fonts/Droid/DroidSans-Bold.ttf.stream");
        Font f2 = new Font(objects, stream, Font.STREAM);
        f2.setSize(12f);

        List<PDFobj> pages = pdf.getPageObjects(objects);
        Page page = new Page(pdf, pages.get(0));
        // page.invertYAxis();

        page.addResource(image, objects);
        page.addResource(f1, objects);
        page.addResource(f2, objects);
        Font f3 = page.addResource(CoreFont.HELVETICA, objects).setSize(12f);

        image.drawOn(page);

        float x = 23f;
        float y = 185f;
        float dx = 15f;
        float dy = 24f;

        page.setBrushColor(Color.blue);

        // First Name and Initial
        page.drawString(f2, "Иван", x, y);

        // Last Name
        page.drawString(f3, "Jones", x + 258f, y);

        // Social Insurance Number
        page.drawString(f1, stripSpacesAndDashes("243-590-129"), x + 437f, y, dx);

        // Last Name at Birth
        page.drawString(f1, "Culverton", x, y += dy);

        // Mailing Address
        page.drawString(f1, "10 Elm Street", x, y += dy);

        // City
        page.drawString(f1, "Toronto", x, y + dy);

        // Province or Territory
        page.drawString(f1, "Ontario", x + 365f, y += dy);

        // Postal Code
        page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home Address
        page.drawString(f1, "10 Oak Road", x, y += dy);

        // City
        y += dy;
        page.drawString(f1, "Toronto", x, y);

        // Previous Province or Territory
        page.drawString(f1, "Ontario", x + 365f, y);

        // Postal Code
        page.drawString(f1, stripSpacesAndDashes("L7B 2E9"), x + 482f, y, dx);

        // Home telephone number
        page.drawString(f1, "905-222-3333", x, y + dy);
        // Work telephone number
        page.drawString(f1, "416-567-9903", x + 279f, y += dy);

        // Previous province or territory
        page.drawString(f1, "British Columbia", x + 452f, y += dy);

        // Move date from previous province or territory
        y += dy;
        page.drawString(f1, stripSpacesAndDashes("2016-04-12"), x + 452f, y, dx);

        // Date new marital status began
        page.drawString(f1, stripSpacesAndDashes("2014-11-02"), x + 452f, 467f, dx);

        // First name of spouse
        y = 521f;
        page.drawString(f1, "Melanie", x, y);
        // Last name of spouse
        page.drawString(f1, "Jones", x + 258f, y);

        // Social Insurance number of spouse
        page.drawString(f1, stripSpacesAndDashes("192-760-427"), x + 437f, y, dx);

        // Spouse or common-law partner's address
        page.drawString(f1, "12 Smithfield Drive", x, 554f);

        // Signature Date
        page.drawString(f1, "2016-08-07", x + 475f, 615f);

        // Signature Date of spouse
        page.drawString(f1, "2016-08-07", x + 475f, 651f);

        // Female Checkbox 1
        // xMarkCheckBox(page, 477.5f, 197.5f, 7f);

        // Male Checkbox 1
        xMarkCheckBox(page, 534.5f, 197.5f, 7f);

        // Married
        xMarkCheckBox(page, 34.5f, 424f, 7f);

        // Living common-law
        // xMarkCheckBox(page, 121.5f, 424f, 7f);

        // Widowed
        // xMarkCheckBox(page, 235.5f, 424f, 7f);

        // Divorced
        // xMarkCheckBox(page, 325.5f, 424f, 7f);

        // Separated
        // xMarkCheckBox(page, 415.5f, 424f, 7f);

        // Single
        // xMarkCheckBox(page, 505.5f, 424f, 7f);

        // Female Checkbox 2
        xMarkCheckBox(page, 478.5f, 536.5f, 7f);

        // Male Checkbox 2
        // xMarkCheckBox(page, 535.5f, 536.5f, 7f);

        page.complete(objects);

        pdf.addObjects(objects);

        pdf.complete();
    }

    private void xMarkCheckBox(Page page, float x, float y, float diagonal) throws Exception {
        page.setPenColor(Color.blue);
        page.setPenWidth(diagonal / 5);
        page.moveTo(x, y);
        page.lineTo(x + diagonal, y + diagonal);
        page.moveTo(x, y + diagonal);
        page.lineTo(x + diagonal, y);
        page.strokePath();
    }

    private String stripSpacesAndDashes(String str) {
        StringBuilder buf = new StringBuilder();
        for (int i = 0; i < str.length(); i++) {
            char ch = str.charAt(i);
            if (ch != ' ' && ch != '-') {
                buf.append(ch);
            }
        }
        return buf.toString();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_50("50", "data/testPDFs/rc65-16e.pdf");
        // new Example_50("50", "data/testPDFs/NoPredictor.pdf");
        // new Example_50("50", "../../eBooks/UniversityPhysicsVolume1.pdf");
        // new Example_50("50", "../../eBooks/PDF32000_2008.pdf");
        long time1 = System.currentTimeMillis();
        TextUtils.printDuration("Example_50", time0, time1);
    }
}   // End of Example_50.java
