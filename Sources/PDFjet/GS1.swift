/**
 * GS1.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// Reads GS1 data as people write it, each Application Identifier in
/// parentheses and its data after it, for the barcodes that carry it: GS1
/// DataMatrix and GS1-128, and makes its GS1 Digital Link.
public enum GS1 {
    /// An Application Identifier and its data, and whether a separator follows
    /// it in a barcode: after a field of no set length that another follows.
    struct Field {
        let ai: String
        let data: String
        let separator: Bool
    }

    private static let format =
            "GS1 data is Application Identifiers in parentheses, each followed by its data, such as (01)09506000134352(17)261231!"

    // The lengths, of the Application Identifier and its data together, of the
    // fields that the first two digits of their Application Identifier give a
    // set length, as the GS1 General Specifications list them. Their data is
    // digits, and they need no separator after them.
    private static let predefinedLengths: [String: Int] = [
        "00": 20, "01": 16, "02": 16, "03": 16, "04": 18,
        "11": 8, "12": 8, "13": 8, "14": 8, "15": 8, "16": 8, "17": 8, "18": 8, "19": 8,
        "20": 4, "31": 10, "32": 10, "33": 10, "34": 10, "35": 10, "36": 10, "41": 16,
    ]

    // The characters of the data of a field, GS1's character set 82, less the
    // parentheses, which enclose the Application Identifiers.
    private static let characters = Set(
            "!\"%&'*+,-./0123456789:;<=>?ABCDEFGHIJKLMNOPQRSTUVWXYZ_abcdefghijklmnopqrstuvwxyz".unicodeScalars)

    /// Returns the fields of the GS1 data written as people read it. Throws if
    /// the data is not GS1; see DataMatrix(gs1:).
    static func parse(_ str: String) throws -> [Field] {
        var rest = Array(str.unicodeScalars)
        if rest.first != "(" {
            throw PDFjetError(message: format)
        }
        var fields = [Field]()
        while !rest.isEmpty {
            guard let end = rest.firstIndex(of: ")") else {
                throw PDFjetError(message: format)
            }
            let ai = String(String.UnicodeScalarView(rest[1..<end]))
            rest = Array(rest[(end + 1)...])
            let next = rest.firstIndex(of: "(") ?? rest.count
            let data = Array(rest[..<next])
            rest = Array(rest[next...])
            try checkField(ai, data)
            fields.append(Field(
                    ai: ai,
                    data: String(String.UnicodeScalarView(data)),
                    separator: predefinedLengths[String(ai.prefix(2))] == nil && !rest.isEmpty))
        }
        return fields
    }

    // Throws if the field is not one GS1 allows.
    private static func checkField(_ ai: String, _ data: [Unicode.Scalar]) throws {
        let aiScalars = Array(ai.unicodeScalars)
        if aiScalars.count < 2 || aiScalars.count > 4 || !digits(aiScalars) {
            throw PDFjetError(message: "The Application Identifier (\(ai)) is not two to four digits!")
        }
        if data.isEmpty {
            throw PDFjetError(message: "The Application Identifier (\(ai)) has no data!")
        }
        for c in data where !characters.contains(c) {
            throw PDFjetError(message: "The data of (\(ai)) has a character that GS1 does not allow!")
        }
        if data.count > 90 {
            throw PDFjetError(message: "The data of (\(ai)) is longer than 90 characters!")
        }
        if let length = predefinedLengths[String(ai.prefix(2))],
                aiScalars.count + data.count != length || !digits(data) {
            throw PDFjetError(message: "The data of (\(ai)) must be \(length - aiScalars.count) digits!")
        }
        let gln = aiScalars.count == 3 && ai.hasPrefix("41") && aiScalars[2].value <= 55    // '7'
        if (ai == "00" || ai == "01" || ai == "02" || gln) && !checkDigitIsRight(data) {
            throw PDFjetError(message: "The check digit of (\(ai)) is wrong!")
        }
    }

    private static func digits(_ s: [Unicode.Scalar]) -> Bool {
        return s.allSatisfy { $0.value >= 48 && $0.value <= 57 }
    }

    // Returns true if the last digit of the number is its check digit: the
    // digits before it weighted 3 and 1 in turn from the right, and the check
    // digit what takes their sum to a multiple of ten.
    private static func checkDigitIsRight(_ number: [Unicode.Scalar]) -> Bool {
        var sum = 0
        for i in stride(from: number.count - 2, through: 0, by: -1) {
            var digit = Int(number[i].value) - 48
            if (number.count - 2 - i) % 2 == 0 {
                digit *= 3
            }
            sum += digit
        }
        return Int(number[number.count - 1].value) - 48 == (10 - sum % 10) % 10
    }

    // The primary keys of GS1 Digital Link and their qualifiers, in the order
    // the path has them: each array is a place in the path, and the
    // qualifiers in it are alternatives.
    private static let digitalLinkKeys: [String: [[String]]] = [
        "00": [], "01": [["22"], ["10"], ["21"]], "253": [], "255": [], "401": [], "402": [],
        "414": [["254", "7040"]], "417": [["7040"]], "8003": [], "8004": [],
        "8006": [["22"], ["10"], ["21"]], "8010": [["8011"]], "8013": [],
        "8017": [["8019"]], "8018": [["8019"]],
    ]

    /// Returns the GS1 Digital Link of the GS1 data, the web address an
    /// ordinary QR code carries, at the domain: a brand's own, or GS1's
    /// resolver, https://id.gs1.org. digitalLink(domain: "https://id.gs1.org",
    /// data: "(01)09506000134352(10)ABC123(17)261231") is
    /// "https://id.gs1.org/01/09506000134352/10/ABC123?17=261231". The primary
    /// key, such as a GTIN (01), an SSCC (00) or a GLN (414), is first in the
    /// path, then its qualifiers in the order of the standard, such as (22),
    /// the batch (10) and the serial (21) of a GTIN, and the other fields are
    /// in the query, in their order. A value is percent-encoded where a web
    /// address needs it. Throws if the domain does not start with https:// or
    /// http://; if the data is not GS1, as for DataMatrix(gs1:); or if it has
    /// no primary key or two, or both (254) and (7040) of a GLN.
    public static func digitalLink(domain: String, data: String) throws -> String {
        var rest = domain
        if rest.hasPrefix("https://") {
            rest = String(rest.dropFirst(8))
        } else if rest.hasPrefix("http://") {
            rest = String(rest.dropFirst(7))
        }
        if rest == domain || rest.isEmpty || rest == "/" {
            throw PDFjetError(
                    message: "The domain of a GS1 Digital Link starts with https:// or http://, such as https://id.gs1.org!")
        }
        let fields = try parse(data)
        var key = -1
        for (i, field) in fields.enumerated() where digitalLinkKeys[field.ai] != nil {
            if key >= 0 {
                throw PDFjetError(message: "A GS1 Digital Link has one primary key, not (\(fields[key].ai)) and (\(field.ai))!")
            }
            key = i
        }
        if key < 0 {
            throw PDFjetError(
                    message: "A GS1 Digital Link needs a primary key, such as a GTIN (01), an SSCC (00) or a GLN (414)!")
        }

        var result = domain.hasSuffix("/") ? String(domain.dropLast()) : domain
        result += "/" + fields[key].ai + "/" + percentEncode(fields[key].data)
        var used = [Bool](repeating: false, count: fields.count)
        used[key] = true
        for place in digitalLinkKeys[fields[key].ai]! {
            var found = -1
            for (i, field) in fields.enumerated() where place.contains(field.ai) {
                if found >= 0 {
                    throw PDFjetError(message: "The qualifiers (\(fields[found].ai)) and (\(field.ai)) of " +
                            "(\(fields[key].ai)) cannot be together!")
                }
                found = i
            }
            if found >= 0 {
                result += "/" + fields[found].ai + "/" + percentEncode(fields[found].data)
                used[found] = true
            }
        }
        var separator = "?"
        for (i, field) in fields.enumerated() where !used[i] {
            result += separator + field.ai + "=" + percentEncode(field.data)
            separator = "&"
        }
        return result
    }

    // Returns the value with each character but the unreserved ones of a web
    // address, the letters, the digits and "-._~", as % and its two
    // hexadecimal digits.
    private static func percentEncode(_ value: String) -> String {
        let hex = Array("0123456789ABCDEF")
        var result = ""
        for c in value.utf8 {
            if c >= 65 && c <= 90 || c >= 97 && c <= 122 || c >= 48 && c <= 57 || "-._~".utf8.contains(c) {
                result.append(Character(Unicode.Scalar(c)))
            } else {
                result += "%" + String(hex[Int(c >> 4)]) + String(hex[Int(c & 15)])
            }
        }
        return result
    }
}
