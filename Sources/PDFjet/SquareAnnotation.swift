/**
 * SquareAnnotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A square annotation.
public class SquareAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Square
    }
}
