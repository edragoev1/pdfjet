/**
 *  Annotation.swift
 *
Copyright 2020 Innovatics Inc.

Redistribution and use in source and binary forms, with or without modification,
are permitted provided that the following conditions are met:

    * Redistributions of source code must retain the above copyright notice,
      this list of conditions and the following disclaimer.

    * Redistributions in binary form must reproduce the above copyright notice,
      this list of conditions and the following disclaimer in the documentation
      and / or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
"AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/


///
/// Used to create PDF annotation objects.
///
class Annotation {

    var objNumber = 0
    var uri: String?
    var key: String?
    var x1: Float = 0.0
    var y1: Float = 0.0
    var x2: Float = 0.0
    var y2: Float = 0.0

    var language: String?
    var altDescription: String?
    var actualText: String?

    var fileAttachment: FileAttachment?


    ///
    /// This class is used to create annotation objects.
    ///
    /// @param uri the URI string.
    /// @param key the destination name.
    /// @param x1 the x coordinate of the top left corner.
    /// @param y1 the y coordinate of the top left corner.
    /// @param x2 the x coordinate of the bottom right corner.
    /// @param y2 the y coordinate of the bottom right corner.
    ///
    init(
            _ uri: String?,
            _ key: String?,
            _ x1: Float,
            _ y1: Float,
            _ x2: Float,
            _ y2: Float,
            _ language: String?,
            _ altDescription: String?,
            _ actualText: String?) {
        self.uri = uri
        self.key = key
        self.x1 = x1
        self.y1 = y1
        self.x2 = x2
        self.y2 = y2
        self.language = language
        self.altDescription = (altDescription == nil) ? uri : altDescription
        self.actualText = (actualText == nil) ? uri : actualText
    }

}
