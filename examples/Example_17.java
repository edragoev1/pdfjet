/*
 * Example_17.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package examples;

import java.io.*;
import com.pdfjet.*;

/**
 * Example_17.java
 */
public class Example_17 {
    public Example_17() throws Exception {
        PDF pdf = new PDF(
                new BufferedOutputStream(
                        new FileOutputStream("Example_17.pdf")));
        pdf.setCompliance(Compliance.PDF_UA_1);
        pdf.setTitle("PNG Images from PngSuite");

        String fileName = "PngSuite/BASN3P08.PNG";
        FileInputStream fis = new FileInputStream(fileName);
        Image image1 = new Image(pdf, fis);
        image1.setAltDescription(
                "Vertical bands of red, orange, yellow, green, cyan, blue and magenta, each shading from black at the top to white at the bottom, from a palette of 256 colors.");

        fileName = "PngSuite/BASN3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image2 = new Image(pdf, fis);
        image2.setAltDescription(
                "A rainbow that runs from the bottom left to the top right, from a palette of 16 colors.");

        fileName = "PngSuite/BASN3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image3 = new Image(pdf, fis);
        image3.setAltDescription(
                "A diagonal check of red, green, blue and yellow, from a palette of four colors.");

        fileName = "PngSuite/BASN3P01.PNG";
        fis = new FileInputStream(fileName);
        Image image4 = new Image(pdf, fis);
        image4.setAltDescription(
                "A check of blue and yellow squares, from a palette of two colors.");


        fileName = "PngSuite/S01N3P01.PNG";
        fis = new FileInputStream(fileName);
        Image image5 = new Image(pdf, fis);
        image5.setAltDescription(
                "The size test image of 1 by 1 pixels, from a palette of two colors.");


        fileName = "PngSuite/S02N3P01.PNG";
        fis = new FileInputStream(fileName);
        Image image6 = new Image(pdf, fis);
        image6.setAltDescription(
                "The size test image of 2 by 2 pixels, from a palette of two colors.");

        fileName = "PngSuite/S03N3P01.PNG";
        fis = new FileInputStream(fileName);
        Image image7 = new Image(pdf, fis);
        image7.setAltDescription(
                "The size test image of 3 by 3 pixels, from a palette of two colors.");

        fileName = "PngSuite/S04N3P01.PNG";
        fis = new FileInputStream(fileName);
        Image image8 = new Image(pdf, fis);
        image8.setAltDescription(
                "The size test image of 4 by 4 pixels, from a palette of two colors.");

        fileName = "PngSuite/S05N3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image9 = new Image(pdf, fis);
        image9.setAltDescription(
                "The size test image of 5 by 5 pixels: frames of color around its center, from a palette of four colors.");

        fileName = "PngSuite/S06N3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image10 = new Image(pdf, fis);
        image10.setAltDescription(
                "The size test image of 6 by 6 pixels: frames of color around its center, from a palette of four colors.");

        fileName = "PngSuite/S07N3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image11 = new Image(pdf, fis);
        image11.setAltDescription(
                "The size test image of 7 by 7 pixels: frames of color around its center, from a palette of four colors.");

        fileName = "PngSuite/S08N3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image12 = new Image(pdf, fis);
        image12.setAltDescription(
                "The size test image of 8 by 8 pixels: frames of color around its center, from a palette of four colors.");

        fileName = "PngSuite/S09N3P02.PNG";
        fis = new FileInputStream(fileName);
        Image image13 = new Image(pdf, fis);
        image13.setAltDescription(
                "The size test image of 9 by 9 pixels: frames of color around its center, from a palette of four colors.");



        fileName = "PngSuite/S32N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image14 = new Image(pdf, fis);
        image14.setAltDescription(
                "The size test image of 32 by 32 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S33N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image15 = new Image(pdf, fis);
        image15.setAltDescription(
                "The size test image of 33 by 33 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S34N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image16 = new Image(pdf, fis);
        image16.setAltDescription(
                "The size test image of 34 by 34 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S35N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image17 = new Image(pdf, fis);
        image17.setAltDescription(
                "The size test image of 35 by 35 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S36N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image18 = new Image(pdf, fis);
        image18.setAltDescription(
                "The size test image of 36 by 36 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S37N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image19 = new Image(pdf, fis);
        image19.setAltDescription(
                "The size test image of 37 by 37 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S38N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image20 = new Image(pdf, fis);
        image20.setAltDescription(
                "The size test image of 38 by 38 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S39N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image21 = new Image(pdf, fis);
        image21.setAltDescription(
                "The size test image of 39 by 39 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");

        fileName = "PngSuite/S40N3P04.PNG";
        fis = new FileInputStream(fileName);
        Image image22 = new Image(pdf, fis);
        image22.setAltDescription(
                "The size test image of 40 by 40 pixels: vertical rainbow stripes with its size written over them, from a palette of 16 colors.");


        fileName = "images/qrcode.png";
        fis = new FileInputStream(fileName);
        Image image23 = new Image(pdf, fis);
        image23.setAltDescription(
                "A QR code for https://pdfjet.com.");


        fileName = "PngSuite/F00N2C08.PNG";
        fis = new FileInputStream(fileName);
        Image image24 = new Image(pdf, fis);
        image24.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 0, none.");

        fileName = "PngSuite/F01N2C08.PNG";
        fis = new FileInputStream(fileName);
        Image image25 = new Image(pdf, fis);
        image25.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 1, sub.");

        fileName = "PngSuite/F02N2C08.PNG";
        fis = new FileInputStream(fileName);
        Image image26 = new Image(pdf, fis);
        image26.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 2, up.");

        fileName = "PngSuite/F03N2C08.PNG";
        fis = new FileInputStream(fileName);
        Image image27 = new Image(pdf, fis);
        image27.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 3, average.");

        fileName = "PngSuite/F04N2C08.PNG";
        fis = new FileInputStream(fileName);
        Image image28 = new Image(pdf, fis);
        image28.setAltDescription(
                "A zero with a stroke through it, over a gradient of red, yellow, green and blue, written with the PNG filter type 4, Paeth.");


        fileName = "PngSuite/Z00N2C08.PNG";
        fis = new FileInputStream(fileName); // color, no interlacing, compression level 0 (none)
        Image image29 = new Image(pdf, fis);
        image29.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 0, which does not compress.");

        fileName = "PngSuite/Z03N2C08.PNG";
        fis = new FileInputStream(fileName); // color, no interlacing, compression level 3
        Image image30 = new Image(pdf, fis);
        image30.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 3.");

        fileName = "PngSuite/Z06N2C08.PNG";
        fis = new FileInputStream(fileName); // color, no interlacing, compression level 6 (default)
        Image image31 = new Image(pdf, fis);
        image31.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 6, the default.");

        fileName = "PngSuite/Z09N2C08.PNG";
        fis = new FileInputStream(fileName); // color, no interlacing, compression level 9 (maximum)
        Image image32 = new Image(pdf, fis);
        image32.setAltDescription(
                "A gradient of red, yellow, green and blue, deflated at compression level 9, the most.");


        fileName = "PngSuite/F00N0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit greyscale, no interlacing, filter-type 0
        Image image33 = new Image(pdf, fis);
        image33.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 0, none.");

        fileName = "PngSuite/F01N0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit greyscale, no interlacing, filter-type 1
        Image image34 = new Image(pdf, fis);
        image34.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 1, sub.");

        fileName = "PngSuite/F02N0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit greyscale, no interlacing, filter-type 2
        Image image35 = new Image(pdf, fis);
        image35.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 2, up.");

        fileName = "PngSuite/F03N0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit greyscale, no interlacing, filter-type 3
        Image image36 = new Image(pdf, fis);
        image36.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 3, average.");

        fileName = "PngSuite/F04N0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit greyscale, no interlacing, filter-type 4
        Image image37 = new Image(pdf, fis);
        image37.setAltDescription(
                "A zero with a stroke through it, over a gradient from black to white, written with the PNG filter type 4, Paeth.");


        fileName = "PngSuite/BASN0G08.PNG";
        fis = new FileInputStream(fileName); // 8 bit grayscale
        Image image38 = new Image(pdf, fis);
        image38.setAltDescription(
                "Horizontal bands of gray, from black to white, 8 bits a pixel.");

        fileName = "PngSuite/BASN0G04.PNG";
        fis = new FileInputStream(fileName); // 4 bit grayscale
        Image image39 = new Image(pdf, fis);
        image39.setAltDescription(
                "A gradient of 16 grays from black at the top left to white at the bottom right, 4 bits a pixel.");

        fileName = "PngSuite/BASN0G02.PNG";
        fis = new FileInputStream(fileName); // 2 bit grayscale
        Image image40 = new Image(pdf, fis);
        image40.setAltDescription(
                "A diagonal check in four grays, 2 bits a pixel.");

        fileName = "PngSuite/BASN0G01.PNG";
        fis = new FileInputStream(fileName); // Black and White image
        Image image41 = new Image(pdf, fis);
        image41.setAltDescription(
                "The letters W and B on the two halves of a diagonal, in black and white, 1 bit a pixel.");


        fileName = "PngSuite/BGAN6A08.PNG";
        fis = new FileInputStream(fileName); // Image with alpha transparency
        Image image42 = new Image(pdf, fis);
        image42.setAltDescription(
                "A rainbow that shades from red at the top to blue at the bottom, in 8 bit color with an alpha channel.");


        fileName = "PngSuite/OI1N2C16.PNG";
        fis = new FileInputStream(fileName); // Color image with 1 IDAT chunk
        Image image43 = new Image(pdf, fis);
        image43.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in one IDAT chunk.");

        fileName = "PngSuite/OI2N2C16.PNG";
        fis = new FileInputStream(fileName); // Color image with 2 IDAT chunks
        Image image44 = new Image(pdf, fis);
        image44.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in two IDAT chunks.");

        fileName = "PngSuite/OI4N2C16.PNG";
        fis = new FileInputStream(fileName); // Color image with 4 IDAT chunks
        Image image45 = new Image(pdf, fis);
        image45.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in four IDAT chunks.");

        fileName = "PngSuite/OI9N2C16.PNG";
        fis = new FileInputStream(fileName); // IDAT chunks with length == 1
        Image image46 = new Image(pdf, fis);
        image46.setAltDescription(
                "A gradient of red, yellow, green and blue in 16 bit color, in IDAT chunks of one byte.");


        fileName = "PngSuite/OI1N0G16.PNG";
        fis = new FileInputStream(fileName); // Grayscale image with 1 IDAT chunk
        Image image47 = new Image(pdf, fis);
        image47.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in one IDAT chunk.");

        fileName = "PngSuite/OI2N0G16.PNG";
        fis = new FileInputStream(fileName); // Grayscale image with 2 IDAT chunks
        Image image48 = new Image(pdf, fis);
        image48.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in two IDAT chunks.");

        fileName = "PngSuite/OI4N0G16.PNG";
        fis = new FileInputStream(fileName); // Grayscale image with 4 IDAT chunks
        Image image49 = new Image(pdf, fis);
        image49.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in four IDAT chunks.");

        fileName = "PngSuite/OI9N0G16.PNG";
        fis = new FileInputStream(fileName); // IDAT chunks with length == 1
        Image image50 = new Image(pdf, fis);
        image50.setAltDescription(
                "A gradient from black to white in 16 bit grayscale, in IDAT chunks of one byte.");


        fileName = "PngSuite/TBBN3P08.PNG";  // Transparent, black background
        fis = new FileInputStream(fileName); // chunk
        Image image51 = new Image(pdf, fis);
        image51.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a black background color in the file.");

        fileName = "PngSuite/TBGN3P08.PNG";
        fis = new FileInputStream(fileName);
        Image image52 = new Image(pdf, fis);
        image52.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a gray background color in the file.");

        fileName = "PngSuite/TBWN3P08.PNG";
        fis = new FileInputStream(fileName);
        Image image53 = new Image(pdf, fis);
        image53.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a white background color in the file.");

        fileName = "PngSuite/TBYN3P08.PNG";
        fis = new FileInputStream(fileName);
        Image image54 = new Image(pdf, fis);
        image54.setAltDescription(
                "A black cube with the word NeXT on it in colored letters, with transparent pixels around it and a yellow background color in the file.");

        fileName = "images/rgba-8bit-chunks.png";
        fis = new FileInputStream(fileName);
        Image image55 = new Image(pdf, fis);
        image55.setAltDescription(
                "Three half transparent circles in red, green and blue that overlap, beside the heading 8-bit RGBA PNG, 380 by 100, and the note that the image has anti-aliased text and half transparent circles on a transparent background, not interlaced, with iCCP, bKGD, pHYs, tIME and two IDAT chunks.");


        Page page = new Page(pdf, A4.PORTRAIT);

        image1.setLocation(100f, 80f);
        image1.drawOn(page);

        image2.setLocation(100f, 120f);
        image2.drawOn(page);

        image3.setLocation(100f, 160f);
        image3.drawOn(page);

        image4.setLocation(100f, 200f);
        image4.drawOn(page);


        image5.setLocation(200f, 80f);
        image5.drawOn(page);

        image6.setLocation(200f, 120f);
        image6.drawOn(page);

        image7.setLocation(200f, 160f);
        image7.drawOn(page);

        image8.setLocation(200f, 200f);
        image8.drawOn(page);

        image9.setLocation(200f, 240f);
        image9.drawOn(page);

        image10.setLocation(200f, 280f);
        image10.drawOn(page);

        image11.setLocation(200f, 320);
        image11.drawOn(page);

        image12.setLocation(200f, 360f);
        image12.drawOn(page);

        image13.setLocation(200f, 400f);
        image13.drawOn(page);


        image14.setLocation(300f, 80f);
        image14.drawOn(page);

        image15.setLocation(300f, 120f);
        image15.drawOn(page);

        image16.setLocation(300f, 160f);
        image16.drawOn(page);

        image17.setLocation(300f, 200f);
        image17.drawOn(page);

        image18.setLocation(300f, 240f);
        image18.drawOn(page);

        image19.setLocation(300f, 280f);
        image19.drawOn(page);

        image20.setLocation(300f, 320);
        image20.drawOn(page);

        image21.setLocation(300f, 360f);
        image21.drawOn(page);

        image22.setLocation(300f, 400f);
        image22.drawOn(page);


        image23.setLocation(350f, 50f);
        image23.drawOn(page);


        image24.setLocation(100f, 650);
        image24.drawOn(page);

        image25.setLocation(140f, 650);
        image25.drawOn(page);

        image26.setLocation(180f, 650);
        image26.drawOn(page);

        image27.setLocation(220f, 650);
        image27.drawOn(page);

        image28.setLocation(260f, 650);
        image28.drawOn(page);


        image29.setLocation(300f, 650);
        image29.drawOn(page);

        image30.setLocation(340f, 650);
        image30.drawOn(page);

        image31.setLocation(380f, 650);
        image31.drawOn(page);

        image32.setLocation(420f, 650);
        image32.drawOn(page);


        image33.setLocation(100f, 700f);
        image33.drawOn(page);

        image34.setLocation(140f, 700f);
        image34.drawOn(page);

        image35.setLocation(180f, 700f);
        image35.drawOn(page);

        image36.setLocation(220f, 700f);
        image36.drawOn(page);

        image37.setLocation(260f, 700f);
        image37.drawOn(page);


        image38.setLocation(300f, 700f);
        image38.drawOn(page);

        image39.setLocation(340f, 700f);
        image39.drawOn(page);

        image40.setLocation(380f, 700f);
        image40.drawOn(page);

        image41.setLocation(420f, 700f);
        image41.drawOn(page);


        image42.setLocation(100f, 750f);
        image42.drawOn(page);


        image43.setLocation(140f, 750f);
        image43.drawOn(page);

        image44.setLocation(180f, 750f);
        image44.drawOn(page);

        image45.setLocation(220f, 750f);
        image45.drawOn(page);

        image46.setLocation(260f, 750f);
        image46.drawOn(page);


        image47.setLocation(300f, 750f);
        image47.drawOn(page);

        image48.setLocation(340f, 750f);
        image48.drawOn(page);

        image49.setLocation(380f, 750f);
        image49.drawOn(page);

        image50.setLocation(420f, 750f);
        image50.drawOn(page);


        image51.setLocation(300f, 800f);
        image51.drawOn(page);

        image52.setLocation(340f, 800f);
        image52.drawOn(page);

        image53.setLocation(380f, 800f);
        image53.drawOn(page);

        image54.setLocation(420f, 800f);
        image54.drawOn(page);

        image55.setLocation(100f, 500);
        image55.drawOn(page);

        pdf.complete();
    }

    public static void main(String[] args) throws Exception {
        long time0 = System.currentTimeMillis();
        new Example_17();
        long time1 = System.currentTimeMillis();
        System.out.printf("Example_17 => %4d ms%n", time1 - time0);
    }
}   // End of Example_17.java
