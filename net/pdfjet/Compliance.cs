/*
 * Compliance.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

namespace PDFjet.NET {
    /// <summary>
    /// Used to set PDF/A compliance.
    /// See the constructors in the PDF class.
    /// </summary>
    public enum Compliance {
        /// <summary>Plain PDF 1.7.</summary>
        PDF_1_7,
        /// <summary>PDF/UA-1, for accessible documents.</summary>
        PDF_UA_1,
        /// <summary>PDF/A-1a.</summary>
        PDF_A_1A,
        /// <summary>PDF/A-1b.</summary>
        PDF_A_1B,
        /// <summary>PDF/A-2a.</summary>
        PDF_A_2A,
        /// <summary>PDF/A-2b.</summary>
        PDF_A_2B,
        /// <summary>PDF/A-3a.</summary>
        PDF_A_3A,
        /// <summary>PDF/A-3b.</summary>
        PDF_A_3B,
        /// <summary>PDF/A-3a and PDF/UA-1 in one document: tagged as PDF/UA asks, and able to carry files, as PDF/A-3 is.</summary>
        PDF_A_3A_UA_1
    }
}   // End of namespace PDFjet.NET
