/**
 * GS1.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// Reads GS1 data as people write it, for DataMatrix(gs1:).
enum GS1 {
    // GS, which ends a field of no set length when another follows.
    private static let separator = "\u{1d}"

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

    /// Returns the element string of the GS1 data written as people read it:
    /// the Application Identifiers without their parentheses, each followed by
    /// its data, and GS after a field of no set length that another follows.
    static func elementString(_ str: String) throws -> String {
        var rest = Array(str.unicodeScalars)
        if rest.first != "(" {
            throw PDFjetError(message: format)
        }
        var result = ""
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

            result += ai + String(String.UnicodeScalarView(data))
            if predefinedLengths[String(ai.prefix(2))] == nil && !rest.isEmpty {
                result += separator
            }
        }
        return result
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
}
