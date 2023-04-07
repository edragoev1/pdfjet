package examples;

import java.io.*;
import com.pdfjet.*;

/**
 *  Example_33.java
 */
public class Example_33 {
    public Example_33() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_33.pdf")));

        Page page = new Page(pdf, A4.PORTRAIT);

        Image image = new Image(pdf, "images/photoshop.jpg");
        image.setLocation(10f, 10f);
        image.scaleBy(0.25f);
        image.drawOn(page);

        SVGImage icon = new SVGImage(
            "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setLocation(20f, 670f);
        float[] xy = icon.drawOn(page);

        icon = new SVGImage("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setLocation(xy[0], 670f);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setLocation(xy[0], 670f);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setLocation(xy[0], 670f);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        icon.setLocation(xy[0], 670);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg-test/test-CS.svg");
        icon.setLocation(xy[0], 670);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg-test/test-QQ.svg");
        icon.setLocation(xy[0], 670);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg-test/menu-icon.svg");
        icon.setLocation(xy[0], 670);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg-test/menu-icon-close.svg");
        icon.setLocation(xy[0], 670);
        xy = icon.drawOn(page);

        icon = new SVGImage("images/svg-test/europe.svg");
        icon.setLocation(0f, 0f);
        icon.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_33();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_33 => " + (t1 - t0));
    }
}   // End of Example_33.java
