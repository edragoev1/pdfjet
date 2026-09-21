/*
 * DecryptorTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;

import java.util.Arrays;
import java.util.List;
import org.junit.jupiter.api.Test;

/**
 * The parts of the decryptor that a PDF read back from this library does not
 * reach, which the encryption tests of com.pdfjet.encryption cannot call.
 */
class DecryptorTest {
    // Returns the object with the number and the tokens of the raw text, which
    // are separated by single spaces.
    private static PDFobj object(int number, String raw) {
        PDFobj obj = new PDFobj();
        obj.number = number;
        obj.dict.addAll(Arrays.asList(raw.split(" ")));
        return obj;
    }

    @Test
    void aCryptFilterThatIsAnObjectOfItsOwnIsFollowed() {
        // The /CF dictionary and the filter in it as objects of their own, as
        // pdf.js tests them in issue7665: the method was not found, and the
        // streams and the strings of the PDF were left encrypted.
        List<PDFobj> objects = Arrays.asList(
                object(7, "7 0 obj << /StdCF 8 0 R >> endobj"),
                object(8, "8 0 obj << /AuthEvent /DocOpen /CFM /AESV3 /Length 32 >> endobj"),
                object(9, "9 0 obj << /StdCF << /CFM /AESV2 >> >> endobj"));
        Object[][] cases = {
            {"7 0 R", Decryptor.AES_256},
            {"9 0 R", Decryptor.AES_128},
            {"<< /StdCF 8 0 R >>", Decryptor.AES_256},
            {"<< /StdCF << /CFM /V2 >> >>", Decryptor.RC4},
            {"99 0 R", Decryptor.NONE},
        };
        for (Object[] c : cases) {
            PDFobj encrypt = object(6,
                    "6 0 obj << /Filter /Standard /V 5 /CF " + c[0] + " /StmF /StdCF >> endobj");
            assertEquals(c[1], Decryptor.getMethod(encrypt, "/StdCF", objects), "/CF " + c[0]);
        }
    }
}
