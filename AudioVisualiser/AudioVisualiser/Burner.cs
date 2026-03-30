using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Threading.Tasks;
using IMAPI2;
using IMAPI2FS;

namespace AudioHelpers
{
    public class Burner
    {
        public static event EventHandler<string> StatusUpdated;
        public static event EventHandler<Exception> BurnError;
        public static event EventHandler BurnCompleted;

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
                MsftFileSystemImage fileSystemImage = null;
                MsftDiscFormat2Data discFormatData = null;

                try
                {
                    discMaster = new MsftDiscMaster2();

                    if (discMaster.Count == 0)
                    {
                        Console.WriteLine("No disc recorders.");
                        return;
                    }

                    string recorderUniqueId = (string)discMaster[0];

                    discRecorder = new MsftDiscRecorder2();
                    discRecorder.InitializeDiscRecorder(recorderUniqueId);


                    Console.WriteLine($"Active disc recorder: {discRecorder.ActiveDiscRecorder}");

                    foreach(string mountPoint in discRecorder.VolumePathNames)
                    {
                        Console.WriteLine($"Mount point: {mountPoint}");
                    }
                }
                catch (Exception ex)
                {
                    BurnError?.Invoke(null, ex);
                }
                finally
                {
                    if (discFormatData != null) Marshal.ReleaseComObject(discFormatData);
                    if (fileSystemImage != null) Marshal.ReleaseComObject(fileSystemImage);
                    if (discRecorder != null) Marshal.ReleaseComObject(discRecorder);
                    if (discMaster != null) Marshal.ReleaseComObject(discMaster);
                }
            });
        }
    }
}
