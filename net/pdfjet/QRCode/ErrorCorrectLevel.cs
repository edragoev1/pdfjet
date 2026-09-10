/*
 * RSBlock.cs
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
using System;

namespace PDFjet.NET {
/// <summary>
/// Used to specify the error correction level for QR Codes.
/// </summary>
public class ErrorCorrectLevel {
    /// <summary>Recovers about 7% of the data.</summary>
    public const int L = 1;
    /// <summary>Recovers about 15% of the data.</summary>
    public const int M = 0;
    /// <summary>Recovers about 25% of the data.</summary>
    public const int Q = 3;
    /// <summary>Recovers about 30% of the data.</summary>
    public const int H = 2;
}
}   // End of namespace PDFjet.NET
