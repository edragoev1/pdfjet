import Foundation
import PDFjet

public class Example_01 {

    // Initializes the PDF creation process
    public init() throws {
        // Create an output stream to write the PDF to a file
        let stream = OutputStream(toFileAtPath: "Example_01.pdf", append: false)
        let pdf = PDF(stream!)
        pdf.setCompliance(Compliance.PDF_UA_1)
        pdf.setTitle("Document containing English, Greek and Bulgarian text blocks.")

        // Load the font (IBMPlexSans Regular) for use in the document
        let font = try Font(pdf, IBMPlexSans.Regular)

        // Create a new page with Portrait orientation
        let page = Page(pdf, Letter.PORTRAIT)

        // Read English text from a file
        let englishText = try String(
                contentsOfFile: "data/languages/english.txt", encoding: .utf8)
        let textBlock = TextBlock(font, englishText)
        textBlock.setLocation(50, 50)   // Set the position for the English text
        textBlock.setWidth(473)         // Set width of the text block
        textBlock.setTextPadding(10)    // Set padding around the text
        textBlock.setBorderColor(Color.blue)
        var xy = textBlock.drawOn(page) // Draw the English text on the page and get coordinates

        // Draw a blue rectangle around the English text block
        let rect = Rect(xy[0], xy[1], 30, 30)
        rect.setBorderColor(Color.blue)
        rect.drawOn(page)

        // Read Greek text from a file and draw it on the page
        let greekText = try String(
                contentsOfFile: "data/languages/greek.txt", encoding: .utf8)
        let textBlock2 = TextBlock(font, greekText)
        textBlock2.setLocation(50, xy[1] + 30)  // Set location below the previous text
        textBlock2.setWidth(473)                // Set width for Greek text block
        textBlock2.setTextPadding(10)           // Set padding around the Greek text
        xy = textBlock2.drawOn(page)            // Draw Greek text and update coordinates

        // Read Bulgarian text from a file and draw it with a blue border and rounded corners
        let bulgarianText = try String(
                contentsOfFile: "data/languages/bulgarian.txt", encoding: .utf8)
        let textBlock3 = TextBlock(font, bulgarianText)
        textBlock3.setLocation(50, xy[1] + 30)  // Set location below Greek text
        textBlock3.setWidth(473)                // Set width for Bulgarian text block
        textBlock3.setTextPadding(10)           // Set padding around the Bulgarian text
        textBlock3.setBorderColor(Color.blue)   // Blue border for the Bulgarian text
        textBlock3.setBorderCornerRadius(10)    // Set rounded corners for the border
        textBlock3.drawOn(page)                 // Draw the Bulgarian text

        // Finalize the PDF creation
        pdf.complete()
    }
}

// Entry point for execution
let time0 = Int64(Date().timeIntervalSince1970 * 1000) // Record start time
_ = try Example_01()  // Create the PDF
let time1 = Int64(Date().timeIntervalSince1970 * 1000) // Record end time
TextUtils.printDuration("Example_01", time0, time1)    // Print the execution duration
