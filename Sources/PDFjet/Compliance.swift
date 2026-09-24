/**
 * Compliance.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
public enum Compliance: Int {
    case PDF_1_7
    case PDF_UA_1
    case PDF_A_1A
    case PDF_A_1B
    case PDF_A_2A
    case PDF_A_2B
    @available(*, deprecated, renamed: "PDF_A_3A_UA_1",
            message: "PDF_A_3A_UA_1 is the same document, tagged as PDF/UA asks, and says it is PDF/UA-1 too. PDF_A_3A is still written.")
    case PDF_A_3A
    case PDF_A_3B
    /// PDF/A-3a and PDF/UA-1 in one document: tagged as PDF/UA asks, and able to carry files, as PDF/A-3 is.
    case PDF_A_3A_UA_1
}
