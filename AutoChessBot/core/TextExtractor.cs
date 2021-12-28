using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace AutoChessBot.core {
    public class TextExtractor {

        private ConcurrentQueue<TesseractEngine> engines;
        private SemaphoreSlim semaphore;

        private const int ENGINES = 4;

        public TextExtractor(string dataSet) {
            engines = new ConcurrentQueue<TesseractEngine>();
            for (int i = 0; i < ENGINES; ++i) {
                engines.Enqueue(new TesseractEngine("", dataSet, EngineMode.Default));
            }
            semaphore = new SemaphoreSlim(4);
        }

        public string GetTextFromBitmap(Bitmap img, Rect rect) {
            TesseractEngine engine = null;
            Page page = null;
            try {
                semaphore.Wait();
                while (!engines.TryDequeue(out engine)) ;

                try {
                    page = engine.Process(img);
                } catch (Exception e) {
                    Console.WriteLine($"Exception occured {e}");
                    return null;
                }

                page.RegionOfInterest = rect;
                return page.GetText();
            } finally {
                // ???
                if (page != null) {
                    page.Dispose();
                }
                if (engine != null) {
                    engines.Enqueue(engine);
                    semaphore.Release();
                }
            }
        }

        public Task<string> GetTextFromBitmapAsync(Bitmap img) {
            return Task.Run(() => GetTextFromBitmap(img, new Rect(0, 0, img.Width, img.Height)));
        }

        public Task<string> GetTextFromBitmapAsync(Bitmap img, Rect rect) {
            return Task.Run(() => GetTextFromBitmap(img, rect));
        }
    }
}
