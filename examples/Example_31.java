/*
 * Example_31.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;
import com.pdfjet.fonts.*;

/**
 * Example_31.java
 * This example draws with transparency. A GraphicsState sets the alpha of the
 * fills and of the strokes: opaque shapes hide what is under them, transparent
 * ones mix with it, and the stroke of a shape can be more or less transparent
 * than its fill.
 */
public class Example_31 {
    public Example_31() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(new FileOutputStream("Example_31.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("Transparency");

        Font f1 = new Font(pdf, IBMPlexSans.Regular);
        Font f2 = new Font(pdf, IBMPlexSans.SemiBold);

        Page page = new Page(pdf, Letter.PORTRAIT);

        TextLine text = new TextLine(f2, "Transparency");
        text.setStructureType(StructElem.H1);
        text.setFontSize(22f);
        text.setLocation(50f, 80f);
        text.drawOn(page);

        TextBlock textBlock = new TextBlock(f1,
                "A GraphicsState sets the alpha of the fills and of the strokes that "
                + "follow it, until the graphics state is restored. Opaque shapes hide "
                + "what is under them, transparent ones mix with it, and the stroke of "
                + "a shape can be more or less transparent than its fill.");
        textBlock.setFontSize(12f);
        textBlock.setLineSpacing(1.5f);
        textBlock.setLocation(50f, 95f);
        textBlock.setWidth(512f);
        float[] xy = textBlock.drawOn(page);

        float y = xy[1] + 30f;
        int[] colors = {Color.blue, Color.green, Color.red};

        // Opaque rectangles: each one hides the one under it.
        new TextLine(f2, "Opaque").setLocation(50f, y).drawOn(page);
        page.addArtifactBMC();  // The shapes carry no text, so they are artifacts.
        for (int i = 0; i < colors.length; i++) {
            page.setBrushColor(colors[i]);
            page.fillRect(50f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }
        page.addEMC();

        // Half transparent rectangles: the colors mix where they overlap.
        new TextLine(f2, "50% transparent").setLocation(320f, y).drawOn(page);
        page.addArtifactBMC();
        page.saveGraphicsState();
        GraphicsState gs = new GraphicsState();
        gs.setAlphaStroking(0.5f);      // The stroking alpha constant
        gs.setAlphaNonStroking(0.5f);   // The non-stroking alpha constant
        page.setGraphicsState(gs);
        for (int i = 0; i < colors.length; i++) {
            page.setBrushColor(colors[i]);
            page.fillRect(320f + i * 60f, y + 15f + i * 30f, 120f, 120f);
        }
        page.restoreGraphicsState();
        page.addEMC();

        // The same blue over a gray bar at four levels of alpha.
        y += 245f;
        new TextLine(f2, "Fill alpha").setLocation(50f, y).drawOn(page);
        page.addArtifactBMC();
        page.setBrushColor(Color.gray);
        page.fillRect(50f, y + 55f, 506f, 30f);
        page.addEMC();
        float[] alphas = {0.25f, 0.5f, 0.75f, 1f};
        for (int i = 0; i < alphas.length; i++) {
            float x = 50f + i * 132f;
            page.addArtifactBMC();
            page.saveGraphicsState();
            gs = new GraphicsState();
            gs.setAlphaNonStroking(alphas[i]);
            page.setGraphicsState(gs);
            page.setBrushColor(Color.blue);
            page.fillRect(x, y + 15f, 110f, 110f);
            page.restoreGraphicsState();
            page.addEMC();

            text = new TextLine(f1, Math.round(alphas[i] * 100f) + "%");
            text.setFontSize(10f);
            text.setTextColor(Color.gray);
            text.setLocation(x, y + 140f);
            text.drawOn(page);
        }

        // A thick stroke and a fill, each transparent on its own: the stroke
        // shows the fill through it, then the fill shows the stroke.
        y += 175f;
        new TextLine(f2, "Stroke alpha and fill alpha").setLocation(50f, y).drawOn(page);
        String[] labels = {
            "Stroke 25%, fill 100%",
            "Stroke 100%, fill 25%",
            "Stroke 50%, fill 50%",
        };
        float[][] strokeAndFill = {{0.25f, 1f}, {1f, 0.25f}, {0.5f, 0.5f}};
        for (int i = 0; i < labels.length; i++) {
            float x = 100f + i * 180f;
            page.addArtifactBMC();
            page.saveGraphicsState();
            gs = new GraphicsState();
            gs.setAlphaStroking(strokeAndFill[i][0]);
            gs.setAlphaNonStroking(strokeAndFill[i][1]);
            page.setGraphicsState(gs);
            page.setBrushColor(Color.red);
            page.fillCircle(x, y + 65f, 40f);
            page.setPenColor(Color.blue);
            page.setPenWidth(16f);
            page.drawCircle(x, y + 65f, 40f);
            page.restoreGraphicsState();
            page.addEMC();

            text = new TextLine(f1, labels[i]);
            text.setFontSize(10f);
            text.setTextColor(Color.gray);
            text.setLocation(x - text.getWidth() / 2f, y + 130f);
            text.drawOn(page);
        }

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_31();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_31 => %4d ms%n", time1 - time0);
    }
}   // End of Example_31.java
