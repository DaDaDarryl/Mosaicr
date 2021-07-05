using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Collections.Generic;

namespace Mosaicr
{
    class Program
    {
        static void Main(string[] args)
        {
            // Intro
            Console.WriteLine("");
            Console.WriteLine("Mosaicr v0.1 July 2021");
            Console.WriteLine("----------------------");
            Console.WriteLine("");

            byte argsLength = (byte)args.Length;

            // Not all parameters are present or show help
            if ( argsLength < 6 || ( args.Length == 1 && ( args[0] == "-h" || args[0] == "--help" ) ) )
            {
                showInstructions();
                return;
            }

            string pathReference = "";
            string pathSource = "";
            string pathDestination = "";
            byte tilesHorizontal = 20;
            byte tilesVertical = 20;
            byte outputQuality = 75;

            argsLength -= 1;
            for ( byte i = 0; i < argsLength; i++ )
            {
                string key = args[i].ToLower();
                string value = args[i + 1];

                switch ( key )
                {
                    case "-i":
                    case "--input":
                        pathReference = value;
                        break;
                    case "-o":
                    case "--output":
                        pathDestination = value;
                        break;
                    case "-q":
                    case "--quality":
                        outputQuality = Convert.ToByte(value);
                        break;
                    case "-s":
                    case "--sourcefolder":
                        pathSource = value;
                        break;
                    case "-bh":
                    case "--blockshorizontal":
                        tilesHorizontal = Convert.ToByte(value);
                        break;
                    case "-bv":
                    case "--blocksvertical":
                        tilesVertical = Convert.ToByte(value);
                        break;
                }
            }

            processImages(pathReference, pathDestination, pathSource, tilesHorizontal, tilesVertical, outputQuality);
        }

