/*
 * Example_15.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 * Example_15.cs
 * This example draws chemical formulas with CompositeTextLine: the digits of
 * a formula are subscripts, the charge of an ion and the mass number of an
 * isotope are superscripts.
 */
public class Example_15 {
    // The sections of the page: the formulas as CompositeTextLine.AddFormula
    // reads them, and what each one is called.
    private static readonly string[,] COMPOUNDS = {
        {"H2O", "Water"},
        {"CO2", "Carbon dioxide"},
        {"NaCl", "Sodium chloride"},
        {"H2SO4", "Sulfuric acid"},
        {"NaHCO3", "Sodium bicarbonate"},
        {"Fe2O3", "Iron(III) oxide"},
        {"Ca(OH)2", "Calcium hydroxide"},
        {"CuSO4·5H2O", "Copper(II) sulfate"},
        {"(NH4)3PO4", "Ammonium phosphate"},
        {"KAl(SO4)2", "Potassium alum"},
    };

    private static readonly string[,] ORGANIC = {
        {"C6H12O6", "Glucose"},
        {"CH3COOH", "Acetic acid"},
        {"C8H10N4O2", "Caffeine"},
        {"C9H8O4", "Aspirin"},
        {"C2H5OH", "Ethanol"},
        {"CH3(CH2)14COOH", "Palmitic acid"},
    };

    private static readonly string[,] IONS = {
        {"Na^+", "Sodium"},
        {"Ca^2+", "Calcium"},
        {"NH4^+", "Ammonium"},
        {"OH^-", "Hydroxide"},
        {"SO4^2-", "Sulfate"},
        {"PO4^3-", "Phosphate"},
    };

    private static readonly string[,] ISOTOPES = {
        {"^3H", "Tritium"},
        {"^14C", "Carbon-14"},
        {"^235U", "Uranium-235"},
    };

    private static readonly string[,] REACTIONS = {
        {"2H2 + O2 → 2H2O", "Hydrogen burns"},
        {"N2 + 3H2 → 2NH3", "Ammonia, the Haber process"},
        {"CaCO3 → CaO + CO2", "Limestone is calcined"},
        {"CH4 + 2O2 → CO2 + 2H2O", "Methane burns"},
    };

    public Example_15() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_15.pdf", FileMode.Create)));
        pdf.SetCompliance(Compliance.PDF_UA_1);
        pdf.SetTitle("Chemical Formulas");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f2, "Chemical Formulas");
        title.SetFontSize(18f);
        title.SetStructureType(StructElem.H1);
        title.SetLocation(60f, 70f);
        title.DrawOn(page);

        TextBlock intro = new TextBlock(f1,
                "A CompositeTextLine draws one line of text out of parts that sit on "
                + "the baseline, above it or below it. AddFormula reads a formula and "
                + "places its parts: the digits that follow an element or a bracket are "
                + "subscripts, and what follows a circumflex is a superscript, the "
                + "charge of an ion or the mass number of an isotope. Each formula "
                + "below is one line and, in this tagged document, one structure "
                + "element, so a screen reader reads the formula and not its parts.");
        intro.SetFontSize(11f);
        intro.SetLineSpacing(1.5f);
        intro.SetLocation(60f, 90f);
        intro.SetWidth(492f);
        float[] xy = intro.DrawOn(page);

        float y = xy[1] + 26f;
        y = DrawSection(page, f1, f2, "Compounds", COMPOUNDS, 2, 266f, 150f, y);
        y = DrawSection(page, f1, f2, "Organic compounds", ORGANIC, 2, 266f, 150f, y);
        y = DrawSection(page, f1, f2, "Ions", IONS, 3, 170f, 60f, y);
        y = DrawSection(page, f1, f2, "Isotopes", ISOTOPES, 3, 170f, 60f, y);
        DrawSection(page, f1, f2, "Reactions", REACTIONS, 1, 0f, 210f, y);

        pdf.Complete();
    }

    // Draws the heading of a section and its formulas in the number of
    // columns, and returns the y where the next section begins.
    private float DrawSection(Page page, Font f1, Font f2, string title,
            string[,] items, int columns, float columnWidth, float nameOffset, float y) {
        TextLine heading = new TextLine(f2, title);
        heading.SetFontSize(13f);
        heading.SetStructureType(StructElem.H2);
        heading.SetLocation(60f, y);
        heading.DrawOn(page);

        y += 22f;
        int count = items.GetLength(0);
        for (int i = 0; i < count; i++) {
            float x = 70f + (i % columns)*columnWidth;
            DrawFormula(page, f1, items[i, 0], items[i, 1], x, y + (i / columns)*24f, nameOffset);
        }
        int rows = (count + columns - 1) / columns;
        return y + rows*24f + 10f;
    }

    // Draws the formula at the location and its name the offset to the right of it.
    private void DrawFormula(Page page, Font font, string formula, string name,
            float x, float y, float nameOffset) {
        CompositeTextLine composite = new CompositeTextLine(x, y);
        composite.SetFontSize(14f);
        composite.AddFormula(font, formula);
        composite.DrawOn(page);

        TextLine text = new TextLine(font, name);
        text.SetFontSize(10f);
        text.SetTextColor(Color.gray);
        text.SetLocation(x + nameOffset, y);
        text.DrawOn(page);
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_15();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        Console.WriteLine($"Example_15 => {time1 - time0,4} ms");
    }
}   // End of Example_15.cs
