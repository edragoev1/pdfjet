/*
 * Example_22.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import java.util.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_22.java
 * This example flows a guide to four countries onto pages with TextFlow: its
 * headings, paragraphs and maps are added in order, and TextFlow breaks the
 * paragraphs across pages between their lines, moves a map that does not fit
 * to the next page whole with its caption, and keeps each heading with the
 * text after it.
 */
public class Example_22 {
    static final String[] COUNTRIES = {"Spain", "Italy", "Switzerland", "Greece"};

    static final String[] MAPS = {
        "images/spain-admin.jpg", "images/italy-admin.jpg", "images/swiss-admin.jpg", "images/gr-map.jpg"};

    static final String[][] TEXT = {
        {
            "Spain is divided into 17 autonomous communities and two autonomous cities, Ceuta and "
                    + "Melilla, on the coast of North Africa. The communities differ in their powers: "
                    + "some, such as the Basque Country and Navarre, collect most of their own taxes.",
            "Within the communities are 50 provinces, the units of the national government since "
                    + "1833, and more than 8,000 municipalities. Madrid is both the capital of the "
                    + "country and an autonomous community of its own.",
            "Spanish is the official language of the whole country. Catalan, Galician and Basque are "
                    + "official too in the communities where they are spoken, and their schools and "
                    + "regional governments use them.",
        },
        {
            "Italy has 20 regions. Five of them have a special statute and wider self-government: "
                    + "Sicily, Sardinia, Trentino-Alto Adige, Friuli Venezia Giulia and the Aosta "
                    + "Valley, islands or regions on a border, most with languages of their own.",
            "The regions are divided into provinces and metropolitan cities, and those into nearly "
                    + "8,000 comuni, the municipalities, which run the local services from schools "
                    + "to roads.",
            "Two independent states lie within Italy: Vatican City, inside Rome, and San Marino, in "
                    + "the hills near the Adriatic coast.",
        },
        {
            "Switzerland is a federation of 26 cantons. Each has its own constitution, parliament, "
                    + "government and courts, and the cantons keep every power the federal "
                    + "constitution does not give to the confederation.",
            "Bern is the seat of the federal government, called the federal city rather than the "
                    + "capital. Zurich is the largest city, and Geneva is the home of many "
                    + "international organizations.",
            "The country has four national languages: German, French, Italian and Romansh. Most "
                    + "cantons have one official language; Bern, Fribourg and Valais have two, and "
                    + "Graubünden has three.",
        },
        {
            "Greece is divided into 13 regions, each with an elected governor and council, and the "
                    + "regions into 74 regional units and more than 300 municipalities.",
            "Mount Athos, a peninsula of monasteries in the north, is self-governing under the "
                    + "sovereignty of Greece.",
            "Greece has about 6,000 islands and islets, of which a little over 200 are inhabited. "
                    + "The largest, Crete, is a region of its own.",
        },
    };

    static final String[] CAPTIONS = {
        "Spain: its autonomous communities and provinces.",
        "Italy: its regions and provinces.",
        "Switzerland: its cantons.",
        "Greece: its regions.",
    };

    static final String[] ALT_TEXT = {
        "A map of Spain with its autonomous communities and provinces outlined and named.",
        "A map of Italy with its regions and provinces outlined and named.",
        "A map of Switzerland with its cantons outlined and named.",
        "A map of Greece with its regions outlined and named.",
    };

    public Example_22() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_22.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Four Countries and Their Regions");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.setSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.setSize(16f);

        TextFlow flow = new TextFlow(pdf, Letter.PORTRAIT).setMargins(60f);
        flow.add(new Paragraph(new TextLine(f2, "Four Countries and Their Regions").setFontSize(22f))
                .setStructureType(StructElem.H1));
        flow.addSpace(12f);
        flow.add(new Paragraph(new TextLine(f1,
                "This guide is drawn with TextFlow: its headings, paragraphs and maps are added in "
                + "order, and TextFlow puts them on as many pages as they need. A paragraph breaks "
                + "across pages between its lines, a map goes whole to the next page when it does "
                + "not fit, and a heading never ends a page. The maps are of the U.S. government, "
                + "in the public domain.")));

        for (int i = 0; i < COUNTRIES.length; i++) {
            flow.addSpace(24f);
            flow.add(new Paragraph(new TextLine(f2, COUNTRIES[i])).setStructureType(StructElem.H2));
            flow.addSpace(6f);

            List<Paragraph> paragraphs = new ArrayList<Paragraph>();
            for (String text : TEXT[i]) {
                paragraphs.add(new Paragraph(new TextLine(f1, text)));
            }
            flow.add(paragraphs, 8f);
            flow.addSpace(12f);

            Image map = new Image(pdf, MAPS[i]);
            map.setAltDescription(ALT_TEXT[i]);
            map.resizeHeight(320f);
            flow.add(map, Alignment.CENTER);
            flow.addSpace(4f);
            flow.add(new Paragraph(new TextLine(f1, CAPTIONS[i]).setFontSize(9f)
                    .setTextColor(Color.dimgray)).setTextAlignment(Alignment.CENTER)
                    .setStructureType(StructElem.CAPTION));
        }

        List<Page> pages = new ArrayList<Page>();
        flow.drawOn(pages);
        for (int i = 0; i < pages.size(); i++) {
            pages.get(i).addFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.size()).setFontSize(9f));
        }
        pdf.addPages(pages);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_22();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_22 => %4d ms%n", time1 - time0);
    }
}   // End of Example_22.java