        private static void processImages(string pathReference, string pathDestination, string pathSource, int tilesHorizontal, int tilesVertical, byte outputQuality)
        {
            Console.Write("1/4 Validating files...");

            // Check if source image and tile folder exist
            string errors = "";
            if ( !File.Exists(pathReference) )
            {
                errors += "Reference file not found: \"" + pathReference + "\"";
            }
            if ( !Directory.Exists(pathSource) )
            {
                if ( errors != "" )
                {
                    errors += "\n";
                }
                errors += "Source folder not found: \"" + pathSource + "\"";
            }

            if ( errors != "" )
            {
                showInstructions(errors);
                return;
            }

            Console.WriteLine("OK");

            Console.Write("2/4 Processing reference image...");
            Bitmap imageReference = new Bitmap(pathReference);
            int tileWidth = (int)Math.Ceiling((float)(imageReference.Width / tilesHorizontal));
            int tileHeight = (int)Math.Ceiling((float)(imageReference.Height / tilesVertical));

            // Resize input image for a bit of optimisation
            float resizeFactor = 1;
            byte maxTileSize = 100; // Maximum width or height in pixels

            if ( tileWidth > maxTileSize && tileHeight > maxTileSize)
            {
                if (tileWidth > tileHeight)
                {
                    resizeFactor = (float)maxTileSize / (float)tileWidth;
                }
                else
                {
                    resizeFactor = (float)maxTileSize / (float)tileHeight;
                }
            }

            int widthSourceResized = (int)(imageReference.Width * resizeFactor);
            int heightSourceResized = (int)(imageReference.Height * resizeFactor);

            Bitmap imageReferenceResized = resizeImage(widthSourceResized, heightSourceResized, imageReference);
            
            int tileWidthResized = (int)Math.Ceiling((float)(tileWidth * resizeFactor));
            int tileHeightResized = (int)Math.Ceiling((float)(tileHeight * resizeFactor));
            Tile[] tiles = new Tile[tilesHorizontal * tilesVertical];
            SourceImage[] sourceImages;
            RectangleF cloneRect;
            Bitmap cloneBitmap;

            int totalTiles = 0;
            int nw = tileWidthResized;
            int nh = tileHeightResized;
            int nx;
            int ny;
            for ( int x = 0; x < tilesHorizontal; x++ )
            {
                nx = x * tileWidthResized;
                if (nx + tileWidthResized > imageReferenceResized.Width)
                {
                    nw = imageReferenceResized.Width - nx;
                }
                else
                {
                    nw = tileWidthResized;
                }
                for ( var y = 0; y < tilesVertical; y++ )
                {
                    ny = y * tileHeightResized;
                    if (ny + tileHeightResized > imageReferenceResized.Height)
                    {
                        nh = imageReferenceResized.Height - ny;
                    }
                    else
                    {
                        nh = tileHeightResized;
                    }
                    
                    cloneRect = new RectangleF(nx, ny, nw, nh);
                    cloneBitmap = imageReferenceResized.Clone(cloneRect, imageReferenceResized.PixelFormat);
                    tiles[totalTiles++] = new Tile(cloneBitmap);
                }
            }

            Console.WriteLine("OK.");

            Console.WriteLine("3/4 Analysing source images.");
            sourceImages = processSourceImages(pathSource);
            Console.WriteLine("\rAdded " + sourceImages.Length + " source images.                    ");

            Console.Write("4/4 Processing mosaic...");
            Bitmap imageMosaic = new Bitmap(imageReference.Width, imageReference.Height);
            double delta;
            int mx = 0;
            int my = 0;
            int tileCountVertical = 0;
            for ( int i = 0; i < tiles.Length; i++ )
            {
                double min = 1000000;
                int matchIndex = -1;
                for ( int si = 0; si < sourceImages.Length; si++ )
                {
                    delta = calculateDeltaECie(tiles[i].lab, sourceImages[si].lab);
                    if ( delta < min )
                    {
                        matchIndex = si;
                        min = delta;
                    }
                }

                Bitmap preparedSource = resizeAndCrop(sourceImages[matchIndex].filename, tileWidth, tileHeight);
                Graphics g = Graphics.FromImage((System.Drawing.Image)imageMosaic);
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.DrawImage(preparedSource, mx, my, tileWidth, tileHeight);
                g.Dispose();

                if (++tileCountVertical == tilesVertical)
                {
                    tileCountVertical = 0;
                    my = 0;
                    mx += tileWidth;
                } else
                {
                    my += tileHeight;
                }
            }

            // Save mosaic image
            ImageCodecInfo imageCodecInfo;
            Encoder imageEncoder;
            EncoderParameter imageEncoderParameter;
            EncoderParameters imageEncoderParameters;
            imageCodecInfo = GetEncoderInfo("image/jpeg");
            imageEncoder = Encoder.Quality;
            imageEncoderParameters = new EncoderParameters(1);
            imageEncoderParameter = new EncoderParameter(imageEncoder, Convert.ToInt64(outputQuality));
            imageEncoderParameters.Param[0] = imageEncoderParameter;
            imageMosaic.Save(pathDestination, imageCodecInfo, imageEncoderParameters);

            Console.WriteLine("OK.");
            Console.WriteLine("Mosaic saved in " + pathDestination + ".");
        }

        private static ImageCodecInfo GetEncoderInfo(String mimeType)
        {
            int j;
            ImageCodecInfo[] encoders;
            encoders = ImageCodecInfo.GetImageEncoders();
            for (j = 0; j < encoders.Length; ++j)
            {
                if (encoders[j].MimeType == mimeType)
                    return encoders[j];
            }
            return null;
        }

        private static double calculateDeltaECie(double[] colour1, double[] colour2)
        {
            double delta = Math.Sqrt(
                Math.Pow(colour2[0] - colour1[0], 2) +
                Math.Pow(colour2[1] - colour1[1], 2) +
                Math.Pow(colour2[2] - colour1[2], 2));

            return delta;
        }

