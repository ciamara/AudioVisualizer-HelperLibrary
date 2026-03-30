using System;
using System.Drawing.Imaging;
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
        public static event EventHandler<string> StatusUpdated = delegate { };
    public static event EventHandler<Exception> BurnError = delegate { };
        public static event EventHandler BurnCompleted = delegate { };

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
                IMAPI2.IStream bootStream = null;

                [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true, PreserveSig = false)]
                static extern void SHCreateStreamOnFileW(string fileName, uint mode, out IMAPI2.IStream stream);

                const uint STGM_READ = 0x00000000;

                try
                {
                    // checking drive support
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

                    // checking media support
                    discFormatData = new MsftDiscFormat2Data();
                    discFormatData.Recorder = discRecorder;

                    if (discFormatData.IsRecorderSupported(discRecorder))
                    {
                        Console.WriteLine("Current recorder IS supported.");
                    }
                    else
                    {
                        Console.WriteLine("Current recorder IS NOT supported.");
                    }

                    if (discFormatData.IsCurrentMediaSupported(discRecorder))
                    {
                        Console.WriteLine("Current media IS supported.");
                    }
                    else
                    {
                        Console.WriteLine("Current media IS NOT supported.");
                    }

                    Console.WriteLine($"client name: {discFormatData.ClientName}");

                    IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE mediaType = discFormatData.CurrentPhysicalMediaType;

                    switch (mediaType)
                    {
                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_UNKNOWN:
                            Console.WriteLine("Empty device or an unknown disc type.");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDROM:
                            Console.WriteLine("CD-ROM");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDR:
                            Console.WriteLine("CD-R");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_CDRW:
                            Console.WriteLine("CD-RW");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDROM:
                            Console.WriteLine("Read-only DVD drive and/or disc");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDRAM:
                            Console.WriteLine("DVD-RAM");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR:
                            Console.WriteLine("DVD+R");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSRW:
                            Console.WriteLine("DVD+RW");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDPLUSR_DUALLAYER:
                            Console.WriteLine("DVD+R Dual Layer media");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHR:
                            Console.WriteLine("DVD-R");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHRW:
                            Console.WriteLine("DVD-RW");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DVDDASHR_DUALLAYER:
                            Console.WriteLine("DVD-R Dual Layer media");
                            break;

                        case IMAPI2.IMAPI_MEDIA_PHYSICAL_TYPE.IMAPI_MEDIA_TYPE_DISK:
                            Console.WriteLine("Randomly-writable, hardware-defect ");
                            break;
                    }
                    //// file system image
                    //fileSystemImage = new MsftFileSystemImage();
                    //fileSystemImage.ChooseImageDefaults((IMAPI2FS.IDiscRecorder2)discRecorder);
                    //fileSystemImage.VolumeName = volumeLabel;
                    //fileSystemImage.Root.AddTree(source, false);

                    //// result image
                    //IFileSystemImageResult result = fileSystemImage.CreateResultImage();
                    //IMAPI2.IStream imageStream = (IMAPI2.IStream)result.ImageStream;

                    //// write
                    //discFormatData.Write(imageStream);

                    //BurnCompleted?.Invoke(null, EventArgs.Empty);
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
