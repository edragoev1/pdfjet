/**
 * Passwords.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// The user and owner passwords of an encrypted PDF. A password is used as
/// typed, in UTF-8, and at most 127 bytes of it are used. A password that is
/// not set is empty, so the PDF opens without a prompt when the user
/// password is not set.
///
/// Please see Example_30.
///
public class Passwords {
    private var userPassword = ""
    private var ownerPassword = ""

    /// The default constructor.
    public init() {
    }

    ///
    /// Sets the user password, which is required to open the document.
    ///
    /// - Parameter userPassword: the user password.
    /// - Returns: this Passwords object.
    ///
    @discardableResult
    public func setUserPassword(_ userPassword: String) -> Passwords {
        self.userPassword = userPassword
        return self
    }

    ///
    /// Sets the owner password, which opens the document with full access.
    ///
    /// - Parameter ownerPassword: the owner password.
    /// - Returns: this Passwords object.
    ///
    @discardableResult
    public func setOwnerPassword(_ ownerPassword: String) -> Passwords {
        self.ownerPassword = ownerPassword
        return self
    }

    ///
    /// Returns the user password.
    ///
    /// - Returns: the user password.
    ///
    public func getUserPassword() -> String {
        return userPassword
    }

    ///
    /// Returns the owner password.
    ///
    /// - Returns: the owner password.
    ///
    public func getOwnerPassword() -> String {
        return ownerPassword
    }
}   // End of Passwords.swift
