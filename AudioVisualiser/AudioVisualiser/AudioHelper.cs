using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System;
using System.IO;
using System.Numerics;
using TagLib;
using TagLib.Flac;

using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;
using Windows.UI;
using Microsoft.UI;
using Windows.Foundation;

namespace AudioVisualiser
{

    [System.Runtime.InteropServices.ComImport]
    [System.Runtime.InteropServices.Guid("5B0D3235-4DB1-4D88-A10E-859F233F5984")]
    [System.Runtime.InteropServices.InterfaceType(System.Runtime.InteropServices.ComInterfaceType.InterfaceIsIUnknown)]
    public unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    public class AudioHelper : IDisposable
    {
        private IWavePlayer _player;
        private AudioFileReader _reader;
        private Sampler _sampler;

        public event EventHandler<float[]> FftCalculated;

        

        public void PlayMp3(string filePath)
        {
            _player?.Stop();
            _player?.Dispose();
            _reader?.Dispose();

            _reader = new AudioFileReader(filePath);

            // fftsize must be power of 2
            _sampler = new Sampler(_reader, 1024);
            _sampler.FftCalculated += (s, fftData) => FftCalculated?.Invoke(this, fftData);

            _player = new WaveOutEvent();
            _player.Init(_sampler);
            _player.Play();
        }

        public void Stop() => _player?.Stop();

        public void Dispose()
        {
            _player?.Dispose();
            _reader?.Dispose();
        }
    }
}
