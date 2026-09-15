/**
 * Shape.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

/// The shape of the marker drawn at a Point.
public enum Shape {
    /// No marker.
    case INVISIBLE
    /// A circle.
    case CIRCLE
    /// A diamond.
    case DIAMOND
    /// A box.
    case BOX
    /// A plus sign.
    case PLUS
    /// A horizontal dash.
    case HORIZONTAL_DASH
    /// A vertical dash.
    case VERTICAL_DASH
    /// A multiplication sign.
    case MULTIPLY
    /// A star.
    case STAR
    /// An X mark.
    case X_MARK
    /// An arrow pointing up.
    case UP_ARROW
    /// An arrow pointing down.
    case DOWN_ARROW
    /// An arrow pointing left.
    case LEFT_ARROW
    /// An arrow pointing right.
    case RIGHT_ARROW
}
