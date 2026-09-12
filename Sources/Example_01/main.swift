import Foundation
import PDFjet

public class Example_01 {

    // Initializes the PDF creation process
    public init() throws {
        // Create an output stream to write the PDF to a file
        let stream = OutputStream(toFileAtPath: "Example_01.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        // pdf.setCompliance(Compliance.PDF_A_1A)
        // pdf.setCompliance(Compliance.PDF_A_1B)
        // pdf.setCompliance(Compliance.PDF_A_2A)
        // pdf.setCompliance(Compliance.PDF_A_2B)
        // pdf.setCompliance(Compliance.PDF_A_3A)
        // pdf.setCompliance(Compliance.PDF_A_3B)
        pdf.setTitle("Document containing English, Greek and Bulgarian text blocks.")

        // Load the font (IBMPlexSans Regular) for use in the document
        let font = try Font(pdf, IBMPlexSans.Regular)
        font.setSize(12.0)

        // Create a new page with Portrait orientation
        let page = Page(pdf, Letter.PORTRAIT)

        var map = [String: Int32]()
        map["Everyone"] = Color.darkred
        map["Pay"] = Color.darkgreen
        map["Freedom"] = Color.blue

        // Read English text from a file
        var textBlock = TextBlock(
                font, try Content.ofTextFile("data/languages/english.txt"))
        textBlock.setLocation(50, 50)   // Set the position for the English text
        textBlock.setWidth(473)         // Why 473f? To match the Google Fonts samples.
        textBlock.setTextPadding(10)    // Set padding around the text
        textBlock.setBorderColor(Color.blue)
        textBlock.setKeywordHighlightColors(map)
        var xy = textBlock.drawOn(page) // Draw the English text on the page and get coordinates

        // Draw a small blue rectangle for testing ...
        let rect = Rect(xy[0], xy[1], 30, 30)
        rect.setBorderColor(Color.blue)
        rect.drawOn(page)

        // Read Greek text from a file and draw it on the page
        textBlock = TextBlock(font, try Content.ofTextFile("data/languages/greek.txt"))
        textBlock.setLocation(50, xy[1] + 30)   // Set location below the previous text
        textBlock.setWidth(473)                 // Set width for Greek text block
        textBlock.setTextPadding(10)            // Set padding around the Greek text
        xy = textBlock.drawOn(page)             // Draw Greek text and update coordinates

        // Read Bulgarian text from a file and draw it with a blue border and rounded corners
        textBlock = TextBlock(font, try Content.ofTextFile("data/languages/bulgarian.txt"))
        textBlock.setLocation(50, xy[1] + 30)   // Set location below Greek text
        textBlock.setWidth(473)                 // Set width for Bulgarian text block
        textBlock.setTextPadding(10)            // Set padding around the Bulgarian text
        textBlock.setBorderColor(Color.blue)    // Blue border for the Bulgarian text
        textBlock.setBorderCornerRadius(10)     // Set rounded corners for the border
        textBlock.setUnderline(true)            // Underline the Bulgarian text
        textBlock.drawOn(page)                  // Draw the Bulgarian text

        // Finalize the PDF creation
        pdf.complete()
    }
}

// Entry point for execution
let time0 = Int64(Date().timeIntervalSince1970 * 1000) // Record start time
_ = try Example_01()  // Create the PDF
let time1 = Int64(Date().timeIntervalSince1970 * 1000) // Record end time
TextUtils.printDuration("Example_01", time0, time1)    // Print the execution duration
