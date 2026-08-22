//
// Example_35.swift
//
// Copyright (c) 2026 PDFjet Software
// Licensed under the MIT License. See LICENSE file in the project root.
//

import Foundation
import PDFjet

///
/// Example_35.swift
///
public class Example_35 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_35.pdf", append: false)!)

        let page = Page(pdf, Letter.portrait)

        let f1 = Font(pdf, CoreFont.HelveticaBold)
        f1.setSize(14.0)

        let f2 = Font(pdf, CoreFont.Helvetica)
        f2.setSize(14.0)

        // Base container
        let container = Container(400.0, 400.0)
        container.setLocation(100.0, 100.0)

        // Add a rectangle to container
        let rect = Rect(0.0, 0.0, 400.0, 400.0)
        rect.setFillColor(Color.gray)
        container.add(rect)

        var stamp = Stamp(pdf)
        stamp = stamp.withSize(400.0, 400.0)
        stamp = stamp.withFont(f1)
        stamp = stamp.withFont(f2)

        // Draw path ...
        stamp = stamp.setFillColor(Color.lightBlue)
            .setStrokeColor(Color.red)
            .setStrokeWidth(4.0)
            .moveTo(0.0, 0.0)
            .lineTo(400.0, 0.0)
            .lineTo(400.0, 400.0)
            .lineTo(0.0, 400.0)
            .closeFillAndStrokePath()

        // Draw Rectangle
        stamp = stamp.setStrokeColor(Color.blue)
            .setStrokeWidth(1.0)
            .drawRect(10.0, 10.0, 380.0, 380.0)

        // Fill Rectangle
        stamp = stamp.setFillColor(Color.green)
            .fillRect(10.0, 10.0, 20.0, 20.0)

        // Draw some text
        var parameters = TextParameters()
        parameters = parameters.setFont(f1)
        parameters = parameters.setFontSize(14.0)
        parameters = parameters.setTextLocation(25.0, 25.0)
        parameters = parameters.setText("Hello, World!")
        stamp.drawText(parameters)

        // Change some parameters and draw the text again
        parameters = parameters.setFont(f2).setTextLocation(25.0, 50.0)
        stamp = stamp.setFillColor(Color.darkGreen)
        stamp.drawText(parameters)

        // Complete the stamp
        try stamp.complete()

        // Draw stamp on page at original location
        stamp = stamp.setLocation(50.0, 50.0)
        stamp.drawOn(page)

        // Rotate the stamp counter clockwise and draw it again
        stamp.rotate(15).drawOn(page)

        // Rotate the stamp clockwise and draw it again
        stamp.rotate(-15).drawOn(page)

        // Add a text line to container
        let title = TextLine(f1, "Container")
        title.setLocation(150.0, 20.0)
        container.add(title)

        // Nested container #1
        let nested1 = Container(200.0, 200.0)
        nested1.setLocation(0.0, 0.0)
        nested1.setRotationCounterClockwise(30)
        nested1.setScaleFactor(0.8)

        let innerRect = Rect(0.0, 0.0, 200.0, 200.0)
        innerRect.setFillColor(Color.blue)
        nested1.add(innerRect)

        let innerText = TextLine(f1, "Nested 1")
        innerText.setLocation(50.0, 100.0)
        nested1.add(innerText)

        container.add(nested1)

        // Nested container #2
        let nested2 = Container(100.0, 100.0)
        nested2.setLocation(250.0, 250.0)
        nested2.setRotationCounterClockwise(45)

        let smallRect = Rect(0.0, 0.0, 100.0, 100.0)
        smallRect.setFillColor(Color.red)
        nested2.add(smallRect)

        let smallText = TextLine(f1, "Nested 2")
        smallText.setLocation(10.0, 50.0)
        nested2.add(smallText)

        container.add(nested2)

        container.setRotationClockwise(45)
        // Draw the entire hierarchy on the page
        container.drawOn(page)

        // Additional container demonstration
        let container5 = Container(200.0, 20.0)
        let rect5 = Rect(0.0, 0.0, 200.0, 20.0)
        container5.add(rect5)

        let rect6 = Rect(0.0, 0.0, 10.0, 10.0)
        rect6.setFillColor(Color.blue)
        container5.add(rect6)

        let rect7 = Rect(190.0, 10.0, 10.0, 10.0)
        rect7.setBorderColor(Color.red)
        rect7.setBorderWidth(2.0)
        container5.add(rect7)

        container5.setLocation(50.0, 600.0)
        container5.drawOn(page)

        container5.setRotation(-90)
        container5.drawOn(page)

        try pdf.complete()
    }
}

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_35()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
TextUtils.printDuration("Example_35", time0, time1)