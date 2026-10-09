/**
 * Image.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Used to create image objects and draw them on a page.
/// The image type can be one of the following:
/// ImageType.JPG, ImageType.PNG or ImageType.BMP
///
/// Please see Example_03 and Example_24.
///
public class Image : Drawable {
    var objNumber: Int?
    // The identity of the PDF the image was added to, or nil for an image of an existing PDF.
    var pdfIdentity: UUID?

    var x: Float = 0.0      // Position of the image on the page
    var y: Float = 0.0
    var w: Float?           // Image width
    var h: Float?           // Image height
    // The pixels of the image across and down, which the image object is
    // written with: a Float holds every whole number only up to 2^24.
    private var pixelWidth = 0
    private var pixelHeight = 0

    var uri: String?
    var key: String?

    private var degrees = 0
    private var flipUpsideDown = false
    // The Exif orientation of a JPEG, 2 to 8, or 0; w and h are its size as seen.
    private var orientation = 0
    // True for a CMYK JPEG that Adobe software wrote, with its inks inverted.
    private var invertedInks = false
    // The /Mask of the transparent color of a grayscale or truecolor PNG, or
    // nil.
    private var colorKeyMask: [Int]?
    // The colors of a pixel of a PNG whose stream is its IDAT data, for the
    // /DecodeParms that undo the PNG filters of its rows, or 0.
    private var decodeColors = 0
    // The colors of the /Indexed color space of a palette PNG, or nil.
    private var palette: [UInt8]?

    private var language: String?
    private var altDescription: String?
    private var actualText: String?

    enum ImageError: Error {
    case format(String)
        case rotation(String)
    }

    ///
    /// The main constructor for the Image class.
    ///
    /// - Parameter pdf: the PDF to which we add this image.
    /// - Parameter filePath: the path to the image file.
    ///
    public convenience init(_ pdf: PDF, _ filePath: String) throws {
        guard let stream = InputStream(fileAtPath: filePath),
                FileManager.default.fileExists(atPath: filePath) else {
            throw PDFjetError(message: "File not found: " + filePath)
        }
        try self.init(pdf, stream)
    }

    ///
    /// The main constructor for the Image class.
    ///
    /// - Parameter pdf: the PDF to which we add this image.
    /// - Parameter stream: the input stream to read the image from.
    ///
    public convenience init(_ pdf: PDF, _ stream: InputStream) throws {
        try self.init(pdf, try Content.getFromStream(stream))
    }

    private init(_ pdf: PDF, _ bytes: [UInt8]) throws {
        self.pdfIdentity = pdf.identity
        let imageType = try Image.typeOf(bytes)
        let stream = InputStream(data: Data(bytes))
        if imageType == ImageType.JPG {
            let jpg = try JPGImage(bytes)
            setPixels(Int(jpg.getWidth()), Int(jpg.getHeight()))
            if jpg.getColorComponents() == 1 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceGray", 8)
            } else if jpg.getColorComponents() == 3 {
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceRGB", 8)
            } else if jpg.getColorComponents() == 4 {
                invertedInks = jpg.isAdobe()
                addImage(pdf, jpg.getData(), [UInt8](), imageType, "DeviceCMYK", 8)
            }
            setPhysicalSize(jpg.getPhysicalWidth(), jpg.getPhysicalHeight())
            setOrientation(jpg.orientation)
        } else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            setPixels(png.getWidth(), png.getHeight())
            colorKeyMask = png.getColorKeyMask()
            decodeColors = png.decodeColors
            palette = png.palette
            let (colorSpace, bitsPerComponent) = png.getColorSpace()
            addImage(pdf, png.stream, png.getAlpha() ?? [UInt8](), imageType, colorSpace, bitsPerComponent)
            setPhysicalSize(png.getPhysicalWidth(), png.getPhysicalHeight())
        } else if imageType == ImageType.BMP {
            let bmp = try BMPImage(stream)
            setPixels(bmp.getWidth(), bmp.getHeight())
            addImage(pdf, bmp.getData(), bmp.getAlpha() ?? [UInt8](), imageType, "DeviceRGB", 8)
            setPhysicalSize(bmp.getPhysicalWidth(), bmp.getPhysicalHeight())
        }
    }

    ///
    /// Constructor used to attach images to existing PDF.
    ///
    /// - Parameter objects: the map to which we add this image.
    /// - Parameter stream: the input stream to read the image from.
    ///
    public convenience init(_ objects: inout [PDFobj], _ stream: InputStream) throws {
        try self.init(&objects, try Content.getFromStream(stream))
    }

    private init(_ objects: inout [PDFobj], _ bytes: [UInt8]) throws {
        let imageType = try Image.typeOf(bytes)
        let stream = InputStream(data: Data(bytes))
        var data: [UInt8]
        var alpha = [UInt8]()
        if imageType == ImageType.JPG {
            let jpg = try JPGImage(bytes)
            data = jpg.getData()
            setPixels(Int(jpg.getWidth()), Int(jpg.getHeight()))
            if jpg.getColorComponents() == 1 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceGray", 8)
            } else if jpg.getColorComponents() == 3 {
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
            } else if jpg.getColorComponents() == 4 {
                invertedInks = jpg.isAdobe()
                addImageToObjects(&objects, &data, &alpha, imageType, "DeviceCMYK", 8)
            }
            setPhysicalSize(jpg.getPhysicalWidth(), jpg.getPhysicalHeight())
            setOrientation(jpg.orientation)
        } else if imageType == ImageType.PNG {
            let png = try PNGImage(stream)
            data = png.stream
            alpha = png.getAlpha() ?? [UInt8]()
            setPixels(png.getWidth(), png.getHeight())
            colorKeyMask = png.getColorKeyMask()
            decodeColors = png.decodeColors
            palette = png.palette
            let (colorSpace, bitsPerComponent) = png.getColorSpace()
            addImageToObjects(&objects, &data, &alpha, imageType, colorSpace, bitsPerComponent)
            setPhysicalSize(png.getPhysicalWidth(), png.getPhysicalHeight())
        } else if imageType == ImageType.BMP {
            let bmp = try BMPImage(stream)
            data = bmp.getData()
            alpha = bmp.getAlpha() ?? [UInt8]()
            setPixels(bmp.getWidth(), bmp.getHeight())
            addImageToObjects(&objects, &data, &alpha, imageType, "DeviceRGB", 8)
            setPhysicalSize(bmp.getPhysicalWidth(), bmp.getPhysicalHeight())
        }
    }

    /// Creates an image from an image object read from an existing PDF.
    public init(_ pdf: PDF, _ obj: PDFobj) throws {
        self.pdfIdentity = pdf.identity
        setPixels(Int(Double(obj.getValue("/Width")) ?? 0), Int(Double(obj.getValue("/Height")) ?? 0))
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter ")
        pdf.append(obj.getValue("/Filter"))
        pdf.append("\n")
        pdf.append("/Width ")
        pdf.append(pixelWidth)
        pdf.append("\n")
        pdf.append("/Height ")
        pdf.append(pixelHeight)
        pdf.append("\n")
        let colorSpace = obj.getValue("/ColorSpace")
        if colorSpace != "" {
            pdf.append("/ColorSpace ")
            pdf.append(colorSpace)
            pdf.append("\n")
        }
        pdf.append("/BitsPerComponent ")
        pdf.append(obj.getValue("/BitsPerComponent"))
        pdf.append("\n")
        let decodeParms = obj.getValue("/DecodeParms")
        if decodeParms != "" {
            pdf.append("/DecodeParms ")
            pdf.append(decodeParms)
            pdf.append("\n")
        }
        let imageMask = obj.getValue("/ImageMask")
        if imageMask != "" {
            pdf.append("/ImageMask ")
            pdf.append(imageMask)
            pdf.append("\n")
        }
        pdf.append("/Length ")
        pdf.append(obj.stream!.count)
        pdf.append("\n")
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(obj.stream!, 0, obj.stream!.count)
        pdf.append(Token.endStream)
        pdf.endObj()
        pdf.images.append(self)
        objNumber = pdf.getObjNumber()
    }

    // Sets the pixels of the image across and down, which it is drawn at, a
    // pixel to a point, unless the file asks for another size.
    private func setPixels(_ width: Int, _ height: Int) {
        pixelWidth = width
        pixelHeight = height
        w = Float(width)
        h = Float(height)
    }

    ///
    // Draws the image at the size the file asks for, when it asks for one: the
    // pHYs chunk of a PNG, the JFIF density of a JPEG or the pixels per metre
    // of a BMP.
    // The width and the height are the pixels of the image until here, which
    // is what the image object of the PDF is written with, and are the size it
    // is drawn at from here on.
    private func setPhysicalSize(_ width: Float, _ height: Float) {
        if width > 0.0 && height > 0.0 {
            self.w = width
            self.h = height
        }
    }

    // Keeps the Exif orientation of a JPEG, which is drawn as it is meant to be
    // seen: turned a quarter of the way (5 to 8), its size as seen is its
    // height by its width. 1, upright as stored, is drawn as without one.
    private func setOrientation(_ orientation: Int) {
        if orientation < 2 || orientation > 8 {
            return
        }
        self.orientation = orientation
        if orientation >= 5 {
            swap(&self.w, &self.h)
        }
    }

    // Turn or flip the unit square of an image as stored into the image as
    // seen, for the Exif orientations 2 to 8.
    private static let orientationMatrices = [
        "",
        "",
        "-1 0 0 1 1 0 cm\n",
        "-1 0 0 -1 1 1 cm\n",
        "1 0 0 -1 0 1 cm\n",
        "0 -1 -1 0 1 1 cm\n",
        "0 -1 1 0 0 1 cm\n",
        "0 1 1 0 0 0 cm\n",
        "0 1 -1 0 1 0 cm\n",
    ]

    /// Sets the location of this image on the page to (x, y).
    ///
    /// - Parameter x: the x coordinate of the top left corner of the image.
    /// - Parameter y: the y coordinate of the top left corner of the image.
    ///
    @discardableResult
    public func setLocation(_ x: Float, _ y: Float) -> Self {
        self.x = x
        self.y = y
        return self
    }

    ///
    /// Scales this image by the specified factor.
    ///
    /// - Parameter factor: the factor used to scale the image.
    ///
    @discardableResult
    public func scaleBy(_ factor: Float) -> Image {
        return self.scaleBy(factor, factor)
    }

    ///
    /// Scales this image by the specified width and height factor.
    ///
    /// *Author:* **Pieter Libin**, pieter@emweb.be
    ///
    /// - Parameter widthFactor: the factor used to scale the width of the image
    /// - Parameter heightFactor: the factor used to scale the height of the image
    ///
    @discardableResult
    public func scaleBy(_ widthFactor: Float, _ heightFactor: Float) -> Image {
        self.w! *= widthFactor
        self.h! *= heightFactor
        return self
    }

    /// Scales this image proportionally to the specified width.
    @discardableResult
    public func resizeWidth(_ width: Float) -> Image {
        let factor = width / getWidth()
        return self.scaleBy(factor, factor)
    }

    /// Scales this image proportionally to the specified height.
    @discardableResult
    public func resizeHeight(_ height: Float) -> Image {
        let factor = height / getHeight()
        return self.scaleBy(factor, factor)
    }

    ///
    /// Sets the URI for the "click box" action.
    ///
    /// - Parameter uri: the URI
    ///
    @discardableResult
    public func setURIAction(_ uri: String) -> Image {
        self.uri = uri
        return self
    }

    ///
    /// Sets the destination key for the action.
    ///
    /// - Parameter key: the destination name.
    ///
    @discardableResult
    public func setGoToAction(_ key: String) -> Image {
        self.key = key
        return self
    }

    /// Rotates this image clockwise by 0, 90, 180 or 270 degrees, as every rotation in PDFjet turns.
    @discardableResult
    public func setRotation(_ degrees: Int) throws -> Image {
        if degrees != 0 && degrees != 90 && degrees != 180 && degrees != 270 {
            throw ImageError.rotation("The rotation angle must be 0, 90, 180 or 270")
        }
        self.degrees = degrees
        return self
    }

    ///
    /// Sets the alternate description of this image.
    ///
    /// - Parameter altDescription: the alternate description of the image.
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setAltDescription(_ altDescription: String) -> Image {
        self.altDescription = altDescription
        return self
    }

    ///
    /// Sets the actual text for this image.
    ///
    /// - Parameter actualText: the actual text for the image.
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setActualText(_ actualText: String) -> Image {
        self.actualText = actualText
        return self
    }

    ///
    /// Sets the language of this image.
    ///
    /// - Parameter language: the language, for example "en-US".
    /// - Returns: this Image.
    ///
    @discardableResult
    public func setLanguage(_ language: String) -> Image {
        self.language = language
        return self
    }

    ///
    /// Draws this image on the specified page.
    ///
    /// - Parameter page: the page to draw this image on.
    /// - Returns: x and y coordinates of the bottom right corner of this component.
    ///
    @discardableResult
    public func drawOn(_ page: Page?) -> [Float] {
        guard let page = page else {
            return [x + w!, y + h!]     // Measured, not drawn
        }
        if let identity = pdfIdentity, identity != page.pdf.identity {
            page.pdf.fail("The image belongs to another PDF.")
            return [x + w!, y + h!]
        }
        if w! == 0.0 || h! == 0.0 {
            return [x + w!, y + h!]     // A zero size image paints nothing.
        }
        guard let objNumber = objNumber else {
            return [x + w!, y + h!]     // The PDF refused the image, and has failed.
        }
        // A linked image is a Figure in the Link its annotation joins
        var link: StructElement?
        if uri != nil || key != nil {
            link = page.beginLink()
        }
        page.addBDC(StructElem.FIGURE, language, actualText, altDescription)
        page.saveGraphicsState()

        if degrees == 0 {
            page.append(w!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(h!)
            page.append(Token.space)
            page.append(x)
            page.append(Token.space)
            page.append(page.height - (y + h!))
            page.append(" cm\n")
        } else if degrees == 90 {
            page.append(h!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(w!)
            page.append(Token.space)
            page.append(x)
            page.append(Token.space)
            page.append(page.height - y)
            page.append(" cm\n")
            page.append("0 -1 1 0 0 0 cm\n")
        } else if degrees == 180 {
            page.append(w!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(h!)
            page.append(Token.space)
            page.append(x + w!)
            page.append(Token.space)
            page.append(page.height - y)
            page.append(" cm\n")
            page.append("-1 0 0 -1 0 0 cm\n")
        } else if degrees == 270 {
            page.append(h!)
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(Float(0.0))
            page.append(Token.space)
            page.append(w!)
            page.append(Token.space)
            page.append(x + h!)
            page.append(Token.space)
            page.append(page.height - (y + w!))
            page.append(" cm\n")
            page.append("0 1 -1 0 0 0 cm\n")
        }

        if flipUpsideDown {
            page.append("1 0 0 -1 0 1 cm\n")
        }
        if orientation != 0 {
            page.append(Image.orientationMatrices[orientation])
        }

        page.append("/Im")
        page.append(objNumber)
        page.append(" Do\n")

        page.restoreGraphicsState()

        // Turned a quarter of the way, the image is as wide as it is tall unturned
        if degrees == 90 || degrees == 270 {
            page.setFigureBoundingBox(x, y, h!, w!)
        } else {
            page.setFigureBoundingBox(x, y, w!, h!)
        }
        page.addEMC()

        page.endLink(link)
        if uri != nil || key != nil {
            // The link covers the image as it is drawn, turned or not.
            let turned = degrees == 90 || degrees == 270
            page.addAnnotation(Annotation(
                    Annotation.Link,
                    x,
                    y,
                    x + (turned ? h! : w!),
                    y + (turned ? w! : h!),
                    nil,    // Vertices
                    nil,    // Fill Color
                    0.0,    // Opacity
                    nil,    // Title
                    nil,    // Contents
                    uri,
                    key,    // The destination name
                    language,
                    actualText,
                    altDescription).joining(link))
        }

        return [x + w!, y + h!]
    }

    ///
    /// Returns the width of this image when drawn on the page.
    /// The scaling is take into account.
    ///
    /// - Returns: w - the width of this image.
    ///
    public func getWidth() -> Float {
        return self.w!
    }

    ///
    /// Returns the height of this image when drawn on the page.
    /// The scaling is take into account.
    ///
    /// - Returns: h - the height of this image.
    ///
    public func getHeight() -> Float {
        return self.h!
    }

    private func addSoftMask(
            _ pdf: PDF,
            _ alpha: [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) {
        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        pdf.append("/Filter /FlateDecode\n")
        pdf.append("/Width ")
        pdf.append(pixelWidth)
        pdf.append(Token.newline)
        pdf.append("/Height ")
        pdf.append(pixelHeight)
        pdf.append(Token.newline)
        pdf.append("/ColorSpace /")
        pdf.append(colorSpace)
        pdf.append(Token.newline)
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append(Token.newline)
        let buf = pdf.encrypted(alpha)
        pdf.append("/Length ")
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf, 0, buf.count)
        pdf.append(Token.endStream)
        pdf.endObj()
        objNumber = pdf.getObjNumber()
    }

    private func addImage(
            _ pdf: PDF,
            _ data: [UInt8],
            _ alpha: [UInt8],
            _ imageType: ImageType,
            _ colorSpace: String,
            _ bitsPerComponent: Int)  {
        if !isPDFA(pdf, alpha, colorSpace, bitsPerComponent) {
            return
        }
        if alpha.count > 0 {
            addSoftMask(pdf, alpha, "DeviceGray", 8)
        }

        pdf.newObj()
        pdf.append(Token.beginDictionary)
        pdf.append("/Type /XObject\n")
        pdf.append("/Subtype /Image\n")
        if imageType == ImageType.JPG {
            pdf.append("/Filter /DCTDecode\n")
        } else {
            pdf.append("/Filter /FlateDecode\n")
            if alpha.count > 0 {
                pdf.append("/SMask ")
                pdf.append(objNumber!)
                pdf.append(" 0 R\n")
            } else if let mask = colorKeyMask {
                pdf.append("/Mask [" + mask.map { String($0) }.joined(separator: " ") + "]\n")
            }
        }
        pdf.append("/Width ")
        pdf.append(pixelWidth)
        pdf.append(Token.newline)
        pdf.append("/Height ")
        pdf.append(pixelHeight)
        pdf.append(Token.newline)
        if let palette = palette {
            pdf.append("/ColorSpace [/Indexed /DeviceRGB ")
            pdf.append(palette.count/3 - 1)
            pdf.append(" <")
            pdf.append(pdf.toHex(pdf.encrypted(palette)))
            pdf.append(">]\n")
        } else {
            pdf.append("/ColorSpace /")
            pdf.append(colorSpace)
            pdf.append(Token.newline)
        }
        pdf.append("/BitsPerComponent ")
        pdf.append(bitsPerComponent)
        pdf.append(Token.newline)
        if colorSpace == "DeviceCMYK" && invertedInks {
            // Adobe software, Photoshop among them, stores the inks inverted.
            pdf.append("/Decode [1.0 0.0 1.0 0.0 1.0 0.0 1.0 0.0]\n")
        }
        if decodeColors != 0 {
            // The rows of a PNG, each with the filter type of its PNG filter.
            pdf.append("/DecodeParms <</Predictor 15 /Colors ")
            pdf.append(decodeColors)
            pdf.append(" /BitsPerComponent ")
            pdf.append(bitsPerComponent)
            pdf.append(" /Columns ")
            pdf.append(pixelWidth)
            pdf.append(">>\n")
        }
        let buf = pdf.encrypted(data)
        pdf.append("/Length ")
        pdf.append(buf.count)
        pdf.append(Token.newline)
        pdf.append(Token.endDictionary)
        pdf.append(Token.stream)
        pdf.append(buf, 0, buf.count)
        pdf.append(Token.endStream)
        pdf.endObj()
        pdf.images.append(self)
        self.objNumber = pdf.getObjNumber()
    }

    // Returns true if a PDF/A document can hold the image, and fails the
    // document when it cannot. Its output intent is sRGB, an RGB profile, so
    // its images are gray or RGB and not CMYK, as ISO 19005 asks of a device
    // color space. PDF/A-1 is PDF 1.4, which has no soft masks, and 8 bits per
    // component at most. Any other document holds any image.
    private func isPDFA(
            _ pdf: PDF,
            _ alpha: [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) -> Bool {
        let level = pdf.compliance
        if level == Compliance.PDF_1_7 || level == Compliance.PDF_UA_1 {
            return true
        }
        let pdfA1 = level == Compliance.PDF_A_1A || level == Compliance.PDF_A_1B
        if colorSpace == "DeviceCMYK" {
            pdf.fail("A document of \(level) cannot hold a CMYK image: "
                    + "its output intent is sRGB, so its images are gray or RGB.")
            return false
        }
        if pdfA1 && !alpha.isEmpty {
            pdf.fail("A document of \(level) cannot hold an image with transparency: "
                    + "PDF/A-1 has no soft masks, so its images are opaque.")
            return false
        }
        if pdfA1 && bitsPerComponent > 8 {
            pdf.fail("A document of \(level) cannot hold an image of "
                    + "\(bitsPerComponent) bits per component: PDF/A-1 has 8 at most.")
            return false
        }
        return true
    }

    private func addSoftMask2(
            _ objects: inout [PDFobj],
            _ data: inout [UInt8],
            _ colorSpace: String,
            _ bitsPerComponent: Int) {
        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/XObject")
        obj.dict.append("/Subtype")
        obj.dict.append("/Image")
        obj.dict.append("/Filter")
        obj.dict.append("/FlateDecode")
        obj.dict.append("/Width")
        obj.dict.append(String(pixelWidth))
        obj.dict.append("/Height")
        obj.dict.append(String(pixelHeight))
        obj.dict.append("/ColorSpace")
        obj.dict.append("/" + colorSpace)
        obj.dict.append("/BitsPerComponent")
        obj.dict.append(String(bitsPerComponent))
        obj.dict.append("/Length")
        obj.dict.append(String(data.count))
        obj.dict.append(">>")
        obj.setStream(&data)
        obj.number = objects.count + 1
        objects.append(obj)
        objNumber = obj.number
    }

    func addImageToObjects(
            _ objects: inout [PDFobj],
            _ data: inout [UInt8],
            _ alpha: inout [UInt8],
            _ imageType: ImageType,
            _ colorSpace: String,
            _ bitsPerComponent: Int) {
        if !alpha.isEmpty {
            addSoftMask2(&objects, &alpha, "DeviceGray", 8)
        }

        let obj = PDFobj()
        obj.dict.append("<<")
        obj.dict.append("/Type")
        obj.dict.append("/XObject")
        obj.dict.append("/Subtype")
        obj.dict.append("/Image")
        if imageType == ImageType.JPG {
            obj.dict.append("/Filter")
            obj.dict.append("/DCTDecode")
        } else if imageType == ImageType.PNG ||
                imageType == ImageType.BMP {
            obj.dict.append("/Filter")
            obj.dict.append("/FlateDecode")
            if !alpha.isEmpty {
                obj.dict.append("/SMask")
                obj.dict.append(String(objNumber!))
                obj.dict.append("0")
                obj.dict.append("R")
            } else if let mask = colorKeyMask {
                obj.dict.append("/Mask")
                obj.dict.append("[")
                obj.dict.append(contentsOf: mask.map { String($0) })
                obj.dict.append("]")
            }
        }
        obj.dict.append("/Width")
        obj.dict.append(String(pixelWidth))
        obj.dict.append("/Height")
        obj.dict.append(String(pixelHeight))
        obj.dict.append("/ColorSpace")
        if let palette = palette {
            obj.dict.append(contentsOf: ["[", "/Indexed", "/DeviceRGB"])
            obj.dict.append(String(palette.count/3 - 1))
            obj.dict.append("<" + palette.map { String(format: "%02x", $0) }.joined() + ">")
            obj.dict.append("]")
        } else {
            obj.dict.append("/" + colorSpace)
        }
        obj.dict.append("/BitsPerComponent")
        obj.dict.append(String(bitsPerComponent))
        if colorSpace == "DeviceCMYK" && invertedInks {
            // Adobe software, Photoshop among them, stores the inks inverted.
            obj.dict.append("/Decode")
            obj.dict.append("[")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("1.0")
            obj.dict.append("0.0")
            obj.dict.append("]")
        }
        if decodeColors != 0 {
            // The rows of a PNG, each with the filter type of its PNG filter.
            obj.dict.append(contentsOf: ["/DecodeParms", "<<"])
            obj.dict.append(contentsOf: ["/Predictor", "15"])
            obj.dict.append(contentsOf: ["/Colors", String(decodeColors)])
            obj.dict.append(contentsOf: ["/BitsPerComponent", String(bitsPerComponent)])
            obj.dict.append(contentsOf: ["/Columns", String(pixelWidth)])
            obj.dict.append(">>")
        }
        obj.dict.append("/Length")
        obj.dict.append(String(data.count))
        obj.dict.append(">>")
        obj.setStream(&data)
        obj.number = objects.count + 1
        objects.append(obj)

        objNumber = obj.number
    }

    /// Scales this image to fit between its location and the bottom right corner of the page.
    public func resizeToFit(_ page: Page, keepAspectRatio: Bool) {
        if keepAspectRatio {
            self.scaleBy(min((page.width - self.x)/self.w!, (page.height - self.y)/self.h!))
        } else {
            self.scaleBy((page.width - self.x)/self.w!, (page.height - self.y)/self.h!)
        }
    }

    /// Sets whether this image is drawn upside down.
    @discardableResult
    public func setFlipUpsideDown(_ flipUpsideDown: Bool) -> Image {
        self.flipUpsideDown = flipUpsideDown
        return self
    }

    /// Returns the type of the image from its first bytes.
    static func typeOf(_ bytes: [UInt8]) throws -> ImageType {
        if bytes.count >= 4 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47 {
            return ImageType.PNG
        }
        if bytes.count >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8 {
            return ImageType.JPG
        }
        if bytes.count >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D {
            return ImageType.BMP
        }
        throw ImageError.format("The image is not a PNG, JPEG or BMP file.")
    }
}   // End of Image.swift
