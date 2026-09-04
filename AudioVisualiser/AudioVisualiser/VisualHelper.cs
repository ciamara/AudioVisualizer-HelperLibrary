using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using WinRT;

namespace AudioHelpers
{
    public class VisualHelper
    {
        public static async Task<SoftwareBitmap?> GetBitmapFromIPicture(TagLib.IPicture cover)
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
            //if (cover == null) return "#963719";

            return await Task.Run(() =>
            {
                uint pixelCount = (uint)(cover.PixelWidth * cover.PixelHeight);
                var buffer = new Windows.Storage.Streams.Buffer(pixelCount * 4);

                cover.CopyToBuffer(buffer);

                byte[] pixels = buffer.ToArray();

                var colorCounts = new Dictionary<Vector3, int>();
                //colorCounts.Add(new Vector3(150, 55, 25), 1); // default color #963719
                colorCounts.Add(new Vector3(138, 138, 138), 1);   // default greyscale #8a8a8a
                int color_tolerance = 40; // color uniquness tolerance, values range 0-440 preferably choose lower numbers
                int vivid_tolerance = 40; // tolerance for avoiding the grayscale, higher the more vivid the picked colors will be


                for (int i = 0; i < pixels.Length; i += 32)
                {
                    byte r_byte = pixels[i];
                    byte g_byte = pixels[i + 1];
                    byte b_byte = pixels[i + 2];

                    // looking for the most vivid colors
                    int max = Math.Max(r_byte, Math.Max(g_byte, b_byte));
                    int min = Math.Min(r_byte, Math.Min(g_byte, b_byte));
                    int vividness = max - min;

                    if (vividness < vivid_tolerance)
                    {
                        continue; // if not vivid enough, skip
                    }

                    Vector3 rgb = new Vector3(r_byte, g_byte, b_byte);
                    bool new_color = true;
                    foreach (var color in colorCounts.Keys)
                    {
                        // check if it is close enough to any color in the dictionary
                        // if not add it
                        if (Vector3.Distance(rgb, color) <= color_tolerance)
                        {
                            colorCounts[color]++;
                            new_color = false;
                            break;
                        }
                    }
                    if (new_color)
                    {
                        colorCounts.Add(rgb, 1);
                    }
                }

                Vector3 dominantColor = colorCounts.OrderByDescending(kvp => kvp.Value).First().Key;

                int r = Math.Clamp((int)dominantColor.X, 0, 255);
                int g = Math.Clamp((int)dominantColor.Y, 0, 255);
                int b = Math.Clamp((int)dominantColor.Z, 0, 255);

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

        public static bool IsColorLight(Windows.UI.Color color)
        {
            double brightness = (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B);

            return brightness > 128;
        }

        public static Color LightenColor(Color color, double amount)
        {
            byte r = (byte)(color.R + ((255 - color.R) * amount));
            byte g = (byte)(color.G + ((255 - color.G) * amount));
            byte b = (byte)(color.B + ((255 - color.B) * amount));

            return Windows.UI.Color.FromArgb(color.A, r, g, b);
        }

        public static Color DarkenColor(Color color, double amount)
        {
            byte r = (byte)(color.R - (color.R * amount));
            byte g = (byte)(color.G - (color.G * amount));
            byte b = (byte)(color.B - (color.B * amount));

            return Windows.UI.Color.FromArgb(color.A, r, g, b);
        }
    }
}
