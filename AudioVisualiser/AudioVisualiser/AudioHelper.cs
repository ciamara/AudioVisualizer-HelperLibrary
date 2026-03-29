using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System.Numerics;

namespace AudioHelpers
{
    public class AudioHelper : IDisposable
    {
        private FileStream _fileStream;
        private Mp3FileReader _reader;
        private ISampleProvider _sampleProvider;

        private const int FftSize = 1024;
        private float[] _buffer = new float[FftSize * 2]; // stereo
        private float[] _monoBuffer = new float[FftSize];
        private Complex[] _complexBuffer = new Complex[FftSize];

        private bool _hasPrintedNullError = false;

        public void Load(string filePath)
        {
            Dispose(); // clean up previous track
            _hasPrintedNullError = false; // reset error flag for new track

            try
            {
                //Console.WriteLine($"[AudioHelper] Attempting to load file: {filePath}");

                // share prevents locking out
                _fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                _reader = new Mp3FileReader(_fileStream);
                _sampleProvider = _reader.ToSampleProvider();

                //Console.WriteLine($"[AudioHelper] Successfully loaded and shared");
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[AudioHelper] load error: {ex.Message}");
                _reader = null;
                _sampleProvider = null;
            }
        }

        public float[] GetFft(TimeSpan position)
        {
            if (_reader == null || _sampleProvider == null)
            {
                if (!_hasPrintedNullError)
                {
                    //Console.WriteLine("[AudioHelper] GetFft failed: _reader or _sampleProvider is null");
                    _hasPrintedNullError = true;
                }
                return null;
            }

            try
            {
                // seek exact pos
                _reader.CurrentTime = position;

                int channels = _sampleProvider.WaveFormat.Channels;
                int samplesToRead = FftSize * channels;

                int samplesRead = _sampleProvider.Read(_buffer, 0, samplesToRead);
                if (samplesRead == 0) return null;

                // mono conversion (512 samples after)
                int monoIndex = 0;
                for (int i = 0; i < samplesRead && monoIndex < FftSize; i += channels)
                {
                    float monoSample = 0;
                    for (int c = 0; c < channels; c++)
                    {
                        monoSample += _buffer[i + c];
                    }
                    _monoBuffer[monoIndex++] = monoSample / channels;
                }

                while (monoIndex < FftSize)
                {
                    _monoBuffer[monoIndex++] = 0;
                }

                // hann window (smoothing edges)
                for (int i = 0; i < FftSize; i++)
                {
                    float window = (float)(0.5 * (1.0 - Math.Cos(2 * Math.PI * i / (FftSize - 1))));
                    _complexBuffer[i] = new Complex(_monoBuffer[i] * window, 0);
                }

                // fft execution (512 bins, 0-bass, 511-treble)
                Fourier.Forward(_complexBuffer, FourierOptions.Default);

                // magnitudes (how loud specific frequency is)
                float[] magnitudes = new float[FftSize / 2];
                for (int i = 0; i < FftSize / 2; i++)
                {
                    magnitudes[i] = (float)_complexBuffer[i].Magnitude;
                }

                // (512 magnitudes))
                return magnitudes;
            }
            catch (Exception ex)
            {
                //Console.WriteLine($"[AudioHelper] get fft error: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _reader = null;

            _fileStream?.Dispose();
            _fileStream = null;

            _sampleProvider = null;
        }
    }
}