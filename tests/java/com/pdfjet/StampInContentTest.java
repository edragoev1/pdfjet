/*
 * StampInContentTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertTrue;

import java.io.ByteArrayOutputStream;

import org.junit.jupiter.api.Test;

// A stamp with no description of its own, drawn inside the real content of a
// tagged page, is part of that content, not an artifact inside it (ISO
// 14289-1, 7.1): letters drawn once as stamps and placed in a paragraph.
class StampInContentTest {

    private static Stamp letter(PDF pdf) throws Exception {
        Stamp stamp = new Stamp(pdf).setSize(6f, 8f);
        stamp.moveTo(0f, 8f).lineTo(3f, 0f).lineTo(6f, 8f).fillPath();
        stamp.complete();
        return stamp;
    }

    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    @Test
    void anUndescribedStampInAParagraphIsPartOfIt() throws Exception {
        TestSupport.allowCoreFonts(true);
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, Compliance.PDF_UA_1).setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = letter(pdf);
        page.addBDC(StructElem.P, "km", "AAA", "AAA");
        for (int i = 0; i < 3; i++) {
            stamp.setLocation(50f + 7f * i, 50f);
            stamp.drawOn(page);
        }
        page.addEMC();
        String content = TestSupport.content(page);
        assertFalse(content.contains("/Artifact BMC"), content);
        assertEquals(1, count(content, "BDC\n"), content);
        assertEquals(1, count(content, "EMC\n"), content);
        assertEquals(3, count(content, " Do\n"), content);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertTrue(raw.contains("/ActualText"), "the paragraph keeps its text");
    }

    @Test
    void anUndescribedStampOutsideContentIsStillAnArtifact() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = letter(pdf);
        stamp.setLocation(50f, 50f);
        stamp.drawOn(page);
        String content = TestSupport.content(page);
        assertTrue(content.contains("/Artifact BMC\n"), content);
        assertEquals(1, count(content, "EMC\n"), content);
    }

    @Test
    void aDescribedStampInAParagraphIsAnElementOfItsOwn() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream(), Compliance.PDF_UA_1).setTitle("Title");
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = letter(pdf);
        stamp.setAltDescription("A logo");
        page.addBDC(StructElem.P, "en", "Text", "Text");
        stamp.setLocation(50f, 50f);
        stamp.drawOn(page);
        page.addEMC();
        String content = TestSupport.content(page);
        assertEquals(2, count(content, "EMC\n"), content);
    }

    @Test
    void anUntaggedPageMarksNothing() throws Exception {
        PDF pdf = new PDF(new ByteArrayOutputStream());
        Page page = new Page(pdf, Letter.PORTRAIT);
        Stamp stamp = letter(pdf);
        page.addBDC(StructElem.P, "km", "A", "A");
        stamp.setLocation(50f, 50f);
        stamp.drawOn(page);
        page.addEMC();
        String content = TestSupport.content(page);
        assertFalse(content.contains("BMC") || content.contains("BDC") || content.contains("EMC"), content);
    }
}
