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

        SVGImage image = new SVGImage("images/svg-test/europe.svg");
        image.setLocation(-150f, 0f);
        float[] xy = image.drawOn(page);

        image = new SVGImage(
            "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg");
        image.setLocation(20f, 670f);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg");
        image.setLocation(xy[0], 670f);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg");
        image.setLocation(xy[0], 670f);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg");
        image.setLocation(xy[0], 670f);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg/star_FILL0_wght400_GRAD0_opsz48.svg");
        image.setLocation(xy[0], 670);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg-test/test-CS.svg");
        image.setLocation(xy[0], 670);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg-test/test-QQ.svg");
        image.setLocation(xy[0], 670);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg-test/menu-icon.svg");
        image.setLocation(xy[0], 670);
        xy = image.drawOn(page);

        image = new SVGImage("images/svg-test/menu-icon-close.svg");
        image.setLocation(xy[0], 670);
        image.scaleBy(2.0f);
        image.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long t0 = System.currentTimeMillis();
        new Example_33();
        long t1 = System.currentTimeMillis();
        System.out.println("Example_33 => " + (t1 - t0));
    }
}   // End of Example_33.java