        private static SourceImage[] processSourceImages(string pathSource)
        {
            List<SourceImage> sourceImages = new List<SourceImage>();
            string filename;
            try
            {
                var images = Directory.EnumerateFiles(pathSource, "*.jpg", SearchOption.AllDirectories);

                foreach (string currentFile in images)
                {
                    filename = currentFile.Substring(pathSource.Length + 1);
                    SourceImage si = new SourceImage(pathSource + "/" + filename);
                    if (si.lab.Length > 0)
                    {
                        Console.Write("\rAdding {0}                    ", filename);
                        sourceImages.Add(si);
                    }
                }
            }
            catch (Exception)
            { }

            return sourceImages.ToArray();
        }

        private static Bitmap resizeAndCrop(string filename, int targetWidth, int targetHeight)
        {
            Bitmap imageIn = new Bitmap(filename);
            int newWidth;
            int newHeight;
            int offsetX = 0;
            int offsetY = 0;
            
            if ( imageIn.Width > imageIn.Height )
            {
                newHeight = targetHeight;
                newWidth = imageIn.Width * targetHeight / imageIn.Height;

                if (newWidth > targetWidth)
                {
                    offsetX = targetWidth - newWidth;
                    if (offsetX > 0)
                    {
                        offsetX *= -1;
                    }
                    offsetX = (int)(offsetX * 0.5);
                }
                else
                {
                    newWidth = targetWidth;
                    newHeight = imageIn.Height * targetWidth / imageIn.Width;

                    offsetY = targetHeight - newHeight;

                    if (offsetY > 0)
                    {
                        offsetY *= -1;
                    }
                    offsetY = (int)(offsetY * 0.5);
                }
            }
            else
            {
                newWidth = targetWidth;
                newHeight = imageIn.Height * targetWidth / imageIn.Width;

                if (newHeight > targetHeight)
                {

                    offsetY = targetHeight - newHeight;

                    if (offsetY > 0)
                    {
                        offsetY *= -1;
                    }
                    offsetY = (int)(offsetY * 0.5);
                }
                else
                {
                    newHeight = targetHeight;
                    newWidth = imageIn.Width * targetHeight / imageIn.Height;

                    offsetX = targetWidth - newWidth;
                    if (offsetX > 0)
                    {
                        offsetX *= -1;
                    }
                    offsetX = (int)(offsetX * 0.5);
                }
            }

            Bitmap imageOut = new Bitmap(targetWidth, targetHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)imageOut);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imageIn, offsetX, offsetY, newWidth, newHeight);
            g.Dispose();

