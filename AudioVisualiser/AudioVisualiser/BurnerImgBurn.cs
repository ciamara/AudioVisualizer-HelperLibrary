using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AudioHelpers
{
    public class BurnerImgBurn
    {
        public static event EventHandler<string> StatusUpdated = delegate { };
        public static event EventHandler<Exception> BurnError = delegate { };
        public static event EventHandler BurnCompleted = delegate { };

        /// <summary>
        /// Burns an MP3 directory to an Audio CD with CD-Text using ImgBurn.
        /// </summary>
        /// <param name="source">Directory containing MP3 files</param>
        /// <param name="albumTitle">The CD-Text Album Title</param>
        /// <param name="albumArtist">The CD-Text Album Artist</param>
        /// <param name="driveLetter">The target drive letter to burn to</param>
        /// <param name="imgBurnPath">Path to the ImgBurn exe</param>
        public static async Task BurnCDWithText(
            string source,
            string title,
            string author,
            bool isAlbum,
            string driveLetter,
            string imgBurnPath = @"C:\Program Files (x86)\ImgBurn\ImgBurn.exe")
        {
            await Task.Run(() =>
            {
                string cuePath = null;
                try
                {
                    if (!File.Exists(imgBurnPath))
                    {
                        throw new FileNotFoundException("ImgBurn required.");
                    }

                    StatusUpdated?.Invoke(null, "Scanning MP3 files...");
                    List<string> files = Directory.GetFiles(source, "*.mp3").OrderBy(f => f).ToList();

                    if (files.Count == 0)
                    {
                        throw new Exception("No MP3 files found in the source directory.");
                    }

                    StatusUpdated?.Invoke(null, "Generating CUE sheet with CD-Text...");
                    cuePath = Path.Combine(Path.GetTempPath(), $"Burn_{Guid.NewGuid()}.cue");

                    GenerateCueSheet(cuePath, files, title, author, isAlbum);

                    StatusUpdated?.Invoke(null, "Launching ImgBurn...");

                    var processInfo = new ProcessStartInfo
                    {
                        FileName = imgBurnPath,
                        // /MODE WRITE: Write to disc
                        // /SRC: The CUE sheet we just generated
                        // /DEST: The target CD drive
                        // /START: Start burning automatically
                        // /CLOSE: Close ImgBurn when done
                        // /NOIMAGEDETAILS: Skips user prompts
                        Arguments = $"/MODE WRITE /SRC \"{cuePath}\" /DEST \"{driveLetter}\" /START /CLOSE /NOIMAGEDETAILS",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using (Process process = Process.Start(processInfo))
                    {
                        // wait to finish
                        process.WaitForExit();

                        if (process.ExitCode != 0)
                        {
                            throw new Exception($"ImgBurn exited with error code: {process.ExitCode}");
                        }
                    }

                    StatusUpdated?.Invoke(null, "Burning completed.");
                    BurnCompleted?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    BurnError?.Invoke(null, ex);
                }
                finally
                {
                    // clean up
                    if (cuePath != null && File.Exists(cuePath))
                    {
                        try { File.Delete(cuePath); } catch { }
                    }
                }
            });
        }

        /// <summary>
        /// Generates a standard CUE sheet mapped with CD-Text parameters.
        /// </summary>
        private static void GenerateCueSheet(string outputPath, List<string> mp3Files, string title, string author, bool isAlbum)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"PERFORMER \"{author}\"");
            sb.AppendLine($"TITLE \"{title}\"");

            for (int i = 0; i < mp3Files.Count; i++)
            {
                string filePath = mp3Files[i];

                string trackTitle = Path.GetFileNameWithoutExtension(filePath);

                string trackArtist = isAlbum ? author : "Unknown Artist";

                try
                {
                    using (var tfile = TagLib.File.Create(filePath))
                    {
                        if (!string.IsNullOrWhiteSpace(tfile.Tag.Title))
                        {
                            trackTitle = tfile.Tag.Title;
                        }

                        if (tfile.Tag.Performers != null && tfile.Tag.Performers.Length > 0)
                        {
                            trackArtist = string.Join(", ", tfile.Tag.Performers);
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Failed to read tags for {filePath}: {ex.Message}");
                }

                sb.AppendLine($"FILE \"{filePath}\" MP3");
                sb.AppendLine($"  TRACK {(i + 1):D2} AUDIO");
                sb.AppendLine($"    TITLE \"{trackTitle}\"");
                sb.AppendLine($"    PERFORMER \"{trackArtist}\"");
                sb.AppendLine($"    INDEX 01 00:00:00");
            }

            File.WriteAllText(outputPath, sb.ToString(), Encoding.Default);
        }
    }
}
