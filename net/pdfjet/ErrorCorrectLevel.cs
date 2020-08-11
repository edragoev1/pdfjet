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
using System;


namespace PDFjet.NET {
/**
 * Used to specify the error correction level for QR Codes.
 *
 * @author Kazuhiko Arase
 */
public class ErrorCorrectLevel {
    public const int L = 1;
    public const int M = 0;
    public const int Q = 3;
    public const int H = 2;
}
}   // End of namespace PDFjet.NET
