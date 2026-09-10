/**
 * Drawable.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
import Foundation

/**
 * Interface that is required for components that can be drawn on a PDF page as part of Optional Content Group.
 *
 * @author Mark Paxton, Eugene Dragoev
 */
public protocol Drawable {

    /**
     *  Draw the component implementing this interface on the PDF page.
     *
     *  - Parameter page: the page to draw on.
     *  - Returns: x and y coordinates of the bottom right corner of this component.
     */
    @discardableResult
    func drawOn(_ page: Page?) -> [Float]

    /**
     *  Set the x and y coordinates of the drawable object.
     *
     *  - Parameter x: the x location
     *  - Parameter y: the y location
     *  - Returns: this drawable object.
     */
    @discardableResult
    func setLocation(_ x: Float, _ y: Float) -> Self

}
