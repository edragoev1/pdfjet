/*
 * Example_33.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_33.cs
 * Using the SVGImage component to draw a map of Europe, scaled to fit the page,
 * and a bar chart made with D3, its text in the fonts of the document.
 */
public class Example_33 {
    public Example_33() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_33.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("SVG Images");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, A4.PORTRAIT);

        TextLine text = new TextLine(f2, "SVG Images");
        text.SetStructureType(StructElem.H1);
        text.SetFontSize(22f);
        text.SetLocation(50f, 80f);
        text.DrawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "SVGImage reads the paths of an SVG file and draws them as vector "
                + "graphics, which stay sharp at any zoom. The map is 865 points wide "
                + "in its file and is scaled by 0.5 to fit the page.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(495f);
        float[] xy = textBlock.DrawOn(page);

        SVGImage map = new SVGImage("images/svg-test/europe.svg");
        map.SetAltDescription(
                "A map of Europe drawn from an SVG file, the countries filled in peachpuff and outlined in dim gray, Spain in aliceblue and Austria outlined in light gray.");
        map.ScaleBy(0.5f);
        // In the middle of the page, and the text under it as wide as it
        float mapX = (page.GetWidth() - map.GetWidth()) / 2f;
        map.SetLocation(mapX, xy[1] + 20f);
        xy = map.DrawOn(page);

        textBlock = new TextBlock(f1,
                "The colors are set in the SVG file itself. The svg element gives every "
                + "country a peachpuff fill and a dimgray outline; Spain overrides the fill "
                + "with aliceblue, and Austria the outline with lightgray.");
        textBlock.SetFontSize(10f);
        textBlock.SetTextColor(Color.dimgray);
        textBlock.SetLocation(mapX, xy[1] + 10f);
        textBlock.SetWidth(map.GetWidth());
        xy = textBlock.DrawOn(page);

        // A chart made with D3, as a browser draws it, by util/make-d3-chart.mjs:
        // its text, in sans-serif, is drawn in IBM Plex Sans, which PDFjet
        // carries and embeds, as PDF/UA asks; on a page of its own
        page = new Page(pdf, A4.PORTRAIT);
        new TextLine(f2, "A Chart Made with D3")
                .SetStructureType(StructElem.H2)
                .SetFontSize(22f)
                .SetLocation(50f, 80f)
                .DrawOn(page);

        textBlock = new TextBlock(f1,
                "SVGImage draws the charts of D3 and the like, their text in the fonts "
                + "PDFjet carries, those of PDFjet Forms, embedded: IBM Plex Sans for "
                + "sans-serif, Source Serif 4 for serif and JetBrains Mono for monospace. "
                + "This chart was made by D3 in util/make-d3-chart.mjs.");
        textBlock.SetFontSize(12f);
        textBlock.SetLineSpacing(1.5f);
        textBlock.SetLocation(50f, 95f);
        textBlock.SetWidth(495f);
        xy = textBlock.DrawOn(page);

        SVGImage chart = new SVGImage("images/svg/d3-life-expectancy-by-region.svg");
        chart.SetAltDescription("A bar chart of the life expectancy at birth of the World Bank's "
                + "regions in 2023: North America 78.7 years, Europe and Central Asia 78.4, "
                + "East Asia and Pacific 76.7, Latin America and the Caribbean 75.6, South Asia 72.3, "
                + "the Middle East, North Africa, Afghanistan and Pakistan 72.1, "
                + "and Sub-Saharan Africa 62.6; the world 73.3.");
        chart.ScaleBy(495f / chart.GetWidth());
        chart.SetLocation(50f, xy[1] + 20f);
        chart.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_33();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_33 => {time1 - time0,4} ms");
    }
}   // End of Example_33.cs
