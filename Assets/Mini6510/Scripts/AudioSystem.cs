// Author: Fabrizio Radica
// Version: 1.0
// Description: SID-compatible programming model using Unity audio backend.
//              Supports frequency, waveform, ADSR, and master volume via original SID register addresses.

using System;
using UnityEngine;

namespace Mini6510
{
    // SID register layout (base $D400):
    //   Voice 1: $D400–$D406 (freq lo/hi, pw lo/hi, control, attack/decay, sustain/release)
    //   Voice 2: $D407–$D40D
    //   Voice 3: $D40E–$D414
    //   Filter:  $D415–$D417 (not emulated)
    //   Volume:  $D418

    public class AudioSystem : MonoBehaviour
    {
        private const int VOICE_COUNT    = 3;
        private const int SID_BASE       = Memory.SID_START;
        private const int SAMPLE_RATE    = 44100;
        private const float SID_CLOCK    = 985248f; // PAL C64 SID clock

        private Memory _mem;

        private class Voice
        {
            public float   Phase;
            public float   Frequency;
            public byte    Waveform;    // bits: 4=Triangle, 5=Sawtooth, 6=Pulse, 7=Noise
            public bool    Gate;
            public float   Attack, Decay, Sustain, Release;
            public float   EnvLevel;
            public int     EnvState;   // 0=idle,1=attack,2=decay,3=sustain,4=release
            public System.Random Noise = new System.Random();
            public float   NoiseSample;
            public int     NoiseCounter;
            public ushort  PulseWidth;
        }

        private readonly Voice[] _voices = new Voice[VOICE_COUNT]
        {
            new Voice(), new Voice(), new Voice()
        };

        private float _masterVolume = 1f;
        private AudioClip  _clip;
        private AudioSource _source;
        private float[]     _buffer;
        private int         _bufferPos;
        private const int   BUFFER_SIZE = 4096;

        public void Initialize(Memory mem)
        {
            _mem    = mem;
            _buffer = new float[BUFFER_SIZE];

            mem.OnSIDWrite += OnSIDRegisterWrite;

            _source           = gameObject.AddComponent<AudioSource>();
            _source.loop      = true;
            _source.playOnAwake = false;

            _clip = AudioClip.Create("SID", BUFFER_SIZE, 1, SAMPLE_RATE, true, OnAudioRead);
            _source.clip = _clip;
            _source.Play();
        }

        private void OnSIDRegisterWrite(int addr, byte val)
        {
            int offset = addr - SID_BASE;
            int voice  = offset / 7;
            int reg    = offset % 7;

            if (voice >= 0 && voice < VOICE_COUNT)
            {
                var v = _voices[voice];
                int vBase = SID_BASE + voice * 7;

                switch (reg)
                {
                    case 0: // freq lo
                    case 1: // freq hi
                    {
                        byte lo = _mem.Read(vBase + 0);
                        byte hi = _mem.Read(vBase + 1);
                        ushort sidFreq = (ushort)((hi << 8) | lo);
                        v.Frequency = sidFreq * SID_CLOCK / 16777216f;
                        break;
                    }
                    case 2: // pw lo
                    case 3: // pw hi
                    {
                        byte lo = _mem.Read(vBase + 2);
                        byte hi = _mem.Read(vBase + 3);
                        v.PulseWidth = (ushort)(((hi & 0x0F) << 8) | lo);
                        break;
                    }
                    case 4: // control
                        v.Gate     = (val & 0x01) != 0;
                        v.Waveform = (byte)(val >> 4);
                        if (!v.Gate) v.EnvState = 4; // release
                        else         v.EnvState = 1; // attack
                        break;
                    case 5: // attack/decay
                        v.Attack = ADSRTime((val >> 4) & 0x0F);
                        v.Decay  = ADSRTime(val & 0x0F);
                        break;
                    case 6: // sustain/release
                        v.Sustain = ((val >> 4) & 0x0F) / 15f;
                        v.Release = ADSRTime(val & 0x0F);
                        break;
                }
            }
            else if (addr == SID_BASE + 0x18)
            {
                _masterVolume = (val & 0x0F) / 15f;
            }
        }

        private void OnAudioRead(float[] data)
        {
            int len = data.Length;
            for (int i = 0; i < len; i++)
            {
                float sample = 0f;
                foreach (var v in _voices)
                    sample += SampleVoice(v, 1f / SAMPLE_RATE);
                data[i] = sample / VOICE_COUNT * _masterVolume;
            }
        }

        private float SampleVoice(Voice v, float dt)
        {
            if (v.EnvState == 0) return 0f;

            // Advance envelope
            switch (v.EnvState)
            {
                case 1: // attack
                    v.EnvLevel += dt / v.Attack;
                    if (v.EnvLevel >= 1f) { v.EnvLevel = 1f; v.EnvState = 2; }
                    break;
                case 2: // decay
                    v.EnvLevel -= dt / v.Decay * (1f - v.Sustain);
                    if (v.EnvLevel <= v.Sustain) { v.EnvLevel = v.Sustain; v.EnvState = 3; }
                    break;
                case 3: // sustain
                    v.EnvLevel = v.Sustain;
                    break;
                case 4: // release
                    v.EnvLevel -= dt / v.Release;
                    if (v.EnvLevel <= 0f) { v.EnvLevel = 0f; v.EnvState = 0; }
                    break;
            }

            // Generate waveform
            if (v.Frequency <= 0f) return 0f;
            v.Phase += v.Frequency * dt;
            if (v.Phase >= 1f) v.Phase -= 1f;

            float wave = 0f;
            if ((v.Waveform & 0x4) != 0) // Triangle
            {
                wave = v.Phase < 0.5f ? v.Phase * 4f - 1f : 3f - v.Phase * 4f;
            }
            else if ((v.Waveform & 0x2) != 0) // Sawtooth
            {
                wave = v.Phase * 2f - 1f;
            }
            else if ((v.Waveform & 0x1) != 0) // Pulse
            {
                float duty = v.PulseWidth / 4095f;
                wave = v.Phase < duty ? 1f : -1f;
            }
            else if ((v.Waveform & 0x8) != 0) // Noise
            {
                v.NoiseCounter++;
                if (v.NoiseCounter >= SAMPLE_RATE / Mathf.Max(v.Frequency, 1f))
                {
                    v.NoiseSample  = (float)(v.Noise.NextDouble() * 2.0 - 1.0);
                    v.NoiseCounter = 0;
                }
                wave = v.NoiseSample;
            }

            return wave * v.EnvLevel;
        }

        private static float ADSRTime(int nibble)
        {
            // C64 ADSR time table in seconds (approximate)
            float[] times =
            {
                0.002f, 0.008f, 0.016f, 0.024f,
                0.038f, 0.056f, 0.068f, 0.080f,
                0.100f, 0.250f, 0.500f, 0.800f,
                1.000f, 3.000f, 5.000f, 8.000f
            };
            return times[nibble & 0x0F];
        }
    }
}
