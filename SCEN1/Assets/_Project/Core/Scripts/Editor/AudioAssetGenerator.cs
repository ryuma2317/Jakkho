using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace CADT.VR.Core.Editor
{
    /// <summary>
    /// Editor tool to procedurally generate essential UI and feedback audio clips (.wav)
    /// into the shared Assets/_Project/Audio/ folder.
    /// </summary>
    public static class AudioAssetGenerator
    {
        private const string AudioDirectory = "Assets/_Project/Audio";

        [MenuItem("CADT VR/Audio/Generate Shared SFX Clips", priority = 20)]
        public static void GenerateAudioAssets()
        {
            if (!Directory.Exists(AudioDirectory))
            {
                Directory.CreateDirectory(AudioDirectory);
            }

            CreateToneClip("UI_Click.wav", 1200f, 0.05f, 0.4f, WaveType.Sine);
            CreateToneClip("UI_Hover.wav", 800f, 0.03f, 0.2f, WaveType.Sine);
            CreateToneClip("Alert_Warning.wav", 650f, 0.25f, 0.6f, WaveType.Square);
            CreateToneClip("Success_Chime.wav", 1760f, 0.35f, 0.5f, WaveType.Sine);

            AssetDatabase.Refresh();
            Debug.Log("<color=#22C55E><b>[Audio Generator]</b></color> Procedural audio clips generated successfully in " + AudioDirectory);
        }

        private enum WaveType { Sine, Square }

        private static void CreateToneClip(string fileName, float freq, float duration, float volume, WaveType waveType)
        {
            string fullPath = Path.Combine(AudioDirectory, fileName);
            int sampleRate = 44100;
            int samples = (int)(sampleRate * duration);
            float[] audioData = new float[samples];

            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = 1.0f - ((float)i / samples); // Fade out
                float wave = 0f;

                if (waveType == WaveType.Sine)
                {
                    wave = Mathf.Sin(2f * Mathf.PI * freq * t);
                }
                else
                {
                    wave = Mathf.Sign(Mathf.Sin(2f * Mathf.PI * freq * t));
                }

                audioData[i] = wave * volume * envelope;
            }

            WriteWavFile(fullPath, audioData, sampleRate);
        }

        private static void WriteWavFile(string filepath, float[] data, int sampleRate)
        {
            using (var fileStream = new FileStream(filepath, FileMode.Create))
            using (var writer = new BinaryWriter(fileStream))
            {
                short channels = 1;
                short bitsPerSample = 16;
                int byteRate = sampleRate * channels * (bitsPerSample / 8);
                short blockAlign = (short)(channels * (bitsPerSample / 8));
                int subChunk2Size = data.Length * channels * (bitsPerSample / 8);
                int chunkSize = 36 + subChunk2Size;

                // RIFF header
                writer.Write(new char[4] { 'R', 'I', 'F', 'F' });
                writer.Write(chunkSize);
                writer.Write(new char[4] { 'W', 'A', 'V', 'E' });

                // fmt chunk
                writer.Write(new char[4] { 'f', 'm', 't', ' ' });
                writer.Write(16); // Subchunk1Size for PCM
                writer.Write((short)1); // AudioFormat 1 = PCM
                writer.Write(channels);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);

                // data chunk
                writer.Write(new char[4] { 'd', 'a', 't', 'a' });
                writer.Write(subChunk2Size);

                for (int i = 0; i < data.Length; i++)
                {
                    short sample = (short)(Mathf.Clamp(data[i], -1.0f, 1.0f) * 32767f);
                    writer.Write(sample);
                }
            }
        }
    }
}
