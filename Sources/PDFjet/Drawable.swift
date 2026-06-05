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
     *  @param page the page to draw on.
     *  @return x and y coordinates of the bottom right corner of this component.
     *  @throws Exception
     */
    @discardableResult
    func drawOn(_ page: Page?) -> [Float]
    func setPosition(_ x: Float, _ y: Float)

}
