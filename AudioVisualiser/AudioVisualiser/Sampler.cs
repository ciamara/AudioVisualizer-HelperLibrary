using MathNet.Numerics.IntegralTransforms;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;


namespace AudioVisualiser
{
    /// <summary>
    /// SampleProvider that intercepts audio data for fft
    /// </summary>
    internal class Sampler : ISampleProvider
    {
        private readonly ISampleProvider _source;
        private readonly int _fftSize;
        private readonly float[] _buffer;
        private int _bufferPos;

        public WaveFormat WaveFormat => _source.WaveFormat;
        public event EventHandler<float[]> FftCalculated;

        public Sampler(ISampleProvider source, int fftSize)
        {
            _source = source;
            _fftSize = fftSize;
            _buffer = new float[fftSize];
        }

        public int Read(float[] buffer, int offset, int count)
        {
            // source
            int samplesRead = _source.Read(buffer, offset, count);

            for (int n = 0; n < samplesRead; n++)
            {
                // samples in buffer
                _buffer[_bufferPos++] = buffer[offset + n];

                // calculate fft with enough samples
                if (_bufferPos >= _fftSize)
                {
                    _bufferPos = 0;
                    CalculateFft();
                }
            }

            return samplesRead;
        }

        private void CalculateFft()
        {
            // hann windowing
            Complex[] complexSamples = new Complex[_fftSize];
            for (int i = 0; i < _fftSize; i++)
            {
                float window = (float)(0.5 * (1.0 - Math.Cos(2 * Math.PI * i / (_fftSize - 1))));
                complexSamples[i] = new Complex(_buffer[i] * window, 0);
            }

            // transform
            Fourier.Forward(complexSamples, FourierOptions.Default);

            // extract magnitudes
            float[] magnitudes = new float[_fftSize / 2];
            for (int i = 0; i < _fftSize / 2; i++)
            {
                magnitudes[i] = (float)complexSamples[i].Magnitude;
            }

            // to ui
            FftCalculated?.Invoke(this, magnitudes);
        }
    }
}
