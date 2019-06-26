using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;

namespace SpriteGenerator
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                Directory.Delete("output", true);
            }
            catch { }

            Console.WriteLine("Generating thumbnails");
            Directory.CreateDirectory("output");
            // One thumbnail every 2 seconds
            var generateThumbnailArgs = "-ss 00:00:00 -i 5aa149aa2ab18726bc998c6a.mp4 -vf fps=1/2 output/preview-%5d.png";
            var ffmpeg = Process.Start("ffmpeg.exe", generateThumbnailArgs);
            ffmpeg.WaitForExit();

            Console.WriteLine("Generating scaled images");
            Directory.CreateDirectory("output/scaled");
            foreach (var file in Directory.EnumerateFiles("output", "*.png", SearchOption.TopDirectoryOnly))
            {
                var filename = file.Split('-')[1];
                var id = Path.GetFileNameWithoutExtension(filename);
                var scaleThumbnailArgs = $"-i {file} -vf scale=-1:240 output/scaled/output_x240_{id}.png -y";
                var ffmpegScaling = Process.Start("ffmpeg.exe", scaleThumbnailArgs);
                ffmpeg.WaitForExit();
            }

            // ffmpeg might still be writing the output files
            Thread.Sleep(2000);

            // One bitmap contains 12 images or 24 seconds of video
            // Images are laid out in 3 columns and 4 rows
            Console.WriteLine("Generating sprites");
            Directory.CreateDirectory("output/sprites");
            var batches = Directory.EnumerateFiles("output/scaled").InSetsOf(12);
            foreach (var batch in batches)
            {
                GenerateBitmap(batch);
            }

            Console.Read();
        }

        private static void GenerateBitmap(List<string> files)
        {
            var firstImage = Image.FromFile(files[0]);
            var lastTimestamp = GetTimestamp(files[files.Count - 1]);
            var spriteName = $"{lastTimestamp}.png";

            using (var currentBitmap = new Bitmap(firstImage.Width * 3, firstImage.Height * 4))
            using (var canvas = Graphics.FromImage(currentBitmap))
            {
                int currentX = 0, currentY = 0;
                int imagesInBitmap = 0;
                foreach (var file in files)
                {
                    var image = Image.FromFile(file);

                    canvas.DrawImage(image, currentX, currentY);

                    imagesInBitmap++;
                    if (imagesInBitmap % 3 == 0)
                    {
                        currentX = 0;
                        currentY += firstImage.Height;
                    }
                    else
                    {
                        currentX += image.Width;
                    }
                }

                Console.WriteLine($"Writing sprite: {spriteName}");
                currentBitmap.Save($"output/sprites/{spriteName}", ImageFormat.Png);
            }
        }

        private static int GetTimestamp(string file)
        {
            var nakedFilename = Path.GetFileNameWithoutExtension(file);
            var timestamp = int.Parse(nakedFilename.Substring(nakedFilename.LastIndexOf("_") + 1));
            // 1 frame every 2 seconds
            return timestamp * 2;
        }
    }

    public static class IEnumerableExtensions
    {
        public static IEnumerable<List<T>> InSetsOf<T>(this IEnumerable<T> source, int max)
        {
            var toReturn = new List<T>(max);
            foreach (var item in source)
            {
                toReturn.Add(item);
                if (toReturn.Count == max)
                {
                    yield return toReturn;
                    toReturn = new List<T>(max);
                }
            }
            if (toReturn.Any())
            {
                yield return toReturn;
            }
        }
    }
}
