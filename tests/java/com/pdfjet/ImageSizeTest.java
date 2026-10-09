/*
 * ImageSizeTest.java
 *
 * Copyright (c) 2026 PDFjet Software
 * Licensed under the MIT License. See LICENSE file in the project root.
 */
package com.pdfjet;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;
import static org.junit.jupiter.api.Assertions.fail;

import java.io.ByteArrayInputStream;
import java.io.ByteArrayOutputStream;
import java.io.File;
import java.lang.reflect.Field;
import java.nio.file.Files;
import java.util.ArrayList;
import java.util.List;
import java.util.zip.CRC32;
import org.junit.jupiter.api.Test;

class ImageSizeTest {
    // The size read from the header against Image: an image Image takes has
    // the size it draws it at, and an image whose header is refused Image
    // refuses too. Returns whether Image took it.
    static boolean agrees(String name, byte[] data, List<String> problems) throws Exception {
        ImageSize size = null;
        Exception sizeError = null;
        try {
            size = ImageSize.read(new ByteArrayInputStream(data));
        } catch (Exception e) {
            sizeError = e;
        }
        Image image;
        try {
            image = new Image(TestSupport.newPDF(), new ByteArrayInputStream(data));
        } catch (Exception | Error e) {
            return false;
        }
        if (sizeError != null) {
            problems.add(name + ": Image takes it, ImageSize refuses it: " + sizeError.getMessage());
            return true;
        }
        int pixelWidth = pixels(image, "pixelWidth");
        int pixelHeight = pixels(image, "pixelHeight");
        if (size.getWidth() != image.getWidth() || size.getHeight() != image.getHeight()
                || size.getPixelWidth() != pixelWidth || size.getPixelHeight() != pixelHeight) {
            problems.add(name + ": " + size.getWidth() + " by " + size.getHeight() + ", "
                    + size.getPixelWidth() + " by " + size.getPixelHeight() + " pixels; Image "
                    + image.getWidth() + " by " + image.getHeight() + ", " + pixelWidth + " by " + pixelHeight);
        }
        return true;
    }

    static int pixels(Image image, String field) throws Exception {
        Field f = Image.class.getDeclaredField(field);
        f.setAccessible(true);
        return f.getInt(image);
    }

    static void collect(File dir, List<File> files) {
        File[] list = dir.listFiles();
        if (list == null) {
            return;
        }
        for (File f : list) {
            if (f.isDirectory()) {
                collect(f, files);
            } else {
                String name = f.getName().toLowerCase();
                if (name.endsWith(".png") || name.endsWith(".jpg") || name.endsWith(".jpeg") || name.endsWith(".bmp")) {
                    files.add(f);
                }
            }
        }
    }

    @Test
    void theSizeIsTheOneImageDrawsAt() throws Exception {
        List<File> files = new ArrayList<>();
        for (String dir : new String[] {"images", "tests/data", ".images"}) {
            collect(TestSupport.file(dir), files);
        }
        List<String> problems = new ArrayList<>();
        int taken = 0;
        for (File f : files) {
            if (agrees(f.getPath(), Files.readAllBytes(f.toPath()), problems)) {
                taken++;
            }
        }
        if (!problems.isEmpty()) {
            fail(String.join("\n", problems.subList(0, Math.min(20, problems.size()))));
        }
        assertTrue(taken >= 20, taken + " images taken, of " + files.size());
    }

    @Test
    void aHugePNGIsRefusedFromItsHeader() throws Exception {
        ByteArrayOutputStream png = new ByteArrayOutputStream();
        png.write(new byte[] {(byte) 0x89, 'P', 'N', 'G', 0x0D, 0x0A, 0x1A, 0x0A});
        chunk(png, "IHDR", new byte[] {0, 1, (byte) 0x86, (byte) 0xA0, 0, 1, (byte) 0x86, (byte) 0xA0, 8, 6, 0, 0, 0});
        chunk(png, "IEND", new byte[0]);
        try {
            ImageSize.read(new ByteArrayInputStream(png.toByteArray()));
            fail("a PNG of 100,000 by 100,000 taken");
        } catch (Exception e) {
            assertTrue(e.getMessage().contains("larger than"), e.getMessage());
        }
    }

    @Test
    void aJPEGTurnedIsItsHeightByItsWidth() throws Exception {
        for (String orientation : new String[] {"1", "3", "6", "8"}) {
            byte[] data = Files.readAllBytes(TestSupport.file("tests/data/jpeg/orientation-" + orientation + ".jpg").toPath());
            ImageSize size = ImageSize.read(new ByteArrayInputStream(data));
            assertEquals(32f, size.getWidth(), "orientation " + orientation);
            assertEquals(16f, size.getHeight(), "orientation " + orientation);
        }
    }

    static void chunk(ByteArrayOutputStream out, String type, byte[] data) throws Exception {
        int n = data.length;
        out.write(new byte[] {(byte) (n >> 24), (byte) (n >> 16), (byte) (n >> 8), (byte) n});
        byte[] t = type.getBytes("ISO-8859-1");
        out.write(t);
        out.write(data);
        CRC32 crc = new CRC32();
        crc.update(t);
        crc.update(data);
        long c = crc.getValue();
        out.write(new byte[] {(byte) (c >> 24), (byte) (c >> 16), (byte) (c >> 8), (byte) c});
    }
}
