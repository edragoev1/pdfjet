/**
 * Util.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Utility methods.
///
class Util {
    /// Returns the red, green and blue components, from 0.0 to 1.0, of a 0xRRGGBB color.
    static func toRGB(_ color: Int32) -> [Float] {
        return [Float((color >> 16) & 0xff)/255.0, Float((color >> 8) & 0xff)/255.0, Float(color & 0xff)/255.0]
    }

}   // End of Util.swift
