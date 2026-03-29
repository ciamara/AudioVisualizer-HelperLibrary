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
        public event EventHandler<string> StatusUpdated;
        public event EventHandler<Exception> BurnError;
        public event EventHandler BurnCompleted;

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

                }
                catch (Exception ex)
                {
                    BurnError?.Invoke(this, ex);
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
