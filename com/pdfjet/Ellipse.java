/*
 * Ellipse.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

public final class Ellipse extends Arc {
    /**
     * The default constructor.
     */
    public Ellipse() {
        setStartAngle(0f);
        setSweepDegreesCW(360f);
    }
}
