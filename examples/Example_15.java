/*
 * Example_15.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_15.java
 * This example draws chemical formulas with CompositeTextLine: the digits of
 * a formula are subscripts, the charge of an ion and the mass number of an
 * isotope are superscripts.
 */
public class Example_15 {
    // The sections of the page: the formulas as CompositeTextLine.addFormula
    // reads them, and what each one is called.
    private static final String[][] COMPOUNDS = {
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

    private static final String[][] ORGANIC = {
        {"C6H12O6", "Glucose"},
        {"CH3COOH", "Acetic acid"},
        {"C8H10N4O2", "Caffeine"},
        {"C9H8O4", "Aspirin"},
        {"C2H5OH", "Ethanol"},
        {"CH3(CH2)14COOH", "Palmitic acid"},
    };

    private static final String[][] IONS = {
        {"Na^+", "Sodium"},
        {"Ca^2+", "Calcium"},
        {"NH4^+", "Ammonium"},
        {"OH^-", "Hydroxide"},
        {"SO4^2-", "Sulfate"},
        {"PO4^3-", "Phosphate"},
    };

    private static final String[][] ISOTOPES = {
        {"^3H", "Tritium"},
        {"^14C", "Carbon-14"},
        {"^235U", "Uranium-235"},
    };

    private static final String[][] REACTIONS = {
        {"2H2 + O2 → 2H2O", "Hydrogen burns"},
        {"N2 + 3H2 → 2NH3", "Ammonia, the Haber process"},
        {"CaCO3 → CaO + CO2", "Limestone is calcined"},
        {"CH4 + 2O2 → CO2 + 2H2O", "Methane burns"},
    };

    public Example_15() throws Exception {
        PDF pdf = new PDF(
            new BufferedOutputStream(new FileOutputStream("Example_15.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Chemical Formulas");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine title = new TextLine(f2, "Chemical Formulas");
        title.setFontSize(18f);
        title.setStructureType(StructElem.H1);
        title.setLocation(60f, 70f);
        title.drawOn(page);

        TextBlock intro = new TextBlock(f1,
                "A CompositeTextLine draws one line of text out of parts that sit on "
                + "the baseline, above it or below it. AddFormula reads a formula and "
                + "places its parts: the digits that follow an element or a bracket are "
                + "subscripts, and what follows a circumflex is a superscript, the "
                + "charge of an ion or the mass number of an isotope. Each formula "
                + "below is one line and, in this tagged document, one structure "
                + "element, so a screen reader reads the formula and not its parts.");
        intro.setFontSize(11f);
        intro.setLineSpacing(1.5f);
        intro.setLocation(60f, 90f);
        intro.setWidth(492f);
        float[] xy = intro.drawOn(page);

        float y = xy[1] + 26f;
        y = drawSection(page, f1, f2, "Compounds", COMPOUNDS, 2, 266f, 150f, y);
        y = drawSection(page, f1, f2, "Organic compounds", ORGANIC, 2, 266f, 150f, y);
        y = drawSection(page, f1, f2, "Ions", IONS, 3, 170f, 60f, y);
        y = drawSection(page, f1, f2, "Isotopes", ISOTOPES, 3, 170f, 60f, y);
        drawSection(page, f1, f2, "Reactions", REACTIONS, 1, 0f, 210f, y);

        pdf.complete();
    }

    // Draws the heading of a section and its formulas in the number of
    // columns, and returns the y where the next section begins.
    private float drawSection(Page page, Font f1, Font f2, String title,
            String[][] items, int columns, float columnWidth, float nameOffset, float y)
            throws Exception {
        TextLine heading = new TextLine(f2, title);
        heading.setFontSize(13f);
        heading.setStructureType(StructElem.H2);
        heading.setLocation(60f, y);
        heading.drawOn(page);

        y += 22f;
        for (int i = 0; i < items.length; i++) {
            float x = 70f + (i % columns)*columnWidth;
            drawFormula(page, f1, items[i][0], items[i][1], x, y + (i / columns)*24f, nameOffset);
        }
        int rows = (items.length + columns - 1) / columns;
        return y + rows*24f + 10f;
    }

    // Draws the formula at the location and its name the offset to the right of it.
    private void drawFormula(Page page, Font font, String formula, String name,
            float x, float y, float nameOffset) throws Exception {
        CompositeTextLine composite = new CompositeTextLine(x, y);
        composite.setFontSize(14f);
        composite.addFormula(font, formula);
        composite.drawOn(page);

        TextLine text = new TextLine(font, name);
        text.setFontSize(10f);
        text.setTextColor(Color.gray);
        text.setLocation(x + nameOffset, y);
        text.drawOn(page);
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_15();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_15 => %4d ms%n", time1 - time0);
    }
}   // End of Example_15.java
