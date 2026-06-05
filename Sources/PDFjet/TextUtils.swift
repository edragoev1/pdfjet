/**
 * TextUtils.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

///
/// Text processing utilities.
///
public class TextUtils {
    public static func printDuration(_ example: String, _ time0: Int64, _ time1: Int64) {
        var duration = String(time1 - time0)
        if duration.count == 1 {
            duration = "    " + duration
        } else if duration.count == 2 {
            duration = "   " + duration
        } else if duration.count == 3 {
            duration = "  " + duration
        } else if duration.count == 4 {
            duration = " " + duration
        }
        duration += ".0"
        print(example + " => " + duration)
    }
}   // End of TextUtils.swift
