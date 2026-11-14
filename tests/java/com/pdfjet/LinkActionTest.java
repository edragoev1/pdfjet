/*
 * LinkActionTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.io.ByteArrayOutputStream;
import java.util.HashSet;
import java.util.Set;
import java.util.regex.Matcher;
import java.util.regex.Pattern;

import org.junit.jupiter.api.Test;

// The links to one address share one action, which holds the address: each
// link carried it, and twenty links to an address of 20,000 characters added
// about 800 KB. Each link keeps its own description.
class LinkActionTest {
    private static int count(String text, String part) {
        int n = 0;
        for (int i = text.indexOf(part); i >= 0; i = text.indexOf(part, i + 1)) {
            n++;
        }
        return n;
    }

    @Test
    void linksToOneAddressShareOneAction() throws Exception {
        StringBuilder sb = new StringBuilder("https://pdfjet.com/");
        for (int i = 0; i < 300; i++) {
            sb.append('a');
        }
        String address = sb.toString();
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos);
        Font font = TestSupport.plexSans(pdf);
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(font, "One").setURIAction(address).setLocation(50f, 100f).drawOn(page);
        new TextLine(font, "Two").setURIAction(address).setLocation(50f, 120f).drawOn(page);
        new TextLine(font, "Site").setURIAction("https://pdfjet.com").setLocation(50f, 140f).drawOn(page);
        pdf.complete();
        String raw = TestSupport.latin1(bos.toByteArray());
        assertEquals(3, count(raw, "/Subtype /Link"), raw);
        assertEquals(2, count(raw, "/S /URI\n"), raw);
        Matcher m = Pattern.compile("/A (\\d+) 0 R\n").matcher(raw);
        int links = 0;
        Set<String> actions = new HashSet<String>();
        while (m.find()) {
            links++;
            actions.add(m.group(1));
        }
        assertEquals(3, links);
        assertEquals(2, actions.size());
    }
}
