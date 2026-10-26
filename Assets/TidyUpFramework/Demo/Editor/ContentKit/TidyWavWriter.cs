using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace TidyUpFramework.Demo.Editor.ContentKit
{
    public static class TidyWavWriter
    {
        private const short PcmFormat = 1;
        private const short MonoChannels = 1;
        private const short BitsPerSample = 16;
        private const int BytesPerSample = BitsPerSample / 8;
        private const int FormatChunkSize = 16;
        private const int RiffHeaderRemainder = 36;
        private const float PcmScale = 32767f;
        private const float LongClipSeconds = 10f;
        private const float VorbisQuality = 0.7f;

        public static AudioClip SaveWav(float[] samples, int sampleRate, string assetPath, bool loop = false, bool stream = false)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            string path = TidyAssetPaths.Normalize(assetPath);
            TidyAssetPaths.WriteAndImport(path, Encode(samples, sampleRate));
            ConfigureImporter(path, samples.Length / (float)Mathf.Max(1, sampleRate), loop, stream);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        public static byte[] Encode(float[] samples, int sampleRate)
        {
            int dataSize = samples.Length * BytesPerSample;
            using (var stream = new MemoryStream(RiffHeaderRemainder + 8 + dataSize))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Encoding.ASCII.GetBytes("RIFF"));
                writer.Write(RiffHeaderRemainder + dataSize);
                writer.Write(Encoding.ASCII.GetBytes("WAVE"));
                writer.Write(Encoding.ASCII.GetBytes("fmt "));
                writer.Write(FormatChunkSize);
                writer.Write(PcmFormat);
                writer.Write(MonoChannels);
                writer.Write(sampleRate);
                writer.Write(sampleRate * MonoChannels * BytesPerSample);
                writer.Write((short)(MonoChannels * BytesPerSample));
                writer.Write(BitsPerSample);
                writer.Write(Encoding.ASCII.GetBytes("data"));
                writer.Write(dataSize);
                foreach (float sample in samples)
                {
                    writer.Write((short)Mathf.RoundToInt(Mathf.Clamp(sample, -1f, 1f) * PcmScale));
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void ConfigureImporter(string path, float durationSeconds, bool loop, bool stream)
        {
            if (!(AssetImporter.GetAtPath(path) is AudioImporter importer))
            {
                return;
            }

            bool longClip = durationSeconds > LongClipSeconds;
            AudioImporterSampleSettings settings = importer.defaultSampleSettings;
            settings.loadType = SelectLoadType(longClip, stream);
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = VorbisQuality;
            settings.sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate;
            settings.preloadAudioData = settings.loadType != AudioClipLoadType.Streaming;
            importer.defaultSampleSettings = settings;
            importer.forceToMono = true;
            importer.loadInBackground = loop || longClip;
            importer.SaveAndReimport();
        }

        private static AudioClipLoadType SelectLoadType(bool longClip, bool stream)
        {
            if (stream)
            {
                return AudioClipLoadType.Streaming;
            }

            return longClip ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
        }
    }
}
