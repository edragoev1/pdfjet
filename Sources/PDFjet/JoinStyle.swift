/**
 * JoinStyle.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Used to specify the join style when joining two lines.
/// See the Page and Line classes for more details.
///
public enum JoinStyle : Int {
    case MITER = 0
    case ROUND
    case BEVEL
}
