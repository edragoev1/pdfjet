/*
 * EncryptionTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertFalse;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Compliance;
import com.pdfjet.CoreFont;
import com.pdfjet.Encryption;
import com.pdfjet.Font;
import com.pdfjet.Letter;
import com.pdfjet.PDF;
import com.pdfjet.PDFobj;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import com.pdfjet.TextLine;
import java.io.ByteArrayOutputStream;
import java.util.List;
import java.util.regex.Matcher;
import java.util.regex.Pattern;
import org.junit.jupiter.api.Test;

/** AES-256 encryption with the standard security handler, read back with the passwords. */
class EncryptionTest {
    private static byte[] encrypted(Compliance compliance, Passwords passwords, Permissions permissions) throws Exception {
        ByteArrayOutputStream bos = new ByteArrayOutputStream();
        PDF pdf = new PDF(bos, compliance);
        pdf.setTitle("Encrypted title");
        pdf.setEncryption(new Encryption(pdf, passwords, permissions));
        Page page = new Page(pdf, Letter.PORTRAIT);
        new TextLine(new Font(pdf, CoreFont.HELVETICA), "Secret text").setLocation(50f, 50f).drawOn(page);
        pdf.complete();
        return bos.toByteArray();
    }

    private static byte[] encrypted(String user, String owner) throws Exception {
        return encrypted(Compliance.PDF_1_7,
                new Passwords().setUserPassword(user).setOwnerPassword(owner),
                new Permissions().grant(UserAccess.PRINT.getValue()));
    }

    private static int accessValue(byte[] pdf) {
        Matcher matcher = Pattern.compile("/P (-?\\d+)").matcher(TestSupport.latin1(pdf));
        assertTrue(matcher.find());
        return Integer.parseInt(matcher.group(1));
    }

    private static String title(List<PDFobj> objects) {
        return TestSupport.utf16Hex(TestSupport.findObject(objects, "/Producer").getValue("/Title"));
    }

    @Test
    void readsBackWithTheUserOrTheOwnerPassword() throws Exception {
        byte[] pdf = encrypted("hello", "world");
        for (String password : new String[] {"hello", "world"}) {
            List<PDFobj> objects = TestSupport.read(pdf, password);
            assertEquals(1, new PDF().getPageObjects(objects).size());
            assertEquals("Encrypted title", title(objects));
        }
    }

    @Test
    void theTitleIsNotInTheFileInPlainText() throws Exception {
        String raw = TestSupport.latin1(encrypted("hello", "world"));
        assertFalse(raw.contains("feff0045006e0063"));
        assertTrue(raw.contains("/Encrypt "));
    }

    @Test
    void aWrongOrMissingPasswordFailsWithAMessage() throws Exception {
        final byte[] pdf = encrypted("hello", "world");
        Exception wrong = assertThrows(Exception.class, () -> TestSupport.read(pdf, "wrong"));
        assertEquals("The password of the PDF is not correct.", wrong.getMessage());
        Exception missing = assertThrows(Exception.class, () -> TestSupport.read(pdf));
        assertEquals("The PDF needs a password.", missing.getMessage());
    }

    @Test
    void anEmptyUserPasswordOpensWithoutAPassword() throws Exception {
        List<PDFobj> objects = TestSupport.read(encrypted("", "owner"));
        assertEquals("Encrypted title", title(objects));
    }

    @Test
    void aNonAsciiPasswordIsUtf8() throws Exception {
        byte[] pdf = encrypted("пароль", "owner");
        assertEquals("Encrypted title", title(TestSupport.read(pdf, "пароль")));
    }

    @Test
    void passwordsAreCutAt127Bytes() throws Exception {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < 200; i++) {
            sb.append('a');
        }
        final byte[] pdf = encrypted(sb.toString(), "owner");
        assertEquals("Encrypted title", title(TestSupport.read(pdf, sb.substring(0, 127))));
        assertThrows(Exception.class, () -> TestSupport.read(pdf, sb.substring(0, 126)));
    }

    @Test
    void permissionsAreANegativeNumberWithTheReservedBitsSet() throws Exception {
        assertEquals(-3900, accessValue(encrypted("hello", "world")));
    }

    @Test
    void pdfUaGrantsExtractionForAccessibility() throws Exception {
        byte[] pdf = encrypted(Compliance.PDF_UA_1,
                new Passwords().setUserPassword("hello").setOwnerPassword("world"),
                new Permissions().grant(UserAccess.PRINT.getValue()));
        int access = accessValue(pdf);
        assertEquals(-3388, access);
        assertTrue(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(access));
    }
}
