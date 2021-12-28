using AutoChessBot.core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace AutoChessBot {

    class Program {

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);
        [DllImport("user32.dll")]
        public static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, int nFlags);

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        static IntPtr FindWindowByCaption(string caption) {
            return FindWindowByCaption(IntPtr.Zero, caption);
        }

        public static Bitmap PrintWindow(IntPtr hwnd) {
            RECT rc;
            GetWindowRect(hwnd, out rc);

            Bitmap bmp = new Bitmap(rc.Width, rc.Height, PixelFormat.Format32bppArgb);
            Graphics gfxBmp = Graphics.FromImage(bmp);
            IntPtr hdcBitmap = gfxBmp.GetHdc();

            PrintWindow(hwnd, hdcBitmap, 0);

            gfxBmp.ReleaseHdc(hdcBitmap);
            gfxBmp.Dispose();

            return bmp;
        }

        static Nullable<IntPtr> FetchLeagueHandle() {
            Process[] processes = Process.GetProcessesByName("League of Legends");
            if (processes.Length == 0) {
                return null;
            } else {
                return processes[0].MainWindowHandle;
            }
        }

        static readonly Tuple<Color, double> [] PALETTE =
        {
            Tuple.Create(Color.FromArgb(255, 255, 165, 0), 1.0),
            Tuple.Create(Color.FromArgb(255, 195, 125, 0), 1.0),
            Tuple.Create(Color.FromArgb(255, 115, 75, 0), 1.0),

            Tuple.Create(Color.FromArgb(255, 165, 110, 10), 1.0),
            Tuple.Create(Color.FromArgb(255, 208, 133, 48), 1.0),

            // star pixels.
            Tuple.Create(Color.FromArgb(255, 70, 50, 0), 3.0),
        };

        /**
         * returns [0.0-1.0]
         */
        static int ColorDiff(Color a, Color b) {
            int diffA = a.A - b.A;
            int diffR = a.R - b.R;
            int diffG = a.G - b.G;
            int diffB = a.B - b.B;
            int sum = Math.Abs(diffR) + Math.Abs(diffG) + Math.Abs(diffB);
            return sum;
        }

        private static Bitmap recolor(Bitmap bitmap) {
            Bitmap temp = bitmap.Clone(
                new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                PixelFormat.Format32bppArgb
            );
            for (int i = 0; i < bitmap.Height; ++i) {
                for (int j = 0; j < bitmap.Width; ++j) {
                    Color pixel = bitmap.GetPixel(j, i);
                    int diff = 100000;
                    foreach (var x in PALETTE) {
                        diff = Math.Min(diff, (int)(ColorDiff(pixel, x.Item1) * x.Item2));
                    }
                    double coeff = 0;
                    if (diff < 70) {
                        coeff = 1;
                    }
                    temp.SetPixel(j, i,
                        Color.FromArgb(255, (int)(255 * coeff), (int)(255 * coeff), (int)(255 * coeff))
                    );
                    //Console.Write(tmp.GetPixel(j, i) + " ");
                }
                Console.WriteLine();
            }
            return temp;
        }

        static void Main(string[] args) {
            //Run(args);
            Bitmap tmp = new Bitmap("small.bmp");
            Bitmap res = recolor(tmp);
            res.Save("small2_bw.bmp");

            var ocr = new TextExtractor("rus");
            string text = ocr.GetTextFromBitmapAsync(tmp).Result;
            Console.WriteLine(text);
        }

        static void Run(string[] args) {
            Thread.Sleep(1000);
            var ocr = new TextExtractor("rus");
            for (int id = 0; ; ++id, Thread.Sleep(1000)) {
                try {
                    Nullable<IntPtr> leagueHandle = FetchLeagueHandle();
                    if (!leagueHandle.HasValue) {
                        Console.WriteLine("League window not detected.");
                        continue;
                    }
                    Bitmap orig =
                        Direct3DCapture.CaptureWindow(leagueHandle.Value);
                    if (orig == null) {
                        continue;
                    }
                    Bitmap processed =
                        orig.Clone(new Rectangle(0, 0, orig.Width, orig.Height), PixelFormat.Format32bppArgb);
                    processed.Save($"processed{id}.bmp");
                    continue;
                    orig = null;

                    //using (FileStream compressedFileStream = File.Create("processed.bmp.gz")) {
                    //    using (GZipStream compressionStream = new GZipStream(compressedFileStream, CompressionLevel.Optimal)) {
                    //        processed.Save(compressionStream, System.Drawing.Imaging.ImageFormat.Bmp);
                    //    }
                    //}
                    //processed.Save($"processed{id}.bmp");
                    int cachedId = id;
                    ocr.GetTextFromBitmapAsync(
                        processed, 
                        new Rect(0, 0, processed.Width, processed.Height)
                    ).ContinueWith(async t => {
                        string text = await t;
                        File.AppendAllText($"text{cachedId}", text);
                    });
                } catch (Exception e) {

                }
            }
        }
    }
}
