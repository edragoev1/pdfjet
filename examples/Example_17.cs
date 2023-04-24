using System;
using System.IO;
using System.Diagnostics;
using PDFjet.NET;

/**
 *  Example_17.cs
 */
public class Example_17 {
    public Example_17() {
        PDF pdf = new PDF(new BufferedStream(
                new FileStream("Example_17.pdf", FileMode.Create)));

        String fileName = "PngSuite/BASN3P08.PNG";
        FileStream fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image1 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image2 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image3 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN3P01.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image4 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S01N3P01.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image5 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S02N3P01.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image6 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S03N3P01.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image7 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S04N3P01.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image8 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S05N3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image9 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S06N3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image10 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S07N3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image11 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S08N3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image12 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S09N3P02.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image13 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/S32N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image14 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S33N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image15 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S34N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image16 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S35N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image17 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S36N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image18 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S37N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image19 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S38N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image20 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S39N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image21 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/S40N3P04.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image22 = new Image(pdf, fis, ImageType.PNG);


        fileName = "images/qrcode.png";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image23 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/F00N2C08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image24 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F01N2C08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image25 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F02N2C08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image26 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F03N2C08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image27 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F04N2C08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image28 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/Z00N2C08.PNG";
        // color, no interlacing, compression level 0 (none)
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image29 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/Z03N2C08.PNG";
        // color, no interlacing, compression level 3
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image30 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/Z06N2C08.PNG";
        // color, no interlacing, compression level 6 (default)
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image31 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/Z09N2C08.PNG";
        // color, no interlacing, compression level 9 (maximum)
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image32 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/F00N0G08.PNG";
        // 8 bit greyscale, no interlacing, filter-type 0
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image33 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F01N0G08.PNG";
        // 8 bit greyscale, no interlacing, filter-type 1
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image34 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F02N0G08.PNG";
        // 8 bit greyscale, no interlacing, filter-type 2
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image35 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F03N0G08.PNG";
        // 8 bit greyscale, no interlacing, filter-type 3
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image36 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/F04N0G08.PNG";
        // 8 bit greyscale, no interlacing, filter-type 4
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image37 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/BASN0G08.PNG";
        // 8 bit grayscale
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image38 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN0G04.PNG";
        // 4 bit grayscale
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image39 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN0G02.PNG";
        // 2 bit grayscale
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image40 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/BASN0G01.PNG";
        // Black and White image
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image41 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/BGAN6A08.PNG";
        // Image with alpha transparency
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image42 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/OI1N2C16.PNG";
        // Color image with 1 IDAT chunk
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image43 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI4N2C16.PNG";
        // Color image with 2 IDAT chunks
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image44 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI4N2C16.PNG";
        // Color image with 4 IDAT chunks
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image45 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI9N2C16.PNG";
        // IDAT chunks with length == 1
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image46 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/OI1N0G16.PNG";
        // Grayscale image with 1 IDAT chunk
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image47 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI4N0G16.PNG";
        // Grayscale image with 2 IDAT chunks
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image48 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI4N0G16.PNG";
        // Grayscale image with 4 IDAT chunks
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image49 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/OI9N0G16.PNG";
        // IDAT chunks with length == 1
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image50 = new Image(pdf, fis, ImageType.PNG);


        fileName = "PngSuite/TBBN3P08.PNG";
        // Transparent, black background chunk
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image51 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/TBGN3P08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image52 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/TBWN3P08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image53 = new Image(pdf, fis, ImageType.PNG);

        fileName = "PngSuite/TBYN3P08.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image54 = new Image(pdf, fis, ImageType.PNG);

        fileName = "images/LGK_ADDRESS.PNG";
        fis = new FileStream(fileName, FileMode.Open, FileAccess.Read);
        Image image55 = new Image(pdf, fis, ImageType.PNG);


        Page page = new Page(pdf, A4.PORTRAIT);

        image1.SetLocation(100f, 80f);
        image1.DrawOn(page);

        image2.SetLocation(100f, 120f);
        image2.DrawOn(page);

        image3.SetLocation(100f, 160f);
        image3.DrawOn(page);

        image4.SetLocation(100f, 200f);
        image4.DrawOn(page);


        image5.SetLocation(200f, 80f);
        image5.DrawOn(page);

        image6.SetLocation(200f, 120f);
        image6.DrawOn(page);

        image7.SetLocation(200f, 160f);
        image7.DrawOn(page);

        image8.SetLocation(200f, 200f);
        image8.DrawOn(page);

        image9.SetLocation(200f, 240f);
        image9.DrawOn(page);

        image10.SetLocation(200f, 280f);
        image10.DrawOn(page);

        image11.SetLocation(200f, 320f);
        image11.DrawOn(page);

        image12.SetLocation(200f, 360f);
        image12.DrawOn(page);

        image13.SetLocation(200f, 400f);
        image13.DrawOn(page);


        image14.SetLocation(300f, 80f);
        image14.DrawOn(page);

        image15.SetLocation(300f, 120f);
        image15.DrawOn(page);

        image16.SetLocation(300f, 160f);
        image16.DrawOn(page);

        image17.SetLocation(300f, 200f);
        image17.DrawOn(page);

        image18.SetLocation(300f, 240f);
        image18.DrawOn(page);

        image19.SetLocation(300f, 280f);
        image19.DrawOn(page);

        image20.SetLocation(300f, 320f);
        image20.DrawOn(page);

        image21.SetLocation(300f, 360f);
        image21.DrawOn(page);

        image22.SetLocation(300f, 400f);
        image22.DrawOn(page);


        image23.SetLocation(350f, 50f);
        image23.DrawOn(page);


        image24.SetLocation(100f, 650f);
        image24.DrawOn(page);

        image25.SetLocation(140f, 650f);
        image25.DrawOn(page);

        image26.SetLocation(180f, 650f);
        image26.DrawOn(page);

        image27.SetLocation(220f, 650f);
        image27.DrawOn(page);

        image28.SetLocation(260f, 650f);
        image28.DrawOn(page);


        image29.SetLocation(300f, 650f);
        image29.DrawOn(page);

        image30.SetLocation(340f, 650f);
        image30.DrawOn(page);

        image31.SetLocation(380f, 650f);
        image31.DrawOn(page);

        image32.SetLocation(420f, 650f);
        image32.DrawOn(page);


        image33.SetLocation(100f, 700f);
        image33.DrawOn(page);

        image34.SetLocation(140f, 700f);
        image34.DrawOn(page);

        image35.SetLocation(180f, 700f);
        image35.DrawOn(page);

        image36.SetLocation(220f, 700f);
        image36.DrawOn(page);

        image37.SetLocation(260f, 700f);
        image37.DrawOn(page);


        image38.SetLocation(300f, 700f);
        image38.DrawOn(page);

        image39.SetLocation(340f, 700f);
        image39.DrawOn(page);

        image40.SetLocation(380f, 700f);
        image40.DrawOn(page);

        image41.SetLocation(420f, 700f);
        image41.DrawOn(page);


        image42.SetLocation(100f, 750f);
        image42.DrawOn(page);


        image43.SetLocation(140f, 750f);
        image43.DrawOn(page);

        image44.SetLocation(180f, 750f);
        image44.DrawOn(page);

        image45.SetLocation(220f, 750f);
        image45.DrawOn(page);

        image46.SetLocation(260f, 750f);
        image46.DrawOn(page);


        image47.SetLocation(300f, 750f);
        image47.DrawOn(page);

        image48.SetLocation(340f, 750f);
        image48.DrawOn(page);

        image49.SetLocation(380f, 750f);
        image49.DrawOn(page);

        image50.SetLocation(420f, 750f);
        image50.DrawOn(page);


        image51.SetLocation(300f, 800f);
        image51.DrawOn(page);

        image52.SetLocation(340f, 800f);
        image52.DrawOn(page);

        image53.SetLocation(380f, 800f);
        image53.DrawOn(page);

        image54.SetLocation(420f, 800f);
        image54.DrawOn(page);


        image55.SetLocation(100f, 500f);
        image55.DrawOn(page);

        pdf.Complete();
    }

    public static void Main(String[] args) {
        Stopwatch sw = Stopwatch.StartNew();
        long time0 = sw.ElapsedMilliseconds;
        new Example_17();
        long time1 = sw.ElapsedMilliseconds;
        sw.Stop();
        TextUtils.PrintDuration("Example_17", time0, time1);
    }
}   // End of Example_17.cs
