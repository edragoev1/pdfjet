/*
 * Permissions.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

import java.util.EnumSet;
import java.util.Set;

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
    private static final int VALID_BITS_MASK = 0b1111_1111_1100; // Hex: 0xFFC

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
     * Returns a copy of the granted permissions. They are the /P key without its
     * reserved bits; Encryption sets the reserved bits that ISO 32000-2 requires
     * to be one when it writes the /P key.
     *
     * @return the granted UserAccess values.
     */
    public Set<UserAccess> getAccess() {
        EnumSet<UserAccess> access = EnumSet.noneOf(UserAccess.class);
        for (UserAccess value : UserAccess.values()) {
            if (value != UserAccess.NONE && isGranted(value)) {
                access.add(value);
            }
        }
        return access;
    }

    /**
     * Sets the granted permissions. The permissions that are not in the set are revoked.
     *
     * @param access the UserAccess values to grant.
     * @return this Permissions object.
     */
    public Permissions setAccess(Set<UserAccess> access) {
        permissionsFlags = 0;
        for (UserAccess value : access) {
            permissionsFlags |= value.getValue();
        }
        permissionsFlags &= VALID_BITS_MASK;
        return this;
    }

    /**
     * Gets a value indicating whether the user can print the document
     * (possibly at low quality, unless canPrintHighQuality() is true).
     *
     * @return true if the user can print the document.
     */
    public boolean canPrint() {
        return isGranted(UserAccess.PRINT);
    }

    /**
     * Gets a value indicating whether the user can modify the document's contents.
     *
     * @return true if the user can modify the contents.
     */
    public boolean canModifyContents() {
        return isGranted(UserAccess.MODIFY_CONTENTS);
    }

    /**
     * Gets a value indicating whether the user can copy or extract content.
     *
     * @return true if the user can copy the contents.
     */
    public boolean canCopyContents() {
        return isGranted(UserAccess.COPY_CONTENTS);
    }

    /**
     * Gets a value indicating whether the user can add or modify annotations and form fields.
     * This is primarily for legacy PDF support.
     *
     * @return true if the user can modify annotations.
     */
    public boolean canModifyAnnotations() {
        return isGranted(UserAccess.MODIFY_ANNOTATIONS);
    }

    /**
     * Gets a value indicating whether the user can fill interactive form fields.
     *
     * @return true if the user can fill form fields.
     */
    public boolean canFillFormFields() {
        return isGranted(UserAccess.FILL_FORM_FIELDS);
    }

    /**
     * Gets a value indicating whether the user can extract content for accessibility.
     *
     * @return true if the user can extract content for accessibility.
     */
    public boolean canExtractForAccessibility() {
        return isGranted(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY);
    }

    /**
     * Gets a value indicating whether the user can assemble the document (manipulate pages).
     *
     * @return true if the user can assemble the document.
     */
    public boolean canAssembleDocument() {
        return isGranted(UserAccess.ASSEMBLE_DOCUMENT);
    }

    /**
     * Gets a value indicating whether the user can print the document at high quality.
     *
     * @return true if the user can print at high quality.
     */
    public boolean canPrintHighQuality() {
        return isGranted(UserAccess.PRINT_HIGH_QUALITY);
    }

    /**
     * Grants the specified permissions. The other permissions stay as they are.
     *
     * @param permissions the UserAccess values to grant.
     * @return this Permissions object.
     */
    public Permissions grant(UserAccess... permissions) {
        for (UserAccess value : permissions) {
            permissionsFlags |= value.getValue();
        }
        // Re-apply mask to ensure no invalid bits were set
        permissionsFlags &= VALID_BITS_MASK;
        return this;
    }

    /**
     * Revokes the specified permissions. The other permissions stay as they are.
     *
     * @param permissions the UserAccess values to revoke.
     * @return this Permissions object.
     */
    public Permissions revoke(UserAccess... permissions) {
        for (UserAccess value : permissions) {
            permissionsFlags &= ~value.getValue();
        }
        permissionsFlags &= VALID_BITS_MASK;
        return this;
    }

    private boolean isGranted(UserAccess access) {
        return (permissionsFlags & access.getValue()) == access.getValue();
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
