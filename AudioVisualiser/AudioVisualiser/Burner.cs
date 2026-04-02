using System;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using IMAPI2;
using NAudio.Wave;
using IMAPI2FS;

namespace AudioHelpers
{
    public class Burner
    {
        public static event EventHandler<string> StatusUpdated = delegate { };
        public static event EventHandler<Exception> BurnError = delegate { };
        public static event EventHandler BurnCompleted = delegate { };

        private const int BytesPerSector = 2352;

        [DllImport("shlwapi.dll", EntryPoint = "SHCreateStreamOnFileW", CharSet = CharSet.Unicode, PreserveSig = true)]
        private static extern int SHCreateStreamOnFile(string pszFile, uint grfMode, out IMAPI2.IStream ppstm);

        private const uint STGM_READ = 0x00000000;
        private const uint STGM_SHARE_DENY_WRITE = 0x00000020;

        /// <summary>
        /// burns mp3 directory into cd
        /// </summary>
        /// <param name="source">mp3 file directory</param>
        /// <param name="volumeLabel">disc name</param>
        public static async Task BurnCD(string source, string volumeLabel)
        {
            await Task.Run(() =>
            {
                MsftDiscMaster2 discMaster = null;
                MsftDiscRecorder2 discRecorder = null;
                MsftDiscFormat2TrackAtOnce discFormatAudio = null;

                try
                {
                    discMaster = new MsftDiscMaster2();
                    if (discMaster.Count == 0) throw new Exception("no cd drive");

                    string recorderUniqueId = (string)discMaster[0];
                    discRecorder = new MsftDiscRecorder2();
                    discRecorder.InitializeDiscRecorder(recorderUniqueId);

                    discFormatAudio = new MsftDiscFormat2TrackAtOnce();
                    discFormatAudio.Recorder = discRecorder;
                    discFormatAudio.ClientName = "KithBurner";

                    StatusUpdated?.Invoke(null, "preparing cd...");
                    discFormatAudio.PrepareMedia();

                    // sorting files for correct order
                    var files = Directory.GetFiles(source, "*.mp3").OrderBy(f => f).ToList();

                    foreach (string file in files)
                    {
                        StatusUpdated?.Invoke(null, $"adding: {Path.GetFileName(file)}");

                        string tempPcmPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".raw");

                        try
                        {
                            PreparePcmFile(file, tempPcmPath);

                            int hResult = SHCreateStreamOnFile(tempPcmPath, STGM_READ | STGM_SHARE_DENY_WRITE, out IMAPI2.IStream audioStream);

                            if (hResult != 0)
                            {
                                throw new Exception($"com stream error: {hResult}");
                            }

                            try
                            {
                                // add cd text
                                discFormatAudio.AddAudioTrack(audioStream);
                            }
                            finally
                            {
                                // releasing com 
                                if (audioStream != null) Marshal.ReleaseComObject(audioStream);
                            }
                        }
                        catch (Exception ex)
                        {
                            throw new Exception($"error processing file {Path.GetFileName(file)}: {ex.Message}");
                        }
                        finally
                        {
                            try
                            {
                                if (File.Exists(tempPcmPath)) File.Delete(tempPcmPath);
                            }
                            catch { }
                        }
                    }

                    StatusUpdated?.Invoke(null, "finalizing...");
                    discFormatAudio.ReleaseMedia();

                    discRecorder.EjectMedia();

                    Console.WriteLine("Burning ended.");
                    BurnCompleted?.Invoke(null, EventArgs.Empty);
                }
                catch (Exception ex)
                {
                    BurnError?.Invoke(null, ex);
                }
                finally
                {
                    if (discFormatAudio != null) Marshal.ReleaseComObject(discFormatAudio);
                    if (discRecorder != null) Marshal.ReleaseComObject(discRecorder);
                    if (discMaster != null) Marshal.ReleaseComObject(discMaster);
                }
            });
        }

        private static void PreparePcmFile(string inputMp3, string outputRaw)
        {
            using (var reader = new AudioFileReader(inputMp3))
            {
                var targetFormat = new WaveFormat(44100, 16, 2);
                using (var resampler = new MediaFoundationResampler(reader, targetFormat))
                {
                    resampler.ResamplerQuality = 60;

                    using (var outStream = new FileStream(outputRaw, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        byte[] buffer = new byte[targetFormat.AverageBytesPerSecond];
                        int read;
                        long totalBytesWritten = 0;

                        while ((read = resampler.Read(buffer, 0, buffer.Length)) > 0)
                        {
                            outStream.Write(buffer, 0, read);
                            totalBytesWritten += read;
                        }

                        //alignment to 2352
                        int remainder = (int)(totalBytesWritten % BytesPerSector);
                        if (remainder > 0)
                        {
                            int paddingNeeded = BytesPerSector - remainder;
                            byte[] padding = new byte[paddingNeeded];
                            outStream.Write(padding, 0, padding.Length);
                        }
                    }
                }
            }
        }
    }
}