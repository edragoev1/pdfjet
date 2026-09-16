/**
 * CircleAnnotation.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/// A circle annotation.
public class CircleAnnotation: BaseAnnotation {
    override public init() {
        super.init()
        self.annotationType = Annotation.Circle
    }
}
