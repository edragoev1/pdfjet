/**
 * Ellipse.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class Ellipse  : Arc {
    /**
     * The default constructor.
     */
    public Ellipse() : base() {
        SetStartAngle(0f);
        SetSweepDegreesCW(360f);
    }
}
}
