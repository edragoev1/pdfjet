/**
 * Modules.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

// The modules of a two-dimensional barcode, a QR code or a Data Matrix,
// which both draw them the same way.
enum Modules {
    // Draws the dark modules, m1 long each, from x, y, each run of them in a
    // row as one rectangle, in the color. Described, the barcode is a figure
    // of a tagged document, w by h; not described, its modules, which carry no
    // text, are decoration.
    static func draw(_ page: Page, _ modules: [[Bool]], _ x: Float, _ y: Float, _ m1: Float,
            _ color: Int32, _ altDescription: String?, _ w: Float, _ h: Float) {
        if let altDescription = altDescription, !altDescription.isEmpty {
            page.addBDC(StructElem.FIGURE, nil, nil, altDescription)
        } else {
            page.addArtifactBMC()
        }
        page.saveGraphicsState()
        page.setBrushColor(color)
        for row in 0..<modules.count {
            let cols = modules[row].count
            var col = 0
            while col < cols {
                if !modules[row][col] {
                    col += 1
                    continue
                }
                let start = col
                while col < cols && modules[row][col] {
                    col += 1
                }
                page.fillRect(x + Float(start)*m1, y + Float(row)*m1, Float(col - start)*m1, m1)
            }
        }
        page.restoreGraphicsState()
        if let altDescription = altDescription, !altDescription.isEmpty {
            page.setFigureBoundingBox(x, y, w, h)
        }
        page.addEMC()
    }
}
