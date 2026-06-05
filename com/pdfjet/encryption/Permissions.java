/**
 * Permissions.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

/**
 * Encapsulates the user access permissions for a PDF document as specified in
 * ISO 32000-2, Table 22. Provides a type-safe interface to manipulate and query
 * the permissions flags.
 */
public class Permissions {
    private int permissionsFlags;

    /**
     * A mask that defines the valid bits (3-12) that can be set in the permissions flag.
     * Bits outside this range are reserved and must be zero.
     */
    private static final int VALID_BITS_MASK = 0b1_1111_1111_1000; // Hex: 0xFFF8

    /**
     * Initializes a new instance of the Permissions class
     * with no permissions granted.
     */
    public Permissions() {
        permissionsFlags = 0;
    }

    /**
     * Initializes a new instance of the Permissions class
     * from the raw 32-bit integer value found in the PDF encryption dictionary's /P key.
     * Invalid bits (outside positions 3-12) are masked out to ensure compliance.
     *
     * @param rawFlags The raw integer value.
     */
    public Permissions(int rawFlags) {
        permissionsFlags = rawFlags & VALID_BITS_MASK;
    }

    /**
     * Gets the permissions as the type-safe UserAccess enum combination.
     * Note: Java enums don't have built-in flags support like C#, so we return the raw value.
     */
    public int getAccess() {
        return permissionsFlags;
    }

    /**
     * Sets the permissions using the type-safe UserAccess enum values.
     * The value is automatically masked to ensure any invalid bits are cleared.
     */
    public void setAccess(int access) {
        permissionsFlags = access & VALID_BITS_MASK;
    }

    /**
     * Gets the raw 32-bit integer value of the permissions flags.
     * This value is suitable for writing to the /P key in a PDF encryption dictionary.
     * All reserved bits are guaranteed to be zero.
     */
    public int getRawValue() {
        return permissionsFlags;
    }

    /**
     * Gets a value indicating whether the user can print the document
     * (possibly at low quality, unless canPrintHighQuality() is true).
     */
    public boolean canPrint() {
        return UserAccess.PRINT.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can modify the document's contents.
     */
    public boolean canModifyContents() {
        return UserAccess.MODIFY_CONTENTS.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can copy or extract content.
     */
    public boolean canCopyContents() {
        return UserAccess.COPY_CONTENTS.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can add or modify annotations and form fields.
     * This is primarily for legacy PDF support.
     */
    public boolean canModifyAnnotations() {
        return UserAccess.MODIFY_ANNOTATIONS.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can fill interactive form fields.
     */
    public boolean canFillFormFields() {
        return UserAccess.FILL_FORM_FIELDS.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can extract content for accessibility.
     */
    public boolean canExtractForAccessibility() {
        return UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can assemble the document (manipulate pages).
     */
    public boolean canAssembleDocument() {
        return UserAccess.ASSEMBLE_DOCUMENT.isSetIn(permissionsFlags);
    }

    /**
     * Gets a value indicating whether the user can print the document at high quality.
     */
    public boolean canPrintHighQuality() {
        return UserAccess.PRINT_HIGH_QUALITY.isSetIn(permissionsFlags);
    }

    /**
     * Sets or clears the specified permissions.
     *
     * @param permissions The permissions to modify (from the UserAccess enum values).
     * @param grant True to grant the permissions; false to revoke them.
     */
    public void setPermissions(int permissions, boolean grant) {
        if (grant) {
            permissionsFlags |= permissions;
        } else {
            permissionsFlags &= ~permissions;
        }
        // Re-apply mask to ensure no invalid bits were set
        permissionsFlags &= VALID_BITS_MASK;
    }

    /**
     * Returns a string that represents the current permissions for debugging purposes.
     *
     * @return A string representation of the current permissions.
     */
    @Override
    public String toString() {
        return String.format("Permissions: 0x%X (Raw Value: %d)", permissionsFlags, permissionsFlags);
    }
}