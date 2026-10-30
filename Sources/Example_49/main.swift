/**
 * main.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation
import PDFjet

/**
 * Example_49.swift
 *
 * Merges and splits existing PDF documents. merge draws a cover page and
 * merges three documents after it, into Example_49.pdf: the pages of each
 * follow in their order and keep their content, resources, annotations and
 * links, and what belongs to a whole document, such as its bookmarks, form
 * fields and tagging, is left out. split writes one PDF for each page of a
 * document, Example_49_1.pdf to Example_49_5.pdf, and all of its pages in
 * reverse order to Example_49_reversed.pdf; a link to a page that is not in
 * the same PDF leads nowhere.
 */
public class Example_49 {
    public init() throws {
        try merge()
        try split()
    }

    private func merge() throws {
        let pdf = PDF(OutputStream(toFileAtPath: "Example_49.pdf", append: false)!)

        let fileNames = [
            "resources/data/testPDFs/scanned-linearized.pdf",
            "resources/data/testPDFs/sample-document.pdf",
            "resources/data/testPDFs/PDFjetLogo.pdf"
        ]
        var documents = [[PDFobj]]()
        for fileName in fileNames {
            documents.append(try pdf.read(from: InputStream(fileAtPath: fileName)!))
        }

        let f1 = try Font(pdf, IBMPlexSans.Bold)
        f1.setSize(24.0)
        let f2 = try Font(pdf, IBMPlexSans.Regular)
        f2.setSize(12.0)

        let page = Page(pdf, Letter.PORTRAIT)
        TextLine(f1, "Merged documents").setLocation(50.0, 80.0).drawOn(page)
        var y: Float = 130.0
        for i in 0..<fileNames.count {
            let pages = pdf.getPageObjects(from: documents[i]).count
            let text = fileNames[i] + ", " + String(pages) + (pages == 1 ? " page" : " pages")
            TextLine(f2, text).setLocation(50.0, y).drawOn(page)
            y += 20.0
        }

        for objects in documents {
            try pdf.merge(objects)
        }

        try pdf.complete()
    }

    private func split() throws {
        let objects = try PDF().read(from: InputStream(fileAtPath: "resources/data/testPDFs/scanned-linearized.pdf")!)
        let count = PDF().getPageObjects(from: objects).count

        for i in 1...count {
            let part = PDF(OutputStream(toFileAtPath: "Example_49_\(i).pdf", append: false)!)
            try part.merge(objects, [i])
            try part.complete()
        }

        let pdf = PDF(OutputStream(toFileAtPath: "Example_49_reversed.pdf", append: false)!)
        try pdf.merge(objects, Array((1...count).reversed()))
        try pdf.complete()
    }
}   // End of Example_49.swift

let time0 = Int64(Date().timeIntervalSince1970 * 1000)
_ = try Example_49()
let time1 = Int64(Date().timeIntervalSince1970 * 1000)
print("Example_49 => \(String(format: "%4lld", time1 - time0)) ms")
