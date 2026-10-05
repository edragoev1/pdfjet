/**
 * ColorTransparentTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

/// Each setter of a 0xRRGGBB color leaves the color as it was for
/// Color.transparent, -1, which they drew white, its low 24 bits, before
/// v9.0.3: the object set to red, then to transparent, is the object set to red.
@Suite struct ColorTransparentTests {
    @Test func everyColorSetterLeavesTheColorForTransparent() {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        let red: Int32 = 0xFF0000
        let cases: [(String, () -> AnyObject, (AnyObject, Int32) -> Void)] = [
            ("Arc.setStrokeColor", { Arc() }, { ($0 as! Arc).setStrokeColor($1) }),
            ("Arc.setFillColor", { Arc() }, { ($0 as! Arc).setFillColor($1) }),
            ("Chart.setGridLineColor", { Chart(font, font) }, { ($0 as! Chart).setGridLineColor($1) }),
            ("BarChart.setGridLineColor", { BarChart(font, font) }, { ($0 as! BarChart).setGridLineColor($1) }),
            ("CheckBox.setBorderColor", { CheckBox(font, "A") }, { ($0 as! CheckBox).setBorderColor($1) }),
            ("CheckBox.setCheckmarkColor", { CheckBox(font, "A") }, { ($0 as! CheckBox).setCheckmarkColor($1) }),
            ("BaseAnnotation.setFillColor", { SquareAnnotation() }, { ($0 as! SquareAnnotation).setFillColor($1) }),
            ("Container.setBorderColor", { Container(100, 50) }, { ($0 as! Container).setBorderColor($1) }),
            ("Page.setPenColor", { Page(pdf, Letter.PORTRAIT) }, { ($0 as! Page).setPenColor($1) }),
            ("Page.setBrushColor", { Page(pdf, Letter.PORTRAIT) }, { ($0 as! Page).setBrushColor($1) }),
            ("Markup.setLinkColor", { Markup(font, font, font, font, font) }, { ($0 as! Markup).setLinkColor($1) }),
            ("Form.setLabelColor", { Form([]) }, { ($0 as! Form).setLabelColor($1) }),
            ("Form.setValueColor", { Form([]) }, { ($0 as! Form).setValueColor($1) }),
            ("Line.setStrokeColor", { Line(0, 0, 10, 10) }, { ($0 as! Line).setStrokeColor($1) }),
            ("Point.setStrokeColor", { Point(1, 1) }, { ($0 as! Point).setStrokeColor($1) }),
            ("Point.setFillColor", { Point(1, 1) }, { ($0 as! Point).setFillColor($1) }),
            ("Paragraph.setTextColor", { Paragraph(TextLine(font, "A")) }, { ($0 as! Paragraph).setTextColor($1) }),
            ("Stamp.setStrokeColor", { Stamp(pdf) }, { ($0 as! Stamp).setStrokeColor($1) }),
            ("Stamp.setFillColor", { Stamp(pdf) }, { ($0 as! Stamp).setFillColor($1) }),
            ("Table.setCellBorderColor", { Table().setTableData([[Cell(font, "A")]], 0) },
                { ($0 as! Table).setCellBorderColor($1) }),
            ("Path.setStrokeColor", { Path() }, { ($0 as! Path).setStrokeColor($1) }),
            ("Rect.setFillColor", { Rect(0, 0, 10, 10) }, { ($0 as! Rect).setFillColor($1) }),
            ("Series.setStrokeColor", { Series("A") }, { ($0 as! Series).setStrokeColor($1) }),
        ]
        for (name, make, set) in cases {
            let o = make()
            set(o, red)
            let before = dump(o, 4)
            set(o, Color.transparent)
            #expect(dump(o, 4) == before, "\(name): Color.transparent changed it")
        }
    }

    /// The value, its private properties too, following references that many
    /// levels down, so that a color kept in a cell of a table, or a line of a
    /// paragraph, is seen.
    private func dump(_ value: Any, _ depth: Int) -> String {
        let mirror = Mirror(reflecting: value)
        if mirror.displayStyle == .optional {
            guard let child = mirror.children.first else {
                return "nil"
            }
            return dump(child.value, depth)
        }
        if mirror.displayStyle == .class && depth == 0 {
            return "&"
        }
        if mirror.children.isEmpty && mirror.superclassMirror == nil {
            return String(describing: value)
        }
        let next = mirror.displayStyle == .class ? depth - 1 : depth
        var s = "{"
        var m: Mirror? = mirror
        while let current = m {
            for (i, child) in current.children.enumerated() where i < 64 {
                s += (child.label ?? "") + ":" + dump(child.value, next) + " "
            }
            m = current.superclassMirror
        }
        return s + "}"
    }
}
