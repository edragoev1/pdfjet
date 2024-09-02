/**
 *
Copyright 2009 Kazuhiko Arase

URL: http://www.d-project.com/

Licensed under the MIT license:
  http://www.opensource.org/licenses/mit-license.php

The word "QR Code" is registered trademark of
DENSO WAVE INCORPORATED
  http://www.denso-wave.com/qrcode/faqpatent-e.html
*/
package com.pdfjet;

/**
 * Used to specify the error correction level for QR Codes.
 *
 * @author Kazuhiko Arase
 */
public class ErrorCorrectLevel {
    /** Default Constructor */
    public ErrorCorrectLevel() {
    }

    /** Low */
    public static final int L = 1;

    /** Medium */
    public static final int M = 0;

    /** Quartile */
    public static final int Q = 3;

    /** High */
    public static final int H = 2;
}
