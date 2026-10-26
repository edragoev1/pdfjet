/*
 * Example_22.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_22.cs
 * This example flows a guide to four countries onto pages with TextFlow: its
 * headings, paragraphs and maps are added in order, and TextFlow breaks the
 * paragraphs across pages between their lines, moves a map that does not fit
 * to the next page whole with its caption, and keeps each heading with the
 * text after it.
 */
public class Example_22 {
    static readonly string[] COUNTRIES = {"Spain", "Italy", "Switzerland", "Greece"};

    static readonly string[] MAPS = {
        "images/spain-admin.jpg", "images/italy-admin.jpg", "images/swiss-admin.jpg", "images/gr-map.jpg"};

    static readonly string[][] TEXT = {
        new string[] {
            "Spain is divided into 17 autonomous communities and two autonomous cities, "
                    + "Ceuta and Melilla, on the coast of North Africa. The communities differ in "
                    + "their powers: some, such as the Basque Country and Navarre, collect most "
                    + "of their own taxes.",
            "Within the communities are 50 provinces, the units of the national "
                    + "government since 1833, and more than 8,000 municipalities. Madrid is both "
                    + "the capital of the country and an autonomous community of its own.",
            "Spanish is the official language of the whole country. Catalan, Galician "
                    + "and Basque are official too in the communities where they are spoken, and "
                    + "their schools and regional governments use them.",
        },
        new string[] {
            "Italy has 20 regions. Five of them have a special statute and wider "
                    + "self-government: Sicily, Sardinia, Trentino-Alto Adige, Friuli Venezia "
                    + "Giulia and the Aosta Valley, islands or regions on a border, most with "
                    + "languages of their own.",
            "The regions are divided into provinces and metropolitan cities, and those "
                    + "into nearly 8,000 comuni, the municipalities, which run the local services "
                    + "from schools to roads.",
            "Two independent states lie within Italy: Vatican City, inside Rome, and "
                    + "San Marino, in the hills near the Adriatic coast.",
        },
        new string[] {
            "Switzerland is a federation of 26 cantons. Each has its own constitution, "
                    + "parliament, government and courts, and the cantons keep every power the "
                    + "federal constitution does not give to the confederation.",
            "Bern is the seat of the federal government, called the federal city rather "
                    + "than the capital. Zurich is the largest city, and Geneva is the home of "
                    + "many international organizations.",
            "The country has four national languages: German, French, Italian and "
                    + "Romansh. Most cantons have one official language; Bern, Fribourg and "
                    + "Valais have two, and Graubünden has three.",
        },
        new string[] {
            "Greece is divided into 13 regions, each with an elected governor and "
                    + "council, and the regions into 74 regional units and more than 300 "
                    + "municipalities.",
            "Mount Athos, a peninsula of monasteries in the north, is self-governing "
                    + "under the sovereignty of Greece.",
            "Greece has about 6,000 islands and islets, of which a little over 200 are "
                    + "inhabited. The largest, Crete, is a region of its own.",
        },
    };

    static readonly string[] CAPTIONS = {
        "Spain: its autonomous communities and provinces.",
        "Italy: its regions and provinces.",
        "Switzerland: its cantons.",
        "Greece: its regions.",
    };

    static readonly string[] ALT_TEXT = {
        "A map of Spain with its autonomous communities and provinces outlined and "
                + "named.",
        "A map of Italy with its regions and provinces outlined and named.",
        "A map of Switzerland with its cantons outlined and named.",
        "A map of Greece with its regions outlined and named.",
    };

    public Example_22() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_22.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Four Countries and Their Regions");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        f1.SetSize(11f);

        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);
        f2.SetSize(16f);

        TextFlow flow = new TextFlow(pdf, Letter.PORTRAIT).SetMargins(60f);
        flow.Add(new Paragraph(new TextLine(f2, "Four Countries and Their Regions").SetFontSize(22f))
                .SetStructureType(StructElem.H1));
        flow.AddSpace(12f);
        flow.Add(new Paragraph(new TextLine(f1,
                "This guide is drawn with TextFlow: its headings, paragraphs and maps are added "
                + "in order, and TextFlow puts them on as many pages as they need. A paragraph "
                + "breaks across pages between its lines, a map goes whole to the next page when "
                + "it does not fit, and a heading never ends a page. The maps are of the U.S. "
                + "government, in the public domain.")));

        for (int i = 0; i < COUNTRIES.Length; i++) {
            flow.AddSpace(24f);
            flow.Add(new Paragraph(new TextLine(f2, COUNTRIES[i])).SetStructureType(StructElem.H2));
            flow.AddSpace(6f);

            List<Paragraph> paragraphs = new List<Paragraph>();
            foreach (String text in TEXT[i]) {
                paragraphs.Add(new Paragraph(new TextLine(f1, text)));
            }
            flow.Add(paragraphs, 8f);
            flow.AddSpace(12f);

            Image map = new Image(pdf, MAPS[i]);
            map.SetAltDescription(ALT_TEXT[i]);
            map.ResizeHeight(320f);
            flow.Add(map, Alignment.CENTER);
            flow.AddSpace(4f);
            flow.Add(new Paragraph(new TextLine(f1, CAPTIONS[i]).SetFontSize(9f)
                    .SetTextColor(Color.dimgray)).SetTextAlignment(Alignment.CENTER)
                    .SetStructureType(StructElem.CAPTION));
        }

        List<Page> pages = new List<Page>();
        flow.DrawOn(pages);
        for (int i = 0; i < pages.Count; i++) {
            pages[i].AddFooter(new TextLine(f1, "Page " + (i + 1) + " of " + pages.Count).SetFontSize(9f));
        }
        pdf.AddPages(pages);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_22();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_22 => {time1 - time0,4} ms");
    }
}   // End of Example_22.cs
