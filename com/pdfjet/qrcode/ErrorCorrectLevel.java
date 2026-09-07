/*
 * Polynomial.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 *
 * Original author: Kazuhiko Arase, 2009
 * URL: http://www.d-project.com/
 * Licensed under MIT: http://www.opensource.org/licenses/mit-license.php
 *
 * The word "QR Code" is a registered trademark of
 * DENSO WAVE INCORPORATED
 * http://www.denso-wave.com/qrcode/faqpatent-e.html
 *
 * Modified and adapted for use in PDFjet by PDFjet Software
 */
package com.pdfjet;

/**
 * Used to specify the error correction level for QR Codes.
 */
public class ErrorCorrectLevel {
    /** Low */
    public static final int L = 1;

    /** Medium */
    public static final int M = 0;

    /** Quartile */
    public static final int Q = 3;

    /** High */
    public static final int H = 2;
}
