/*
 * Ellipse.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;
using System.Collections.Generic;

namespace PDFjet.NET {
public class Ellipse : Arc {
    /// <summary>
    /// The default constructor.
    /// </summary>
    public Ellipse() : base() {
        SetStartAngle(0f);
        SetSweepDegreesCW(360f);
    }
}
}
