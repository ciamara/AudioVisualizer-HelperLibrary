using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NAudio.Wave;

namespace AudioVisualiser
{
    public class AudioHelper
    {
        /// <summary>
        /// converts mp3 into wav file
        /// </summary>
        /// <param name="mp3Path">
        /// path to mp3 file
        /// </param>
        /// <param name="wavPath">
        /// path to wav file
        /// </param>
        public void ConvertMp3ToWav(string mp3Path, string wavPath)
        {
            using (var reader = new Mp3FileReader(mp3Path))
            {
                WaveFileWriter.CreateWaveFile(wavPath, reader);
            }
        }
    }
}
