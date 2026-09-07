/*
 * Passwords.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet.encryption;

public class Passwords {
    private String userPassword;
    private String ownerPassword;

    public Passwords() {
    }

    public void setUserPassword(String userPassword) {
        this.userPassword = userPassword;
    }

    public void setOwnerPassword(String ownerPassword) {
        this.ownerPassword = ownerPassword;
    }

    public String getUserPassword() {
        return userPassword;
    }

    public String getOwnerPassword() {
        return ownerPassword;
    }
}