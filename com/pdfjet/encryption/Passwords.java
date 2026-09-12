/*
 * Passwords.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

/**
 * The user and owner passwords of an encrypted PDF. A password is used as
 * typed, in UTF-8, and at most 127 bytes of it are used. A password that is
 * not set is empty, so the PDF opens without a prompt when the user
 * password is not set.
 *
 * Please see Example_30.
 */
public class Passwords {
    private String userPassword = "";
    private String ownerPassword = "";

    /** The default constructor */
    public Passwords() {
    }

    /**
     * Sets the user password, which is required to open the document.
     *
     * @param userPassword the user password.
     * @return this Passwords object.
     */
    public Passwords setUserPassword(String userPassword) {
        this.userPassword = userPassword;
        return this;
    }

    /**
     * Sets the owner password, which opens the document with full access.
     *
     * @param ownerPassword the owner password.
     * @return this Passwords object.
     */
    public Passwords setOwnerPassword(String ownerPassword) {
        this.ownerPassword = ownerPassword;
        return this;
    }

    /**
     * Returns the user password.
     *
     * @return the user password.
     */
    public String getUserPassword() {
        return userPassword;
    }

    /**
     * Returns the owner password.
     *
     * @return the owner password.
     */
    public String getOwnerPassword() {
        return ownerPassword;
    }
}
