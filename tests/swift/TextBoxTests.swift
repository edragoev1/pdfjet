/**
 * TextBoxTests.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Testing
@testable import PDFjet

@Suite struct TextBoxTests {
    @Test func measuringDoesNotFixTheHeight() {
        let box = TextBox(TestSupport.helvetica(TestSupport.newPDF()),
                "one two three four five six seven eight nine ten")
        _ = box.setLocation(0, 0)
        _ = box.setWidth(60)
        TestSupport.expectXY(60, 83.232, box.drawOn(nil))
        TestSupport.expectNear(83.232, box.getHeight())

        _ = box.setText("one two three four five six seven eight nine ten eleven twelve thirteen fourteen")
        TestSupport.expectXY(60, 124.848, box.drawOn(nil))
        TestSupport.expectNear(124.848, box.getHeight())
    }

    @Test func bordersAreOffByDefaultAndCanBeRemovedOneByOne() {
        let box = TextBox(TestSupport.helvetica(TestSupport.newPDF()), "x")
        #expect(!box.getBorder(Border.TOP))
        _ = box.setBorders(true)
        _ = box.setBorder(Border.TOP, false)
        #expect(!box.getBorder(Border.TOP))
        #expect(box.getBorder(Border.LEFT))
        #expect(box.getBorder(Border.RIGHT))
        #expect(box.getBorder(Border.BOTTOM))
    }

    @Test func colorGettersReturnCopies() {
        let box = TextBox(TestSupport.helvetica(TestSupport.newPDF()), "x")
        _ = box.setTextColor(Int32(0x0000FF))
        var copy = box.getTextColor()
        copy[2] = 0
        TestSupport.expectRGB(0, 0, 1, box.getTextColor())
        _ = box.setBorderColor(Int32(0xFF0000))
        TestSupport.expectRGB(1, 0, 0, box.getBorderColor())
    }
}
