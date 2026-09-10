/**
 *  Ellipse.swift
 *
 *  Copyright (c) 2026 PDFjet Software
 *  Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// An ellipse: an arc that sweeps 360 degrees.
public final class Ellipse : Arc {
    ///
    /// The default constructor.
    ///
    public override init() {
        super.init()
        _ = setStartAngle(0.0)
        _ = setSweepDegreesCW(360.0)
    }
}   // End of Ellipse.swift
