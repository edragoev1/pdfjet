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

namespace PDFjet.NET {
/// <summary>The eight QR code mask patterns.</summary>
internal class MaskPattern {
    /// <summary>Mask pattern 000.</summary>
    public const int PATTERN000 = 0;
    /// <summary>Mask pattern 001.</summary>
    public const int PATTERN001 = 1;
    /// <summary>Mask pattern 010.</summary>
    public const int PATTERN010 = 2;
    /// <summary>Mask pattern 011.</summary>
    public const int PATTERN011 = 3;
    /// <summary>Mask pattern 100.</summary>
    public const int PATTERN100 = 4;
    /// <summary>Mask pattern 101.</summary>
    public const int PATTERN101 = 5;
    /// <summary>Mask pattern 110.</summary>
    public const int PATTERN110 = 6;
    /// <summary>Mask pattern 111.</summary>
    public const int PATTERN111 = 7;
}
}   // End of namespace PDFjet.NET
