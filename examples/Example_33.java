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
 * and a bar chart made with D3, its text in the fonts of the document.
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
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 865 points wide "
                + "in its file and is scaled by 0.5 to fit the page.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(495f);
        float[] xy = textBlock.drawOn(page);

        SVGImage map = new SVGImage("images/svg-test/europe.svg");
        map.setAltDescription(
                "A map of Europe drawn from an SVG file, the countries filled in peachpuff and outlined in dim gray, Spain in aliceblue and Austria outlined in light gray.");
        map.scaleBy(0.5f);
        // In the middle of the page, and the text under it as wide as it
        float mapX = (page.getWidth() - map.getWidth()) / 2f;
        map.setLocation(mapX, xy[1] + 20f);
        xy = map.drawOn(page);

        textBlock = new TextBlock(f1,
                "The colors are set in the SVG file itself. The svg element gives every "
                + "country a peachpuff fill and a dimgray outline; Spain overrides the fill "
                + "with aliceblue, and Austria the outline with lightgray.");
        textBlock.setFontSize(10f);
        textBlock.setTextColor(Color.dimgray);
        textBlock.setLocation(mapX, xy[1] + 10f);
        textBlock.setWidth(map.getWidth());
        xy = textBlock.drawOn(page);

        // A chart made with D3, as a browser draws it, by util/make-d3-chart.mjs:
        // its text, in sans-serif, is drawn in IBM Plex Sans, which PDFjet
        // carries and embeds, as PDF/UA asks; on a page of its own
        page = new Page(pdf, A4.PORTRAIT);
        new TextLine(f2, "A Chart Made with D3")
                .setStructureType(StructElem.H2)
                .setFontSize(22f)
                .setLocation(50f, 80f)
                .drawOn(page);

        textBlock = new TextBlock(f1,
                "SVGImage draws the charts of D3 and the like, their text in the fonts "
                + "PDFjet carries, embedded: IBM Plex Sans for "
                + "sans-serif, IBM Plex Serif for serif and Cascadia Mono for monospace. "
                + "This chart was made by D3 in util/make-d3-chart.mjs.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(495f);
        xy = textBlock.drawOn(page);

        SVGImage chart = new SVGImage("images/svg/d3-life-expectancy-by-region.svg");
        chart.setAltDescription("A bar chart of the life expectancy at birth of the World Bank's "
                + "regions in 2023: North America 78.7 years, Europe and Central Asia 78.4, "
                + "East Asia and Pacific 76.7, Latin America and the Caribbean 75.6, South Asia 72.3, "
                + "the Middle East, North Africa, Afghanistan and Pakistan 72.1, "
                + "and Sub-Saharan Africa 62.6; the world 73.3.");
        chart.scaleBy(495f / chart.getWidth());
        chart.setLocation(50f, xy[1] + 20f);
        chart.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_33();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_33 => %4d ms%n", time1 - time0);
    }
}   // End of Example_33.java
