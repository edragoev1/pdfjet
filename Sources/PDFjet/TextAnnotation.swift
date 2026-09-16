/**
 * TextAnnotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A text note annotation.
public class TextAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Text
    }
}
