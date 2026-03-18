using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT;

namespace AudioVisualiser
{
    public class VisualHelper
    {
        public static async Task<SoftwareBitmap> GetBitmapFromIPicture(TagLib.IPicture cover)
        {
            if (cover == null || cover.Data.Data == null) return null;

            byte[] imageData = cover.Data.Data;

            using (InMemoryRandomAccessStream stream = new InMemoryRandomAccessStream())
            {
                await stream.WriteAsync(imageData.AsBuffer());
                stream.Seek(0);

                BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);

                return await decoder.GetSoftwareBitmapAsync(
                BitmapPixelFormat.Rgba8,
                BitmapAlphaMode.Premultiplied);
            }
        }

        public static async Task<string> ExtractFeatureColor(SoftwareBitmap cover)
        {
            if (cover == null) return "#963719";    // default

            return await Task.Run(() =>
            {
                uint pixelCount = (uint)(cover.PixelWidth * cover.PixelHeight);
                var buffer = new Windows.Storage.Streams.Buffer(pixelCount * 4);

                cover.CopyToBuffer(buffer);

                byte[] pixels = buffer.ToArray();

                long rSum = 0, gSum = 0, bSum = 0;
                int count = 0;

                for (int i = 0; i < pixels.Length; i += 32)
                {
                    rSum += pixels[i];     // r
                    gSum += pixels[i + 1]; // g
                    bSum += pixels[i + 2]; // b
                    count++;
                }

                if (count == 0) return "#963719";

                // averages
                int r = (int)(rSum / count);
                int g = (int)(gSum / count);
                int b = (int)(bSum / count);

                double brightness = (0.299 * r) + (0.587 * g) + (0.114 * b);
                double threshold = 50.0;

                //if (brightness < threshold)
                //{
                //    float factor = 0.3f;

                //    r = (int)(r + (255 - r) * factor);
                //    g = (int)(g + (255 - g) * factor);
                //    b = (int)(b + (255 - b) * factor);
                //}

                r = Math.Clamp(r, 0, 255);
                g = Math.Clamp(g, 0, 255);
                b = Math.Clamp(b, 0, 255);

                return $"#{r:X2}{g:X2}{b:X2}";
            });
        }

        /// <summary>
        /// hex into a Color
        /// </summary>
        public static Color GetColorFromHex(string hex)
        {
            hex = hex.Replace("#", "");
            byte r = (byte)Convert.ToUInt32(hex.Substring(0, 2), 16);
            byte g = (byte)Convert.ToUInt32(hex.Substring(2, 2), 16);
            byte b = (byte)Convert.ToUInt32(hex.Substring(4, 2), 16);

            return Color.FromArgb(255, r, g, b);
        }
    }
}
