/*
 * DataMatrixTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.datamatrix;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertThrows;
import static org.junit.jupiter.api.Assertions.assertTrue;

import com.pdfjet.Letter;
import com.pdfjet.Page;
import com.pdfjet.TestSupport;
import org.junit.jupiter.api.Test;

class DataMatrixTest {
    @Test
    void sixDigitsFitTheSmallestSquareWithItsFinderPattern() {
        boolean[][] modules = new DataMatrix("123456").getModules();
        assertEquals(10, modules.length);
        assertEquals(10, modules[0].length);
        for (int i = 0; i < 10; i++) {
            assertEquals(i % 2 == 0, modules[0][i], "top row alternates");
            assertEquals(true, modules[9][i], "bottom row is solid");
            assertEquals(true, modules[i][0], "left column is solid");
        }
    }

    @Test
    void longerDataGetsALargerSymbol() {
        assertEquals(32, new DataMatrix(new String(new char[60]).replace('\0', 'Z')).getModules().length);
    }

    @Test
    void theRectangleShapeIsWiderThanTall() {
        boolean[][] modules = new DataMatrix("Hello, World!", DataMatrix.RECTANGLE).getModules();
        assertEquals(12, modules.length);
        assertEquals(26, modules[0].length);
    }

    @Test
    void drawOnReturnsTheCornerOfTheModules() throws Exception {
        Page page = new Page(TestSupport.newPDF(), Letter.PORTRAIT);
        DataMatrix dm = new DataMatrix("123456").setLocation(5f, 5f).setModuleLength(3f);
        TestSupport.assertXY(35f, 35f, dm.drawOn(page));
    }

    // The GS1 symbols below were read back with ZXing, which gave the symbology
    // identifier ]d2 of GS1 DataMatrix and these element strings.
    @Test
    void gs1WritesTheElementStringWithGSAfterAFieldOfNoSetLength() {
        String[][] cases = {
            {"(01)09506000134352(17)261231(10)ABC123(21)XYZ-42", "01095060001343521726123110ABC123\u001d21XYZ-42"},
            {"(10)BATCH7(21)SN001(01)09506000134352", "10BATCH7\u001d21SN001\u001d0109506000134352"},
            {"(00)106141412345678908", "00106141412345678908"},
            {"(01)09506000134352(3103)000750(15)270101", "0109506000134352310300075015270101"},
        };
        for (String[] c : cases) {
            assertEquals(c[1], GS1.elementString(c[0]), c[0]);
        }
    }

    @Test
    void gs1TakesTheSymbolOfItsCodewords() {
        // FNC1 and 12 codewords, as the plain text takes 13 with GS: 18 by 18 holds 18
        assertEquals(18, DataMatrix.fromGS1("(01)09506000134352(10)ABC").getModules().length);
        assertEquals(18, new DataMatrix("0109506000134352\u001d10ABC").getModules().length);
        boolean[][] rect = DataMatrix.fromGS1("(01)09506000134352", DataMatrix.RECTANGLE).getModules();
        assertTrue(rect.length < rect[0].length);
    }

    @Test
    void gs1RefusesDataThatIsNotGS1() {
        final String FORMAT =
                "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!";
        String[][] cases = {
            {"", FORMAT},
            {"01)09506000134352", FORMAT},
            {"(01", FORMAT},
            {"(1)5", "The Application Identifier (1) is not two to four digits!"},
            {"(12345)5", "The Application Identifier (12345) is not two to four digits!"},
            {"(A1)5", "The Application Identifier (A1) is not two to four digits!"},
            {"(10)(21)SN", "The Application Identifier (10) has no data!"},
            {"(10)AB C", "The data of (10) has a character that GS1 does not allow!"},
            {"(10)AB)C", "The data of (10) has a character that GS1 does not allow!"},
            {"(10)Gr\u00fc\u00dfe", "The data of (10) has a character that GS1 does not allow!"},
            {"(91)" + repeat("X", 91) + "", "The data of (91) is longer than 90 characters!"},
            {"(01)0950600013435", "The data of (01) must be 14 digits!"},
            {"(17)2612A1", "The data of (17) must be 6 digits!"},
            {"(3103)00075", "The data of (3103) must be 6 digits!"},
            {"(01)09506000134353", "The check digit of (01) is wrong!"},
            {"(00)106141412345678909", "The check digit of (00) is wrong!"},
            {"(414)9506000134353", "The check digit of (414) is wrong!"},
        };
        for (final String[] c : cases) {
            IllegalArgumentException e =
                    assertThrows(IllegalArgumentException.class, () -> DataMatrix.fromGS1(c[0]), c[0]);
            assertEquals(c[1], e.getMessage(), c[0]);
        }
        // Fields of no set length, such as (10) and (21), take any data GS1
        // allows, and (418), a GLN of no check digit here, takes any 13 digits
        DataMatrix.fromGS1("(10)!\"%&'*+,-./:;<=>?_az(21)1(418)1234567890123");
    }

    private static String repeat(String s, int n) {
        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < n; i++) {
            sb.append(s);
        }
        return sb.toString();
    }
}
