/**
 * CoreFont.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

public class CoreFont {
    public static let COURIER = 1
    public static let COURIER_BOLD = 2
    public static let COURIER_OBLIQUE = 3
    public static let COURIER_BOLD_OBLIQUE = 4
    public static let HELVETICA = 5
    public static let HELVETICA_BOLD = 6
    public static let HELVETICA_OBLIQUE = 7
    public static let HELVETICA_BOLD_OBLIQUE = 8
    public static let TIMES_ROMAN = 9
    public static let TIMES_BOLD = 10
    public static let TIMES_ITALIC = 11
    public static let TIMES_BOLD_ITALIC = 12
    public static let SYMBOL = 13
    public static let ZAPF_DINGBATS = 14

    var name: String?
    var bBoxLLx: Int16?
    var bBoxLLy: Int16?
    var bBoxURx: Int16?
    var bBoxURy: Int16?
    var underlinePosition: Int16?
    var underlineThickness: Int16?
    var metrics: [[Int16]]?

    public init(_ coreFont: Int) {
        if coreFont == CoreFont.COURIER {
            self.name = Courier.name
            self.bBoxLLx = Courier.bBoxLLx
            self.bBoxLLy = Courier.bBoxLLy
            self.bBoxURx = Courier.bBoxURx
            self.bBoxURy = Courier.bBoxURy
            self.underlinePosition = Courier.underlinePosition
            self.underlineThickness = Courier.underlineThickness
            self.metrics = Courier.metrics
        } else if coreFont == CoreFont.COURIER_BOLD {
            self.name = Courier_Bold.name
            self.bBoxLLx = Courier_Bold.bBoxLLx
            self.bBoxLLy = Courier_Bold.bBoxLLy
            self.bBoxURx = Courier_Bold.bBoxURx
            self.bBoxURy = Courier_Bold.bBoxURy
            self.underlinePosition = Courier_Bold.underlinePosition
            self.underlineThickness = Courier_Bold.underlineThickness
            self.metrics = Courier_Bold.metrics
        } else if coreFont == CoreFont.COURIER_OBLIQUE {
            self.name = Courier_Oblique.name
            self.bBoxLLx = Courier_Oblique.bBoxLLx
            self.bBoxLLy = Courier_Oblique.bBoxLLy
            self.bBoxURx = Courier_Oblique.bBoxURx
            self.bBoxURy = Courier_Oblique.bBoxURy
            self.underlinePosition = Courier_Oblique.underlinePosition
            self.underlineThickness = Courier_Oblique.underlineThickness
            self.metrics = Courier_Oblique.metrics
        } else if coreFont == CoreFont.COURIER_BOLD_OBLIQUE {
            self.name = Courier_BoldOblique.name
            self.bBoxLLx = Courier_BoldOblique.bBoxLLx
            self.bBoxLLy = Courier_BoldOblique.bBoxLLy
            self.bBoxURx = Courier_BoldOblique.bBoxURx
            self.bBoxURy = Courier_BoldOblique.bBoxURy
            self.underlinePosition = Courier_BoldOblique.underlinePosition
            self.underlineThickness = Courier_BoldOblique.underlineThickness
            self.metrics = Courier_BoldOblique.metrics
        } else if coreFont == CoreFont.HELVETICA {
            self.name = Helvetica.name
            self.bBoxLLx = Helvetica.bBoxLLx
            self.bBoxLLy = Helvetica.bBoxLLy
            self.bBoxURx = Helvetica.bBoxURx
            self.bBoxURy = Helvetica.bBoxURy
            self.underlinePosition = Helvetica.underlinePosition
            self.underlineThickness = Helvetica.underlineThickness
            self.metrics = Helvetica.metrics
        } else if coreFont == CoreFont.HELVETICA_BOLD {
            self.name = Helvetica_Bold.name
            self.bBoxLLx = Helvetica_Bold.bBoxLLx
            self.bBoxLLy = Helvetica_Bold.bBoxLLy
            self.bBoxURx = Helvetica_Bold.bBoxURx
            self.bBoxURy = Helvetica_Bold.bBoxURy
            self.underlinePosition = Helvetica_Bold.underlinePosition
            self.underlineThickness = Helvetica_Bold.underlineThickness
            self.metrics = Helvetica_Bold.metrics
        } else if coreFont == CoreFont.HELVETICA_OBLIQUE {
            self.name = Helvetica_Oblique.name
            self.bBoxLLx = Helvetica_Oblique.bBoxLLx
            self.bBoxLLy = Helvetica_Oblique.bBoxLLy
            self.bBoxURx = Helvetica_Oblique.bBoxURx
            self.bBoxURy = Helvetica_Oblique.bBoxURy
            self.underlinePosition = Helvetica_Oblique.underlinePosition
            self.underlineThickness = Helvetica_Oblique.underlineThickness
            self.metrics = Helvetica_Oblique.metrics
        } else if coreFont == CoreFont.HELVETICA_BOLD_OBLIQUE {
            self.name = Helvetica_BoldOblique.name
            self.bBoxLLx = Helvetica_BoldOblique.bBoxLLx
            self.bBoxLLy = Helvetica_BoldOblique.bBoxLLy
            self.bBoxURx = Helvetica_BoldOblique.bBoxURx
            self.bBoxURy = Helvetica_BoldOblique.bBoxURy
            self.underlinePosition = Helvetica_BoldOblique.underlinePosition
            self.underlineThickness = Helvetica_BoldOblique.underlineThickness
            self.metrics = Helvetica_BoldOblique.metrics
        } else if coreFont == CoreFont.TIMES_ROMAN {
            self.name = Times_Roman.name
            self.bBoxLLx = Times_Roman.bBoxLLx
            self.bBoxLLy = Times_Roman.bBoxLLy
            self.bBoxURx = Times_Roman.bBoxURx
            self.bBoxURy = Times_Roman.bBoxURy
            self.underlinePosition = Times_Roman.underlinePosition
            self.underlineThickness = Times_Roman.underlineThickness
            self.metrics = Times_Roman.metrics
        } else if coreFont == CoreFont.TIMES_BOLD {
            self.name = Times_Bold.name
            self.bBoxLLx = Times_Bold.bBoxLLx
            self.bBoxLLy = Times_Bold.bBoxLLy
            self.bBoxURx = Times_Bold.bBoxURx
            self.bBoxURy = Times_Bold.bBoxURy
            self.underlinePosition = Times_Bold.underlinePosition
            self.underlineThickness = Times_Bold.underlineThickness
            self.metrics = Times_Bold.metrics
        } else if coreFont == CoreFont.TIMES_ITALIC {
            self.name = Times_Italic.name
            self.bBoxLLx = Times_Italic.bBoxLLx
            self.bBoxLLy = Times_Italic.bBoxLLy
            self.bBoxURx = Times_Italic.bBoxURx
            self.bBoxURy = Times_Italic.bBoxURy
            self.underlinePosition = Times_Italic.underlinePosition
            self.underlineThickness = Times_Italic.underlineThickness
            self.metrics = Times_Italic.metrics
        } else if coreFont == CoreFont.TIMES_BOLD_ITALIC {
            self.name = Times_BoldItalic.name
            self.bBoxLLx = Times_BoldItalic.bBoxLLx
            self.bBoxLLy = Times_BoldItalic.bBoxLLy
            self.bBoxURx = Times_BoldItalic.bBoxURx
            self.bBoxURy = Times_BoldItalic.bBoxURy
            self.underlinePosition = Times_BoldItalic.underlinePosition
            self.underlineThickness = Times_BoldItalic.underlineThickness
            self.metrics = Times_BoldItalic.metrics
        } else if coreFont == CoreFont.SYMBOL {
            self.name = Symbol.name
            self.bBoxLLx = Symbol.bBoxLLx
            self.bBoxLLy = Symbol.bBoxLLy
            self.bBoxURx = Symbol.bBoxURx
            self.bBoxURy = Symbol.bBoxURy
            self.underlinePosition = Symbol.underlinePosition
            self.underlineThickness = Symbol.underlineThickness
            self.metrics = Symbol.metrics
        } else if coreFont == CoreFont.ZAPF_DINGBATS {
            self.name = ZapfDingbats.name
            self.bBoxLLx = ZapfDingbats.bBoxLLx
            self.bBoxLLy = ZapfDingbats.bBoxLLy
            self.bBoxURx = ZapfDingbats.bBoxURx
            self.bBoxURy = ZapfDingbats.bBoxURy
            self.underlinePosition = ZapfDingbats.underlinePosition
            self.underlineThickness = ZapfDingbats.underlineThickness
            self.metrics = ZapfDingbats.metrics
        }
    }
}
