/**
 * Permissions.swift
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */

///
/// Encapsulates the user access permissions for a PDF document as specified in
/// ISO 32000-2, Table 22. Provides a type-safe interface to manipulate and
/// query the permissions flags.
///
public class Permissions: CustomStringConvertible {
    private var permissionsFlags = 0

    /// A mask that defines the valid bits (3-12) that can be set in the
    /// permissions flag. Bits outside this range are reserved and must be zero.
    private static let VALID_BITS_MASK = 0b1_1111_1111_1000  // Hex: 0xFFF8

    ///
    /// Initializes a new instance of the Permissions class with no permissions
    /// granted.
    ///
    public init() {
    }

    ///
    /// Initializes a new instance of the Permissions class from the raw 32-bit
    /// integer value found in the PDF encryption dictionary's /P key. Invalid
    /// bits (outside positions 3-12) are masked out to ensure compliance.
    ///
    /// - Parameter rawFlags: the raw integer value.
    ///
    public init(_ rawFlags: Int) {
        permissionsFlags = rawFlags & Permissions.VALID_BITS_MASK
    }

    ///
    /// Gets the permissions as the combination of the UserAccess values.
    ///
    /// - Returns: the permissions flags.
    ///
    public func getAccess() -> Int {
        return permissionsFlags
    }

    ///
    /// Sets the permissions using the UserAccess values. The value is
    /// automatically masked to ensure any invalid bits are cleared.
    ///
    /// - Parameter access: the UserAccess values combined with bitwise OR.
    /// - Returns: this Permissions object.
    ///
    @discardableResult
    public func setAccess(_ access: Int) -> Permissions {
        permissionsFlags = access & Permissions.VALID_BITS_MASK
        return self
    }

    ///
    /// Gets the raw 32-bit integer value of the permissions flags. This value
    /// is suitable for writing to the /P key in a PDF encryption dictionary.
    /// All reserved bits are guaranteed to be zero.
    ///
    /// - Returns: the value of the /P key.
    ///
    public func getRawValue() -> Int {
        return permissionsFlags
    }

    ///
    /// Gets a value indicating whether the user can print the document
    /// (possibly at low quality, unless canPrintHighQuality() is true).
    ///
    /// - Returns: true if the user can print the document.
    ///
    public func canPrint() -> Bool {
        return UserAccess.PRINT.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can modify the document's
    /// contents.
    ///
    /// - Returns: true if the user can modify the contents.
    ///
    public func canModifyContents() -> Bool {
        return UserAccess.MODIFY_CONTENTS.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can copy or extract content.
    ///
    /// - Returns: true if the user can copy the contents.
    ///
    public func canCopyContents() -> Bool {
        return UserAccess.COPY_CONTENTS.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can add or modify annotations
    /// and form fields. This is primarily for legacy PDF support.
    ///
    /// - Returns: true if the user can modify annotations.
    ///
    public func canModifyAnnotations() -> Bool {
        return UserAccess.MODIFY_ANNOTATIONS.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can fill interactive form
    /// fields.
    ///
    /// - Returns: true if the user can fill form fields.
    ///
    public func canFillFormFields() -> Bool {
        return UserAccess.FILL_FORM_FIELDS.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can extract content for
    /// accessibility.
    ///
    /// - Returns: true if the user can extract content for accessibility.
    ///
    public func canExtractForAccessibility() -> Bool {
        return UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can assemble the document
    /// (manipulate pages).
    ///
    /// - Returns: true if the user can assemble the document.
    ///
    public func canAssembleDocument() -> Bool {
        return UserAccess.ASSEMBLE_DOCUMENT.isSetIn(permissionsFlags)
    }

    ///
    /// Gets a value indicating whether the user can print the document at
    /// high quality.
    ///
    /// - Returns: true if the user can print at high quality.
    ///
    public func canPrintHighQuality() -> Bool {
        return UserAccess.PRINT_HIGH_QUALITY.isSetIn(permissionsFlags)
    }

    ///
    /// Sets or clears the specified permissions.
    ///
    /// - Parameter permissions: the permissions to modify (from the UserAccess
    ///   values).
    /// - Parameter grant: true to grant the permissions; false to revoke them.
    /// - Returns: this Permissions object.
    ///
    @discardableResult
    public func setPermissions(_ permissions: Int, _ grant: Bool) -> Permissions {
        if grant {
            permissionsFlags |= permissions
        } else {
            permissionsFlags &= ~permissions
        }
        // Re-apply mask to ensure no invalid bits were set
        permissionsFlags &= Permissions.VALID_BITS_MASK
        return self
    }

    ///
    /// A string that represents the current permissions for debugging purposes.
    ///
    public var description: String {
        return "Permissions: 0x" + String(permissionsFlags, radix: 16, uppercase: true) +
                " (Raw Value: " + String(permissionsFlags) + ")"
    }
}   // End of Permissions.swift
