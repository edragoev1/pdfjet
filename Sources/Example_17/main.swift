/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

///
/// Example_17.swift
///
public class Example_17 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_17.pdf", append: false)!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("PNG Images from PngSuite")

        var fileName = "PngSuite/BASN3P08.PNG"
        var fis = InputStream(fileAtPath: fileName)
        let image1 = try Image(pdf, fis!)
        image1.setAltDescription(
                "Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.")

        fileName = "PngSuite/BASN3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image2 = try Image(pdf, fis!)
        image2.setAltDescription(
                "A rainbow that runs from the bottom left to the top right, from a palette of 16 colors.")

        fileName = "PngSuite/BASN3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image3 = try Image(pdf, fis!)
        image3.setAltDescription(
                "A diagonal check of red, green, blue and yellow, from a palette of four colors.")

        fileName = "PngSuite/BASN3P01.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image4 = try Image(pdf, fis!)
        image4.setAltDescription(
                "A check of blue and yellow squares, from a palette of two colors.")


        fileName = "PngSuite/S01N3P01.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image5 = try Image(pdf, fis!)
        image5.setAltDescription(
                "The size test image of 1 by 1 pixels, from a palette of two colors.")


        fileName = "PngSuite/S02N3P01.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image6 = try Image(pdf, fis!)
        image6.setAltDescription(
                "The size test image of 2 by 2 pixels, from a palette of two colors.")

        fileName = "PngSuite/S03N3P01.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image7 = try Image(pdf, fis!)
        image7.setAltDescription(
                "The size test image of 3 by 3 pixels, from a palette of two colors.")

        fileName = "PngSuite/S04N3P01.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image8 = try Image(pdf, fis!)
        image8.setAltDescription(
                "The size test image of 4 by 4 pixels, from a palette of two colors.")

        fileName = "PngSuite/S05N3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image9 = try Image(pdf, fis!)
        image9.setAltDescription(
                "The size test image of 5 by 5 pixels: frames of color around its center, from a palette of four colors.")

        fileName = "PngSuite/S06N3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image10 = try Image(pdf, fis!)
        image10.setAltDescription(
                "The size test image of 6 by 6 pixels: frames of color around its center, from a palette of four colors.")

        fileName = "PngSuite/S07N3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image11 = try Image(pdf, fis!)
        image11.setAltDescription(
                "The size test image of 7 by 7 pixels: frames of color around its center, from a palette of four colors.")

        fileName = "PngSuite/S08N3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image12 = try Image(pdf, fis!)
        image12.setAltDescription(
                "The size test image of 8 by 8 pixels: frames of color around its center, from a palette of four colors.")

        fileName = "PngSuite/S09N3P02.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image13 = try Image(pdf, fis!)
        image13.setAltDescription(
                "The size test image of 9 by 9 pixels: frames of color around its center, from a palette of four colors.")



        fileName = "PngSuite/S32N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image14 = try Image(pdf, fis!)
        image14.setAltDescription(
                "The size test image of 32 by 32 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S33N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image15 = try Image(pdf, fis!)
        image15.setAltDescription(
                "The size test image of 33 by 33 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S34N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image16 = try Image(pdf, fis!)
        image16.setAltDescription(
                "The size test image of 34 by 34 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S35N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image17 = try Image(pdf, fis!)
        image17.setAltDescription(
                "The size test image of 35 by 35 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S36N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image18 = try Image(pdf, fis!)
        image18.setAltDescription(
                "The size test image of 36 by 36 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S37N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image19 = try Image(pdf, fis!)
        image19.setAltDescription(
                "The size test image of 37 by 37 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S38N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image20 = try Image(pdf, fis!)
        image20.setAltDescription(
                "The size test image of 38 by 38 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S39N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image21 = try Image(pdf, fis!)
        image21.setAltDescription(
                "The size test image of 39 by 39 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")

        fileName = "PngSuite/S40N3P04.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image22 = try Image(pdf, fis!)
        image22.setAltDescription(
                "The size test image of 40 by 40 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.")


        fileName = "images/qrcode.png"
        fis = InputStream(fileAtPath: fileName)
        let image23 = try Image(pdf, fis!)
        image23.setAltDescription(
                "A QR code for https://pdfjet.com.")


        fileName = "PngSuite/F00N2C08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image24 = try Image(pdf, fis!)
        image24.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 0, none.")

        fileName = "PngSuite/F01N2C08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image25 = try Image(pdf, fis!)
        image25.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 1, sub.")

        fileName = "PngSuite/F02N2C08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image26 = try Image(pdf, fis!)
        image26.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 2, up.")

        fileName = "PngSuite/F03N2C08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image27 = try Image(pdf, fis!)
        image27.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 3, average.")

        fileName = "PngSuite/F04N2C08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image28 = try Image(pdf, fis!)
        image28.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 4, Paeth.")


        fileName = "PngSuite/Z00N2C08.PNG"
        // color, no interlacing, compression level 0 (none)
        fis = InputStream(fileAtPath: fileName)
        let image29 = try Image(pdf, fis!)
        image29.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 0, which does not compress.")

        fileName = "PngSuite/Z03N2C08.PNG"
        // color, no interlacing, compression level 3
        fis = InputStream(fileAtPath: fileName)
        let image30 = try Image(pdf, fis!)
        image30.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 3.")

        fileName = "PngSuite/Z06N2C08.PNG"
        // color, no interlacing, compression level 6 (default)
        fis = InputStream(fileAtPath: fileName)
        let image31 = try Image(pdf, fis!)
        image31.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 6, the default.")

        fileName = "PngSuite/Z09N2C08.PNG"
        // color, no interlacing, compression level 9 (maximum)
        fis = InputStream(fileAtPath: fileName)
        let image32 = try Image(pdf, fis!)
        image32.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 9, the most.")


        fileName = "PngSuite/F00N0G08.PNG"
        // 8 bit greyscale, no interlacing, filter-type 0
        fis = InputStream(fileAtPath: fileName)
        let image33 = try Image(pdf, fis!)
        image33.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 0, none.")

        fileName = "PngSuite/F01N0G08.PNG"
        // 8 bit greyscale, no interlacing, filter-type 1
        fis = InputStream(fileAtPath: fileName)
        let image34 = try Image(pdf, fis!)
        image34.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 1, sub.")

        fileName = "PngSuite/F02N0G08.PNG"
        // 8 bit greyscale, no interlacing, filter-type 2
        fis = InputStream(fileAtPath: fileName)
        let image35 = try Image(pdf, fis!)
        image35.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 2, up.")

        fileName = "PngSuite/F03N0G08.PNG"
        // 8 bit greyscale, no interlacing, filter-type 3
        fis = InputStream(fileAtPath: fileName)
        let image36 = try Image(pdf, fis!)
        image36.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 3, average.")

        fileName = "PngSuite/F04N0G08.PNG"
        // 8 bit greyscale, no interlacing, filter-type 4
        fis = InputStream(fileAtPath: fileName)
        let image37 = try Image(pdf, fis!)
        image37.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 4, Paeth.")


        fileName = "PngSuite/BASN0G08.PNG"
        // 8 bit grayscale
        fis = InputStream(fileAtPath: fileName)
        let image38 = try Image(pdf, fis!)
        image38.setAltDescription(
                "Horizontal bands of gray, from black to white, 8 bits a pixel.")

        fileName = "PngSuite/BASN0G04.PNG"
        // 4 bit grayscale
        fis = InputStream(fileAtPath: fileName)
        let image39 = try Image(pdf, fis!)
        image39.setAltDescription(
                "A gradient of 16 grays from black at the top left to white at the bottom right, 4 bits a pixel.")

        fileName = "PngSuite/BASN0G02.PNG"
        // 2 bit grayscale
        fis = InputStream(fileAtPath: fileName)
        let image40 = try Image(pdf, fis!)
        image40.setAltDescription(
                "A diagonal check in four grays, 2 bits a pixel.")

        fileName = "PngSuite/BASN0G01.PNG"
        // Black and White image
        fis = InputStream(fileAtPath: fileName)
        let image41 = try Image(pdf, fis!)
        image41.setAltDescription(
                "The letters W and B on the two halves of a diagonal, in black and white, 1 bit a pixel.")


        fileName = "PngSuite/BGAN6A08.PNG"
        // Image with alpha transparency
        fis = InputStream(fileAtPath: fileName)
        let image42 = try Image(pdf, fis!)
        image42.setAltDescription(
                "A rainbow that shades from red at the top to blue at the bottom, in 8 bit color with an alpha channel.")


        fileName = "PngSuite/OI1N2C16.PNG"
        // Color image with 1 IDAT chunk
        fis = InputStream(fileAtPath: fileName)
        let image43 = try Image(pdf, fis!)
        image43.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in one IDAT chunk.")

        fileName = "PngSuite/OI2N2C16.PNG"
        // Color image with 2 IDAT chunks
        fis = InputStream(fileAtPath: fileName)
        let image44 = try Image(pdf, fis!)
        image44.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in two IDAT chunks.")

        fileName = "PngSuite/OI4N2C16.PNG"
        // Color image with 4 IDAT chunks
        fis = InputStream(fileAtPath: fileName)
        let image45 = try Image(pdf, fis!)
        image45.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in four IDAT chunks.")

        fileName = "PngSuite/OI9N2C16.PNG"
        // IDAT chunks with length == 1
        fis = InputStream(fileAtPath: fileName)
        let image46 = try Image(pdf, fis!)
        image46.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in IDAT chunks of one byte.")


        fileName = "PngSuite/OI1N0G16.PNG"
        // Grayscale image with 1 IDAT chunk
        fis = InputStream(fileAtPath: fileName)
        let image47 = try Image(pdf, fis!)
        image47.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in one IDAT chunk.")

        fileName = "PngSuite/OI2N0G16.PNG"
        // Grayscale image with 2 IDAT chunks
        fis = InputStream(fileAtPath: fileName)
        let image48 = try Image(pdf, fis!)
        image48.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in two IDAT chunks.")

        fileName = "PngSuite/OI4N0G16.PNG"
        // Grayscale image with 4 IDAT chunks
        fis = InputStream(fileAtPath: fileName)
        let image49 = try Image(pdf, fis!)
        image49.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in four IDAT chunks.")

        fileName = "PngSuite/OI9N0G16.PNG"
        // IDAT chunks with length == 1
        fis = InputStream(fileAtPath: fileName)
        let image50 = try Image(pdf, fis!)
        image50.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in IDAT chunks of one byte.")


        fileName = "PngSuite/TBBN3P08.PNG"  // Transparent, black background chunk
        fis = InputStream(fileAtPath: fileName)
        let image51 = try Image(pdf, fis!)
        image51.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a black background color in the file.")

        fileName = "PngSuite/TBGN3P08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image52 = try Image(pdf, fis!)
        image52.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a gray background color in the file.")

        fileName = "PngSuite/TBWN3P08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image53 = try Image(pdf, fis!)
        image53.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a white background color in the file.")

        fileName = "PngSuite/TBYN3P08.PNG"
        fis = InputStream(fileAtPath: fileName)
        let image54 = try Image(pdf, fis!)
        image54.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a yellow background color in the file.")


        fileName = "images/rgba-8bit-chunks.png"
        fis = InputStream(fileAtPath: fileName)
        let image55 = try Image(pdf, fis!)
        image55.setAltDescription(
                "Three half transparent circles in red, green and blue that overlap, beside the heading 8-bit RGBA PNG, 380 by 100, and the note that the image has anti-aliased text and half transparent circles on a transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME and two IDAT chunks.")


        let page = Page(pdf, A4.PORTRAIT)

        image1.setLocation(100.0, 80.0)
        image1.drawOn(page)

        image2.setLocation(100.0, 120.0)
        image2.drawOn(page)

        image3.setLocation(100.0, 160.0)
        image3.drawOn(page)

        image4.setLocation(100.0, 200.0)
        image4.drawOn(page)


        image5.setLocation(200.0, 80.0)
        image5.drawOn(page)

        image6.setLocation(200.0, 120.0)
        image6.drawOn(page)

        image7.setLocation(200.0, 160.0)
        image7.drawOn(page)

        image8.setLocation(200.0, 200.0)
        image8.drawOn(page)

        image9.setLocation(200.0, 240.0)
        image9.drawOn(page)

        image10.setLocation(200.0, 280.0)
        image10.drawOn(page)

        image11.setLocation(200.0, 320)
        image11.drawOn(page)

        image12.setLocation(200.0, 360.0)
        image12.drawOn(page)

        image13.setLocation(200.0, 400.0)
        image13.drawOn(page)


        image14.setLocation(300.0, 80.0)
        image14.drawOn(page)

        image15.setLocation(300.0, 120.0)
        image15.drawOn(page)

        image16.setLocation(300.0, 160.0)
        image16.drawOn(page)

        image17.setLocation(300.0, 200.0)
        image17.drawOn(page)

        image18.setLocation(300.0, 240.0)
        image18.drawOn(page)

        image19.setLocation(300.0, 280.0)
        image19.drawOn(page)

        image20.setLocation(300.0, 320)
        image20.drawOn(page)

        image21.setLocation(300.0, 360.0)
        image21.drawOn(page)

        image22.setLocation(300.0, 400.0)
        image22.drawOn(page)


        image23.setLocation(350.0, 50.0)
        image23.drawOn(page)

        image24.setLocation(100.0, 650)
        image24.drawOn(page)

        image25.setLocation(140.0, 650)
        image25.drawOn(page)

        image26.setLocation(180.0, 650)
        image26.drawOn(page)

        image27.setLocation(220.0, 650)
        image27.drawOn(page)

        image28.setLocation(260.0, 650)
        image28.drawOn(page)


        image29.setLocation(300.0, 650)
        image29.drawOn(page)

        image30.setLocation(340.0, 650)
        image30.drawOn(page)

        image31.setLocation(380.0, 650)
        image31.drawOn(page)

        image32.setLocation(420.0, 650)
        image32.drawOn(page)


        image33.setLocation(100.0, 700.0)
        image33.drawOn(page)

        image34.setLocation(140.0, 700.0)
        image34.drawOn(page)

        image35.setLocation(180.0, 700.0)
        image35.drawOn(page)

        image36.setLocation(220.0, 700.0)
        image36.drawOn(page)

        image37.setLocation(260.0, 700.0)
        image37.drawOn(page)


        image38.setLocation(300.0, 700.0)
        image38.drawOn(page)

        image39.setLocation(340.0, 700.0)
        image39.drawOn(page)

        image40.setLocation(380.0, 700.0)
        image40.drawOn(page)

        image41.setLocation(420.0, 700.0)
        image41.drawOn(page)


        image42.setLocation(100.0, 750.0)
        image42.drawOn(page)


        image43.setLocation(140.0, 750.0)
        image43.drawOn(page)

        image44.setLocation(180.0, 750.0)
        image44.drawOn(page)

        image45.setLocation(220.0, 750.0)
        image45.drawOn(page)

        image46.setLocation(260.0, 750.0)
        image46.drawOn(page)


        image47.setLocation(300.0, 750.0)
        image47.drawOn(page)

        image48.setLocation(340.0, 750.0)
        image48.drawOn(page)

        image49.setLocation(380.0, 750.0)
        image49.drawOn(page)

        image50.setLocation(420.0, 750.0)
        image50.drawOn(page)


        image51.setLocation(300.0, 800.0)
        image51.drawOn(page)

        image52.setLocation(340.0, 800.0)
        image52.drawOn(page)

        image53.setLocation(380.0, 800.0)
        image53.drawOn(page)

        image54.setLocation(420.0, 800.0)
        image54.drawOn(page)


        image55.setLocation(100.0, 500.0)
        image55.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_17.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_17()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_17 => \(String(format: "%4lld", time1 - time0)) ms")
