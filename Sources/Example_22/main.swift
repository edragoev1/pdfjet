/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_22.swift
 * This example flows a guide to four countries onto pages with TextFlow: its
 * headings, paragraphs and maps are added in order, and TextFlow breaks the
 * paragraphs across pages between their lines, moves a map that does not fit
 * to the next page whole with its caption, and keeps each heading with the
 * text after it.
 */
public class Example_22 {
    static let countries = ["Spain", "Italy", "Switzerland", "Greece"]

    static let maps = [
        "resources/images/spain-admin.jpg", "resources/images/italy-admin.jpg", "resources/images/swiss-admin.jpg", "resources/images/gr-map.jpg"]

    static let texts = [
        [
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
        ],
        [
            "Italy has 20 regions. Five of them have a special statute and wider "
                    + "self-government: Sicily, Sardinia, Trentino-Alto Adige, Friuli Venezia "
                    + "Giulia and the Aosta Valley, islands or regions on a border, most with "
                    + "languages of their own.",
            "The regions are divided into provinces and metropolitan cities, and those "
                    + "into nearly 8,000 comuni, the municipalities, which run the local services "
                    + "from schools to roads.",
            "Two independent states lie within Italy: Vatican City, inside Rome, and "
                    + "San Marino, in the hills near the Adriatic coast.",
        ],
        [
            "Switzerland is a federation of 26 cantons. Each has its own constitution, "
                    + "parliament, government and courts, and the cantons keep every power the "
                    + "federal constitution does not give to the confederation.",
            "Bern is the seat of the federal government, called the federal city rather "
                    + "than the capital. Zurich is the largest city, and Geneva is the home of "
                    + "many international organizations.",
            "The country has four national languages: German, French, Italian and "
                    + "Romansh. Most cantons have one official language; Bern, Fribourg and "
                    + "Valais have two, and Graubünden has three.",
        ],
        [
            "Greece is divided into 13 regions, each with an elected governor and "
                    + "council, and the regions into 74 regional units and more than 300 "
                    + "municipalities.",
            "Mount Athos, a peninsula of monasteries in the north, is self-governing "
                    + "under the sovereignty of Greece.",
            "Greece has about 6,000 islands and islets, of which a little over 200 are "
                    + "inhabited. The largest, Crete, is a region of its own.",
        ],
    ]

    static let captions = [
        "Spain: its autonomous communities and provinces.",
        "Italy: its regions and provinces.",
        "Switzerland: its cantons.",
        "Greece: its regions.",
    ]

    static let altTexts = [
        "A map of Spain with its autonomous communities and provinces outlined and "
                + "named.",
        "A map of Italy with its regions and provinces outlined and named.",
        "A map of Switzerland with its cantons outlined and named.",
        "A map of Greece with its regions outlined and named.",
    ]

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_22.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Four Countries and Their Regions")

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(11.0)

        let f2 = try Font(pdf, IBMPlexSans.SemiBold)
        f2.setSize(16.0)

        let flow = TextFlow(pdf, Letter.PORTRAIT).setMargins(60.0)
        flow.add(Paragraph(TextLine(f2, "Four Countries and Their Regions").setFontSize(22.0))
                .setStructureType(StructElem.H1))
        flow.addSpace(12.0)
        flow.add(Paragraph(TextLine(f1,
                "This guide is drawn with TextFlow: its headings, paragraphs and maps are added "
                + "in order, and TextFlow puts them on as many pages as they need. A paragraph "
                + "breaks across pages between its lines, a map goes whole to the next page when "
                + "it does not fit, and a heading never ends a page. The maps are of the U.S. "
                + "government, in the public domain.")))

        for i in 0..<Example_22.countries.count {
            flow.addSpace(24.0)
            flow.add(Paragraph(TextLine(f2, Example_22.countries[i])).setStructureType(StructElem.H2))
            flow.addSpace(6.0)

            var paragraphs = [Paragraph]()
            for text in Example_22.texts[i] {
                paragraphs.append(Paragraph(TextLine(f1, text)))
            }
            flow.add(paragraphs, 8.0)
            flow.addSpace(12.0)

            let map = try Image(pdf, Example_22.maps[i])
            map.setAltDescription(Example_22.altTexts[i])
            map.resizeHeight(320.0)
            flow.add(map, Alignment.CENTER)
            flow.addSpace(4.0)
            flow.add(Paragraph(TextLine(f1, Example_22.captions[i]).setFontSize(9.0)
                    .setTextColor(Color.dimgray)).setTextAlignment(Alignment.CENTER)
                    .setStructureType(StructElem.CAPTION))
        }

        var pages = [Page]()
        flow.drawOn(&pages)
        for (i, page) in pages.enumerated() {
            page.addFooter(TextLine(f1, "Page \(i + 1) of \(pages.count)").setFontSize(9.0))
        }
        pdf.addPages(pages)

        try pdf.complete()
    }
}   // End of Example_22.swift


let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_22()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_22 => \(String(format: "%4lld", time1 - time0)) ms")
