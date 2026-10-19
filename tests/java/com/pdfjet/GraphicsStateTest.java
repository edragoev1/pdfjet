/*
 * GraphicsStateTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;
import java.nio.charset.StandardCharsets;
import java.util.List;
import org.junit.jupiter.api.Test;

/**
 * The blend mode of a graphics state, and a state set again on a page that
 * was read (from Stefan Ostermann's fork of PDFjet, 19 October 2026).
 */
class GraphicsStateTest {
    private static String complete(PDF pdf, ByteArrayOutputStream bos) throws Exception {
        pdf.complete();
        return new String(bos.toByteArray(), StandardCharsets.ISO_8859_1);
    }

    @Test
    void theBlendModeIsWrittenInTheState() throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Page page = new Page(pdf, Letter.PORTRAIT);
        page.setGraphicsState(new GraphicsState().setAlphaNonStroking(0.5f).setBlendMode(BlendMode.MULTIPLY));
        page.setGraphicsState(new GraphicsState().setAlphaNonStroking(0.5f));
        page.setGraphicsState(new GraphicsState().setAlphaNonStroking(0.5f).setBlendMode(BlendMode.MULTIPLY));
        page.setGraphicsState(new GraphicsState().setBlendMode(BlendMode.COLOR_DODGE));
        assertEquals("/GS1 gs\n/GS2 gs\n/GS1 gs\n/GS3 gs\n", TestSupport.content(page).replaceAll("(?s)^.*?(/GS1 gs)", "$1"));
        String out = complete(pdf, bos);
        assertTrue(out.contains("/GS1 <</CA 1 /ca 0.5 /BM /Multiply>>"), out);
        assertTrue(out.contains("/GS2 <</CA 1 /ca 0.5>>"), out);
        assertTrue(out.contains("/GS3 <</CA 1 /ca 1 /BM /ColorDodge>>"), out);
    }

    @Test
    void aStateWithoutABlendModeIsWrittenAsBefore() throws Exception {
        GraphicsState gs = new GraphicsState().setAlphaStroking(0.25f);
        assertEquals(null, gs.getBlendMode());
        assertEquals("/CA 0.25 /ca 1", gs.entries());
        gs.setBlendMode(BlendMode.MULTIPLY).setBlendMode(null);
        assertEquals("/CA 0.25 /ca 1", gs.entries());
    }

    @Test
    void pdfA1TakesOnlyTheNormalBlendMode() throws Exception {
        ReviewMediaTest.Drawing multiply = (pdf, page) ->
                page.setGraphicsState(new GraphicsState().setBlendMode(BlendMode.MULTIPLY));
        assertEquals("A document of PDF_A_1B cannot draw with the blend mode Multiply: "
                + "PDF/A-1 has no transparency.",
                ReviewMediaTest.pdfaDrawError(Compliance.PDF_A_1B, multiply)[0]);
        assertEquals("", ReviewMediaTest.pdfaDrawError(Compliance.PDF_A_2B, multiply)[0]);
        assertEquals("", ReviewMediaTest.pdfaDrawError(Compliance.PDF_A_1B, (pdf, page) ->
                page.setGraphicsState(new GraphicsState().setBlendMode(BlendMode.NORMAL)))[0]);
    }

    // A page of a PDF that was read, with a state of its own named /Gs1.
    private static byte[] pageWithAState() throws Exception {
        String[] objects = {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 300 200] "
                    + "/Resources << /ExtGState << /Gs1 << /CA 1 /ca 1 >> >> >> /Contents 4 0 R >>",
        };
        return TestSupport.minimalPDF(objects, "/Gs1 gs");
    }

    @Test
    void theSameStateSetAgainOnAPageThatWasReadIsReused() throws Exception {
        List<PDFobj> objects = TestSupport.read(pageWithAState());
        PDF pdf = new PDF(new ByteArrayOutputStream());
        PDFobj page = pdf.getPageObjects(objects).get(0);
        GraphicsState highlighter = new GraphicsState().setAlphaNonStroking(0.5f).setBlendMode(BlendMode.MULTIPLY);
        page.setGraphicsState(highlighter, objects);
        page.setGraphicsState(new GraphicsState().setAlphaNonStroking(0.5f), objects);
        page.setGraphicsState(highlighter, objects);
        String dict = String.join(" ", page.dict);
        assertTrue(dict.contains("/GS1 << /CA 1 /ca 0.5 /BM /Multiply >>"), dict);
        assertTrue(dict.contains("/GS2 << /CA 1 /ca 0.5 >>"), dict);
        assertTrue(!dict.contains("/GS3"), dict);
        assertTrue(dict.contains("/Gs1 << /CA 1 /ca 1 >>"), dict);
    }
}
