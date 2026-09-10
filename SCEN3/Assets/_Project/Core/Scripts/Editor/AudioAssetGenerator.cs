using System;
using System.IO;
using UnityEngine;
using UnityEditor;

public static class AudioAssetGenerator
{
    private const int SampleRate = 44100;

    [MenuItem("Tools/JakkhoVR/Generate Audio Assets")]
    public static void GenerateAudioAssets()
    {
        string dir = Path.Combine(Application.dataPath, "_Project", "Audio");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        // 1. Phone Ringtone (3.0s, warm marimba chime sequence with natural breath pause)
        SaveWav(Path.Combine(dir, "Phone_Ringtone.wav"), GenerateMarimbaRingtone(3.0f));

        // 2. Notification Chime (0.45s, friendly 2-note message ding)
        SaveWav(Path.Combine(dir, "Phone_Notification.wav"), GenerateNotificationChime(0.45f));

        // 3. Success Fanfare (1.2s, pleasant ascending major arpeggio)
        SaveWav(Path.Combine(dir, "Success_Fanfare.wav"), GenerateSuccessFanfare(1.2f));

        // 4. Security Warning Alert (0.6s, subtle digital ping)
        SaveWav(Path.Combine(dir, "Security_Warning.wav"), GenerateWarningAlert(0.6f));

        AssetDatabase.Refresh();
        Debug.Log("[AudioAssetGenerator] Successfully generated audio assets in Assets/_Project/Audio/");
    }

    private static float[] GenerateMarimbaRingtone(float totalDuration)
    {
        int totalSamples = (int)(totalDuration * SampleRate);
        float[] buffer = new float[totalSamples];

        // Notes (Hz): E5=659.25, G#5=830.61, B5=987.77, E6=1318.51
        // Phrase 1: Ascending roll
        AddMarimbaNote(buffer, 0.00f, 659.25f, 0.45f, 0.70f);
        AddMarimbaNote(buffer, 0.18f, 830.61f, 0.45f, 0.75f);
        AddMarimbaNote(buffer, 0.36f, 987.77f, 0.45f, 0.80f);
        AddMarimbaNote(buffer, 0.54f, 1318.51f, 0.70f, 0.90f);

        // Phrase 2: Echo return
        AddMarimbaNote(buffer, 1.05f, 987.77f, 0.40f, 0.70f);
        AddMarimbaNote(buffer, 1.22f, 830.61f, 0.40f, 0.70f);
        AddMarimbaNote(buffer, 1.39f, 659.25f, 0.85f, 0.80f);
        // 2.24s to 3.0s is clean silence before loop

        Normalize(buffer, 0.85f);
        return buffer;
    }

    private static float[] GenerateNotificationChime(float totalDuration)
    {
        int totalSamples = (int)(totalDuration * SampleRate);
        float[] buffer = new float[totalSamples];

        // Friendly two-tone ding: A5 (880Hz) -> C#6 (1108.73Hz)
        AddMarimbaNote(buffer, 0.00f, 880.00f, 0.25f, 0.70f);
        AddMarimbaNote(buffer, 0.10f, 1108.73f, 0.35f, 0.85f);

        Normalize(buffer, 0.80f);
        return buffer;
    }

    private static float[] GenerateSuccessFanfare(float totalDuration)
    {
        int totalSamples = (int)(totalDuration * SampleRate);
        float[] buffer = new float[totalSamples];

        // C major chord arpeggio: C5 (523.25) -> E5 (659.25) -> G5 (783.99) -> C6 (1046.50)
        AddMarimbaNote(buffer, 0.00f, 523.25f, 0.35f, 0.65f);
        AddMarimbaNote(buffer, 0.14f, 659.25f, 0.35f, 0.70f);
        AddMarimbaNote(buffer, 0.28f, 783.99f, 0.35f, 0.75f);
        AddMarimbaNote(buffer, 0.42f, 1046.50f, 0.75f, 0.90f);

        Normalize(buffer, 0.85f);
        return buffer;
    }

    private static float[] GenerateWarningAlert(float totalDuration)
    {
        int totalSamples = (int)(totalDuration * SampleRate);
        float[] buffer = new float[totalSamples];

        // Gentle dual-tone warning ping
        AddMarimbaNote(buffer, 0.00f, 880.00f, 0.20f, 0.60f);
        AddMarimbaNote(buffer, 0.18f, 659.25f, 0.35f, 0.60f);

        Normalize(buffer, 0.75f);
        return buffer;
    }

    private static void AddMarimbaNote(float[] buffer, float startTime, float frequency, float duration, float amplitude)
    {
        int startSample = (int)(startTime * SampleRate);
        int noteSamples = (int)(duration * SampleRate);

        for (int i = 0; i < noteSamples; i++)
        {
            int targetIndex = startSample + i;
            if (targetIndex >= buffer.Length) break;

            float t = (float)i / SampleRate;

            // Warm wooden chime: fundamental + 2nd harmonic (octave) + 3rd harmonic
            float wave1 = Mathf.Sin(2f * Mathf.PI * frequency * t);
            float wave2 = Mathf.Sin(4f * Mathf.PI * frequency * t) * 0.30f;
            float wave3 = Mathf.Sin(6f * Mathf.PI * frequency * t) * 0.10f;
            float rawWave = wave1 + wave2 + wave3;

            // Exponential decay envelope (fast attack 3ms, exponential tail)
            float attack = Mathf.Clamp01(t / 0.003f);
            float decay = Mathf.Exp(-8.5f * t);
            float envelope = attack * decay;

            buffer[targetIndex] += rawWave * envelope * amplitude;
        }
    }

    private static void Normalize(float[] buffer, float peakTarget)
    {
        float max = 0f;
        for (int i = 0; i < buffer.Length; i++)
        {
            float abs = Mathf.Abs(buffer[i]);
            if (abs > max) max = abs;
        }

        if (max > 0.0001f)
        {
            float scale = peakTarget / max;
            for (int i = 0; i < buffer.Length; i++)
            {
                buffer[i] *= scale;
            }
        }
    }

    private static void SaveWav(string filePath, float[] samples)
    {
        using (FileStream fs = new FileStream(filePath, FileMode.Create))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int channels = 1;
            int bitsPerSample = 16;
            int byteRate = SampleRate * channels * (bitsPerSample / 8);
            int blockAlign = channels * (bitsPerSample / 8);
            int subChunk2Size = samples.Length * channels * (bitsPerSample / 8);
            int chunkSize = 36 + subChunk2Size;

            // RIFF chunk descriptor
            bw.Write(new char[] { 'R', 'I', 'F', 'F' });
            bw.Write(chunkSize);
            bw.Write(new char[] { 'W', 'A', 'V', 'E' });

            // "fmt " sub-chunk
            bw.Write(new char[] { 'f', 'm', 't', ' ' });
            bw.Write(16); // Subchunk1Size for PCM
            bw.Write((short)1); // AudioFormat 1 = PCM
            bw.Write((short)channels);
            bw.Write(SampleRate);
            bw.Write(byteRate);
            bw.Write((short)blockAlign);
            bw.Write((short)bitsPerSample);

            // "data" sub-chunk
            bw.Write(new char[] { 'd', 'a', 't', 'a' });
            bw.Write(subChunk2Size);

            // Write 16-bit PCM samples
            for (int i = 0; i < samples.Length; i++)
            {
                short val = (short)Mathf.Clamp(samples[i] * 32767f, -32768f, 32767f);
                bw.Write(val);
            }
        }
    }
}
