/**
 * Permissions.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// Encapsulates the user access permissions for a PDF document as specified in
    /// ISO 32000-2, Table 22. Provides a type-safe interface to manipulate and query
    /// the permissions flags.
    /// </summary>
    public class Permissions {
        private uint _permissionsFlags;

        /// <summary>
        /// A mask that defines the valid bits (3-12) that can be set in the permissions flag.
        /// Bits outside this range are reserved and must be zero.
        /// </summary>
        private const uint ValidBitsMask = 0b1_1111_1111_1000; // Hex: 0xFFF8

        /// <summary>
        /// Initializes a new instance of the <see cref="Permissions"/> class
        /// with no permissions granted.
        /// </summary>
        public Permissions() {
            _permissionsFlags = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Permissions"/> class
        /// from the raw 32-bit integer value found in the PDF encryption dictionary's /P key.
        /// Invalid bits (outside positions 3-12) are masked out to ensure compliance.
        /// </summary>
        /// <param name="rawFlags">The raw integer value.</param>
        public Permissions(int rawFlags) : this((uint)rawFlags) {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Permissions"/> class
        /// from the raw 32-bit integer value found in the PDF encryption dictionary's /P key.
        /// Invalid bits (outside positions 3-12) are masked out to ensure compliance.
        /// </summary>
        /// <param name="rawFlags">The raw integer value.</param>
        public Permissions(uint rawFlags) {
            _permissionsFlags = rawFlags & ValidBitsMask;
        }

        /// <summary>
        /// Gets or sets the permissions using the type-safe <see cref="UserAccess"/> enum.
        /// The getter returns the enum representation of the current flags.
        /// The setter applies the enum value, automatically masking any invalid bits.
        /// </summary>
        public UserAccess Access {
            get => (UserAccess)_permissionsFlags;
            set => _permissionsFlags = (uint)value & ValidBitsMask;
        }

        /// <summary>
        /// Gets the raw 32-bit integer value of the permissions flags.
        /// This value is suitable for writing to the /P key in a PDF encryption dictionary.
        /// All reserved bits are guaranteed to be zero.
        /// </summary>
        public uint RawValue => _permissionsFlags;

        /// <summary>
        /// Gets a value indicating whether the user can print the document
        /// (possibly at low quality, unless <see cref="CanPrintHighQuality"/> is true).
        /// </summary>
        public bool CanPrint => Access.HasFlag(UserAccess.Print);

        /// <summary>
        /// Gets a value indicating whether the user can modify the document's contents.
        /// </summary>
        public bool CanModifyContents => Access.HasFlag(UserAccess.ModifyContents);

        /// <summary>
        /// Gets a value indicating whether the user can copy or extract content.
        /// </summary>
        public bool CanCopyContents => Access.HasFlag(UserAccess.CopyContents);

        /// <summary>
        /// Gets a value indicating whether the user can add or modify annotations and form fields.
        /// This is primarily for legacy PDF support.
        /// </summary>
        public bool CanModifyAnnotations => Access.HasFlag(UserAccess.ModifyAnnotations);

        /// <summary>
        /// Gets a value indicating whether the user can fill interactive form fields.
        /// </summary>
        public bool CanFillFormFields => Access.HasFlag(UserAccess.FillFormFields);

        /// <summary>
        /// Gets a value indicating whether the user can extract content for accessibility.
        /// </summary>
        public bool CanExtractForAccessibility => Access.HasFlag(UserAccess.ExtractContentsForAccessibility);

        /// <summary>
        /// Gets a value indicating whether the user can assemble the document (manipulate pages).
        /// </summary>
        public bool CanAssembleDocument => Access.HasFlag(UserAccess.AssembleDocument);

        /// <summary>
        /// Gets a value indicating whether the user can print the document at high quality.
        /// </summary>
        public bool CanPrintHighQuality => Access.HasFlag(UserAccess.PrintHighQuality);

        /// <summary>
        /// Sets or clears the specified permissions.
        /// </summary>
        /// <param name="permissions">The permissions to modify (from the <see cref="UserAccess"/> enum).</param>
        /// <param name="grant">True to grant the permissions; false to revoke them.</param>
        public void SetPermissions(UserAccess permissions, bool grant = true) {
            if (grant) {
                _permissionsFlags |= (uint)permissions;
            } else {
                _permissionsFlags &= ~(uint)permissions;
            }
            // Re-apply mask to ensure no invalid bits were set by the enum value itself
            _permissionsFlags &= ValidBitsMask;
        }

        /// <summary>
        /// Returns a string that represents the current permissions for debugging purposes.
        /// </summary>
        /// <returns>A string representation of the current permissions.</returns>
        public override string ToString() {
            return $"Permissions: {Access} (Raw Value: {RawValue})";
        }
    }
}
