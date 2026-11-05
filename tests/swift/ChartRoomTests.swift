/*
 * ChartRoomTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import Testing
@testable import PDFjet

// A chart too small for its plot is an error, the document not completed,
// where the chart drew nothing and said nothing (the owner, 4 November 2026:
// "we can create more space if the user tries to jam too much info"). The
// same chart with room is drawn.
@Suite struct ChartRoomTests {
    private func noRoom(_ name: String, _ chart: (Font) -> Drawable) {
        let pdf = TestSupport.newPDF()
        let font = TestSupport.helvetica(pdf)
        chart(font).drawOn(Page(pdf, Letter.PORTRAIT))
        let error = pdf.error ?? ""
        #expect(error.contains(name) && error.contains("has no room for its plot"), "\(name): \(error)")
    }

    @Test func aChartWithoutRoomIsAnError() {
        noRoom("SimpleLineChart") { f in
            SimpleLineChart(f, f).setTitle("Sales").addLine("a", [1, 5, 3]).setSize(40, 30)
        }
        noRoom("SimpleScatterChart") { f in
            SimpleScatterChart(f, f).setTitle("Points").add(1, 2).add(3, 4).setSize(40, 30)
        }
        noRoom("BarChart") { f in
            BarChart(f, f).setCategories("a", "b").addSeries("s", [1, 2]).setSize(30, 20)
        }
        noRoom("SimpleBarChart") { f in
            SimpleBarChart(f, f).setWidth(90).add("A long name", 1234567)
        }

        let pdf = TestSupport.newPDF()
        let f = TestSupport.helvetica(pdf)
        let page = Page(pdf, Letter.PORTRAIT)
        SimpleLineChart(f, f).setTitle("Sales").addLine("a", [1, 5, 3]).setSize(300, 200).drawOn(page)
        SimpleScatterChart(f, f).setTitle("Points").add(1, 2).add(3, 4).setSize(300, 200).drawOn(page)
        BarChart(f, f).setCategories("a", "b").addSeries("s", [1, 2]).setSize(300, 200).drawOn(page)
        SimpleBarChart(f, f).setWidth(300).add("A long name", 1234567).drawOn(page)
        #expect(pdf.error == nil, "\(pdf.error ?? "")")
    }
}
