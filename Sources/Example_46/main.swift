/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_46.swift
 * Using PDF layers (optional content groups) that can be shown or hidden: a
 * shaded relief map of Europe, the lines of latitude and longitude over it,
 * and the capital cities. The map covers 12°W to 42°E and 34°N to 62°N, and
 * longitude and latitude map linearly to x and y on it.
 */
public class Example_46 {
    private let x0: Float = 50.0    // The top left corner of the map
    private let y0: Float = 180.0
    private let w: Float = 512.0    // The size of the map
    private let h: Float = 512.0 * 840.0 / 1084.0

    private func mapX(_ longitude: Float) -> Float {
        return x0 + (longitude + 12.0) / 54.0 * w
    }

    private func mapY(_ latitude: Float) -> Float {
        return y0 + (62.0 - latitude) / 28.0 * h
    }

    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_46.pdf", append: false)!)

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        let f2 = try Font(pdf, IBMPlexSans.SemiBold)

        let page = Page(pdf, Letter.PORTRAIT)

        var text = TextLine(f2, "PDF Layers")
        text.setFontSize(22.0)
        text.setLocation(50.0, 80.0)
        text.drawOn(page)

        let textBlock = TextBlock(f1,
                "Each part of this map is a layer, an optional content group, that "
                + "a PDF viewer lists in its Layers panel and can show or hide: the "
                + "relief, the lines of latitude and longitude, and the capital "
                + "cities. The lines are shown on the screen but not printed.")
        textBlock.setFontSize(12.0)
        textBlock.setLineSpacing(1.5)
        textBlock.setLocation(50.0, 95.0)
        textBlock.setWidth(512.0)
        textBlock.drawOn(page)

        let image = try Image(pdf, "images/europe-relief.png")
        image.resizeWidth(w)
        image.setLocation(x0, y0)

        // A layer is hidden and not printed unless it is set to be.
        var group = OptionalContentGroup(pdf, "Relief")
        group.setVisible(true)
        group.setPrintable(true)
        group.add(image)
        group.drawOn(page)

        group = OptionalContentGroup(pdf, "Latitude and Longitude")
        group.setVisible(true)
        for longitude in stride(from: -10, through: 40, by: 5) {
            let line = Line(mapX(Float(longitude)), y0, mapX(Float(longitude)), y0 + h)
            line.setStrokeWidth(0.5)
            line.setStrokeColor(Color.white)
            group.add(line)

            let label = (longitude < 0) ? "\(-longitude)°W" :
                    (longitude > 0) ? "\(longitude)°E" : "0°"
            text = TextLine(f1, label)
            text.setFontSize(8.0)
            text.setTextColor(Color.gray)
            text.setLocation(mapX(Float(longitude)) - text.getWidth() / 2.0, y0 + h + 12.0)
            group.add(text)
        }
        for latitude in stride(from: 35, through: 60, by: 5) {
            let line = Line(x0, mapY(Float(latitude)), x0 + w, mapY(Float(latitude)))
            line.setStrokeWidth(0.5)
            line.setStrokeColor(Color.white)
            group.add(line)

            text = TextLine(f1, "\(latitude)°N")
            text.setFontSize(8.0)
            text.setTextColor(Color.gray)
            text.setLocation(x0 + w + 4.0, mapY(Float(latitude)) + 3.0)
            group.add(text)
        }
        group.drawOn(page)

        let cities = [
            "Athens", "Ankara", "Berlin", "Bucharest", "Dublin",
            "Kyiv", "Lisbon", "London", "Madrid", "Oslo",
            "Paris", "Rome", "Stockholm", "Vienna", "Warsaw",
        ]
        let locations: [[Float]] = [    // Longitude and latitude
            [23.73, 37.98], [32.86, 39.93], [13.40, 52.52],
            [26.10, 44.43], [-6.26, 53.35], [30.52, 50.45],
            [-9.14, 38.72], [-0.13, 51.51], [-3.70, 40.42],
            [10.75, 59.91], [ 2.35, 48.86], [12.50, 41.90],
            [18.07, 59.33], [16.37, 48.21], [21.01, 52.23],
        ]
        group = OptionalContentGroup(pdf, "Capital Cities")
        group.setVisible(true)
        group.setPrintable(true)
        for i in 0..<cities.count {
            let x = mapX(locations[i][0])
            let y = mapY(locations[i][1])

            let point = Point(x, y)
            point.setRadius(2.5)
            point.setFillColor(Color.white)
            point.setStrokeColor(Color.black)
            group.add(point)

            text = TextLine(f2, cities[i])
            text.setFontSize(8.0)
            text.setLocation(x + 5.0, y + 3.0)
            group.add(text)
        }
        group.drawOn(page)

        text = TextLine(f1, "Relief: Natural Earth, public domain, naturalearthdata.com")
        text.setFontSize(8.0)
        text.setTextColor(Color.gray)
        text.setURIAction("https://www.naturalearthdata.com")
        text.setLocation(x0, y0 + h + 30.0)
        text.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_46.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_46()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_46 => \(String(format: "%4lld", time1 - time0)) ms")
