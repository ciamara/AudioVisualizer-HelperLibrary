using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using YoutubeExplode;
using YoutubeExplode.Videos.Streams;
using YoutubeExplode.Converter;

namespace AudioVisualiser
{
    public class Downloader
    {
        public static async Task Download(string url, IProgress<double> progress = null)
        {
            if (!string.IsNullOrEmpty(url))
            {
                // url sanitization

                using var youtube = new YoutubeClient();

                var video = await youtube.Videos.GetAsync(url);

                var title = video.Title;

                foreach (var invalidChar in Path.GetInvalidFileNameChars())
                {
                    title = title.Replace(invalidChar.ToString(), string.Empty);
                }

                //title = title.Replace(" ", "_");

                string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);

                string path = Path.Combine(folderPath, $"{title}.mp3");

                await youtube.Videos.DownloadAsync(
                    url,
                    path,
                    builder => builder.SetContainer("mp3"),
                    progress
                );
            }
        }
    }
}
