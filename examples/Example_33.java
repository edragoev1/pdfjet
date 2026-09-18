/*
 * Example_33.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_33.java
 * Using the SVGImage component to draw a map of Europe, scaled to fit the page,
 * and a set of icons, each labeled with its name.
 */
public class Example_33 {
    public Example_33() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_33.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("SVG Images");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, A4.PORTRAIT);

        TextLine text = new TextLine(f2, "SVG Images");
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 1,000 points wide "
                + "in its file and is scaled by 0.5 to fit the page.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(495f);
        float[] xy = textBlock.drawOn(page);

        SVGImage map = new SVGImage("images/svg-test/europe.svg");
        map.scaleBy(0.5f);
        map.setLocation((page.getWidth() - map.getWidth()) / 2f, xy[1] + 20f);
        xy = map.drawOn(page);

        textBlock = new TextBlock(f1,
                "The colors come from the file: the peachpuff fill of the svg element "
                + "for most countries, an aliceblue fill for Spain and an olive "
                + "outline for Austria.");
        textBlock.setFontSize(10f);
        textBlock.setTextColor(Color.gray);
        textBlock.setLocation(50f, xy[1] + 10f);
        textBlock.setWidth(495f);
        xy = textBlock.drawOn(page);

        String[] iconFiles = {
            "images/svg/home_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/search_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/shopping_cart_checkout_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/palette_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/auto_stories_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/add_circle_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/star_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg/settings_FILL0_wght400_GRAD0_opsz48.svg",
            "images/svg-test/menu-icon.svg",
            "images/svg-test/menu-icon-close.svg",
            "images/svg-test/test-CS.svg",
            "images/svg-test/test-QQ1.svg",
        };
        String[] iconNames = {
            "home",
            "search",
            "checkout",
            "palette",
            "stories",
            "add",
            "star",
            "settings",
            "menu",
            "close",
            "C and S curves",
            "Q curves",
        };

        // Two rows of six icons, 48 by 48 points each.
        float y = xy[1] + 30f;
        for (int i = 0; i < iconFiles.length; i++) {
            float x = 50f + (i % 6) * 85f;
            float yIcon = y + (i / 6) * 90f;

            SVGImage icon = new SVGImage(iconFiles[i]);
            icon.setLocation(x, yIcon);
            float[] iconXY = icon.drawOn(page);

            text = new TextLine(f1, iconNames[i]);
            text.setFontSize(9f);
            text.setTextColor(Color.gray);
            text.setLocation(x, iconXY[1] + 15f);
            text.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_33();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_33 => %4d ms%n", time1 - time0);
    }
}   // End of Example_33.java
