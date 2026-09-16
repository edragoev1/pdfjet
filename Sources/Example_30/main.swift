/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_30.swift
 */
public class Example_30 {
    public init() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_30.pdf", append: false)!)
        // pdf.setCompliance(Compliance.PDF_UA_1)

        let passwords = Passwords()
        passwords.setUserPassword("hello")
        passwords.setOwnerPassword("world")

        let permissions = Permissions()
        permissions.grant(
            UserAccess.PRINT |               // Set both to allow the user to print
            UserAccess.PRINT_HIGH_QUALITY |  // this document with high quality
            // UserAccess.MODIFY_CONTENTS |
            // UserAccess.COPY_CONTENTS |
            UserAccess.ASSEMBLE_DOCUMENT)

        pdf.setEncryption(Encryption(pdf, passwords, permissions))

        let f1 = try Font(pdf, IBMPlexSans.Regular)
        f1.setSize(36.0)

        let image = try Image(pdf, "images/ee-map.png")

        let file1 = try EmbeddedFile(pdf, "images/linux-logo.png", false)

        let page = Page(pdf, Letter.PORTRAIT)

        let textLine = TextLine(f1, "Hello, World!")
        textLine.setLocation(100.0, 100.0)
        textLine.drawOn(page)

        image.setLocation(100.0, 150.0)
        image.scaleBy(0.5)
        image.drawOn(page)

        // File attachment functionality
        let attachment = FileAttachment(file1)
        attachment.setLocation(100.0, 550.0)
        attachment.setIconPushPin()
        attachment.setIconSize(24.0)
        attachment.setTitle("Attached File: " + file1.getFileName())
        attachment.setContents(
                "Right mouse click on the icon to save the attached file.")
        attachment.drawOn(page)

        try pdf.complete()
    }
}   // End of Example_30.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_30()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_30 => \(String(format: "%4lld", time1 - time0)) ms")