            return imageOut;
        }

        private static Bitmap resizeImage(int newWidth, int newHeight, Bitmap imageToResize)
        {
            Bitmap imageResized = new Bitmap(newWidth, newHeight);
            Graphics g = Graphics.FromImage((System.Drawing.Image)imageResized);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.DrawImage(imageToResize, 0, 0, newWidth, newHeight);
            g.Dispose();

            return imageResized;
        }

        private static void showInstructions(string errors = "")
        {
            if ( errors != "" )
            {
                Console.WriteLine(errors);
            }
            Console.WriteLine("");
            Console.WriteLine("Mosaicr:");
            Console.WriteLine("\tCreates a mosaic image generated from a source folder of images.");
            Console.WriteLine("Usage:");
            Console.WriteLine("\tmosaicr [options]");
            Console.WriteLine("Options:");
            Console.WriteLine("Required:");
            Console.WriteLine("-i");
            Console.WriteLine("--input\t\t\tPath including filename of the image to mosaic");
            Console.WriteLine("-o");
            Console.WriteLine("--output\t\tPath including filename of the generated mosaic JPEG image");
            Console.WriteLine("-s");
            Console.WriteLine("--sourceFolder\t\tPath to a folder containing images to generate the mosaic. Can include subfolders");
            Console.WriteLine("");
            Console.WriteLine("Optional:");
            Console.WriteLine("-bh");
            Console.WriteLine("--blocksHorizontal\tNumber of mosaic blocks horizontally. Default=20");
            Console.WriteLine("-bv");
            Console.WriteLine("--blocksVertical\tNumber of mosaic blocks vertically. Default=20");
            Console.WriteLine("-q");
            Console.WriteLine("--quality\t\tQuality of the output image: 0-100. Default=75 (0=lowest quality, smaller file size, 100=highest quality, larger file size)");
            Console.WriteLine("");
        }

        public static double[] processColours(Bitmap image)
        {
            Rectangle rect = new Rectangle(0, 0, image.Width, image.Height);
            try
            {
                BitmapData _bd = image.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
                IntPtr ptr = _bd.Scan0;
                byte[] sourceBuffer = new byte[_bd.Stride * _bd.Height];
                int sourceBufferLength = sourceBuffer.Length;

                Marshal.Copy(_bd.Scan0, sourceBuffer, 0, sourceBuffer.Length);
                image.UnlockBits(_bd);

                int ar = 0;
                int ag = 0;
                int ab = 0;
                int total = 0;

                sourceBufferLength -= 3;
                for (int k = 0; k < sourceBufferLength; k += 3)
                {
                    ar += sourceBuffer[k + 2];
                    ag += sourceBuffer[k + 1];
                    ab += sourceBuffer[k];
                    total++;
                }

                ar = ar / total;
                ag = ag / total;
                ab = ab / total;

                return rgbToCieLab(ar, ag, ab);
            }
            catch (Exception)
            {
                return new double[] { };
            }
        }

        /** 
         * Convert RGB to CIE-L*ab referencing illuminant/observer=D65/2°
         */
        private static double[] rgbToCieLab(int r, int g, int b)
        {
            double[] rgb = new double[] { (double)r, (double)g, (double)b };

            // 1. Convert RGB to XYZ
            for (byte i = 0; i < 3; i++)
            {
                double nv = rgb[i] / 255;
                if (nv > 0.04045)
                {
                    nv = Math.Pow((nv + 0.055) / 1.055, 2.4);
                }
                else
                {
                    nv /= 12.92;
                }
                rgb[i] = nv * 100;
            }

            double[] xyz = new double[] {
                Math.Round((rgb[0] * 0.4124) + (rgb[1] * 0.3576) + (rgb[2] * 0.1805), 4),
                Math.Round((rgb[0] * 0.2126) + (rgb[1] * 0.7152) + (rgb[2] * 0.0722), 4),
                Math.Round((rgb[0] * 0.0193) + (rgb[1] * 0.1192) + (rgb[2] * 0.9505), 4)
            };

            xyz[0] /= 95.047;
            xyz[1] /= 100.0;
            xyz[2] /= 108.883;

            for (byte i = 0; i < 3; i++)
            {
                double nv = xyz[i];
                if (nv > 0.008856)
                {
                    nv = Math.Pow(nv, 0.3333333333333333);
                }
                else
                {
                    nv = (7.787 * nv) + (16 / 116);
                }
                xyz[i] = nv;
            }

            // 2. Convert XYZ to CIE-L*ab
            double[] lab = new double[]
            {
                Math.Round(( 116 * xyz[1] ) - 16, 4),
                Math.Round(500 * ( xyz[0] - xyz[1] ), 4),
                Math.Round(200 * ( xyz[1] - xyz[2] ), 4)

            };

            return lab;
        }
    }

    class Tile
    {
        public double[] lab;

        public Tile(Bitmap image)
        {
            lab = Program.processColours(image);
        }
    }

    class SourceImage
    {
        public string filename;
        public double[] lab = new double[] { };

        public SourceImage(string _filename)
        {
            filename = _filename;
            try
            {
                lab = Program.processColours(new Bitmap(_filename));
            }
            catch (Exception)
            { }
        }
    }
}
