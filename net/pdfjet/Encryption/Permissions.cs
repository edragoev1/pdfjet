/*
 * Permissions.cs
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
using System;

namespace PDFjet.NET {
    /// <summary>
    /// The permissions of an encrypted PDF: the UserAccess flags a reader grants
    /// the user, written to the /P entry of the encryption dictionary.
    /// </summary>
    public class Permissions {
        private uint _permissionsFlags;

        // The permission bits of the standard, positions 3 to 12.
        private const uint ValidBitsMask = 0b1111_1111_1100; // Hex: 0xFFC

        /// <summary>Creates permissions that grant nothing.</summary>
        public Permissions() {
            _permissionsFlags = 0;
        }

        /// <summary>
        /// Creates permissions from the /P value of an encryption dictionary,
        /// keeping only its permission bits.
        /// </summary>
        /// <param name="rawFlags">the /P value.</param>
        public Permissions(int rawFlags) {
            _permissionsFlags = (uint) rawFlags & ValidBitsMask;
        }

        /// <summary>
        /// Returns the granted permissions as UserAccess flags: the /P value
        /// without its reserved bits.
        /// </summary>
        public UserAccess GetAccess() {
            return (UserAccess) _permissionsFlags;
        }

        /// <summary>Sets the granted permissions.</summary>
        /// <param name="access">the UserAccess values combined with bitwise OR.</param>
        public Permissions SetAccess(UserAccess access) {
            _permissionsFlags = (uint) access & ValidBitsMask;
            return this;
        }

        /// <summary>Returns true if printing is allowed.</summary>
        public bool CanPrint() {
            return GetAccess().HasFlag(UserAccess.PRINT);
        }

        /// <summary>Returns true if modifying the contents is allowed.</summary>
        public bool CanModifyContents() {
            return GetAccess().HasFlag(UserAccess.MODIFY_CONTENTS);
        }

        /// <summary>Returns true if copying the contents is allowed.</summary>
        public bool CanCopyContents() {
            return GetAccess().HasFlag(UserAccess.COPY_CONTENTS);
        }

        /// <summary>Returns true if modifying the annotations is allowed.</summary>
        public bool CanModifyAnnotations() {
            return GetAccess().HasFlag(UserAccess.MODIFY_ANNOTATIONS);
        }

        /// <summary>Returns true if filling form fields is allowed.</summary>
        public bool CanFillFormFields() {
            return GetAccess().HasFlag(UserAccess.FILL_FORM_FIELDS);
        }

        /// <summary>Returns true if extracting the contents for accessibility is allowed.</summary>
        public bool CanExtractForAccessibility() {
            return GetAccess().HasFlag(UserAccess.EXTRACT_CONTENTS_FOR_ACCESSIBILITY);
        }

        /// <summary>Returns true if assembling the document is allowed.</summary>
        public bool CanAssembleDocument() {
            return GetAccess().HasFlag(UserAccess.ASSEMBLE_DOCUMENT);
        }

        /// <summary>Returns true if printing in high quality is allowed.</summary>
        public bool CanPrintHighQuality() {
            return GetAccess().HasFlag(UserAccess.PRINT_HIGH_QUALITY);
        }

        /// <summary>Grants the permissions.</summary>
        /// <param name="permissions">the UserAccess values combined with bitwise OR.</param>
        public Permissions Grant(UserAccess permissions) {
            _permissionsFlags |= (uint) permissions;
            _permissionsFlags &= ValidBitsMask;
            return this;
        }

        /// <summary>Revokes the permissions.</summary>
        /// <param name="permissions">the UserAccess values combined with bitwise OR.</param>
        public Permissions Revoke(UserAccess permissions) {
            _permissionsFlags &= ~(uint) permissions;
            _permissionsFlags &= ValidBitsMask;
            return this;
        }

        /// <summary>Returns the permissions and their /P value.</summary>
        public override string ToString() {
            return $"Permissions: {GetAccess()} (Raw Value: {_permissionsFlags})";
        }
    }
}
