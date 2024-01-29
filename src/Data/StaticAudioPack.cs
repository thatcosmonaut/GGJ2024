using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonWorks.Audio;

namespace RollAndCash.Data
{
	[JsonSerializable(typeof(Dictionary<string, StaticAudioPackDataEntry>))]
	internal partial class StaticAudioPackDictionaryContext : JsonSerializerContext
	{
	}

	public class StaticAudioPack : IDisposable
	{
		public FileInfo AudioFile { get; }
		public FileInfo JsonFile { get; }

		public AudioBuffer MainBuffer { get; private set; }
		public bool Loaded => MainBuffer != null;
		private bool IsDisposed;

		private Dictionary<string, AudioBuffer> AudioBuffers = new Dictionary<string, AudioBuffer>();

		private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static StaticAudioPackDictionaryContext serializerContext = new StaticAudioPackDictionaryContext(serializerOptions);

		public StaticAudioPack(string audioFilePath, string jsonFilePath)
		{
			AudioFile = new FileInfo(audioFilePath);
			JsonFile = new FileInfo(jsonFilePath);
		}

		public unsafe void Load(AudioDevice audioDevice)
		{
			if (Loaded)
			{
				return;
			}

			MainBuffer = AudioDataWav.CreateBuffer(audioDevice, AudioFile.FullName);

			var entries = JsonSerializer.Deserialize(
				File.ReadAllText(JsonFile.FullName),
				typeof(Dictionary<string, StaticAudioPackDataEntry>),
				serializerContext) as Dictionary<string, StaticAudioPackDataEntry>;

			foreach (var (name, dataEntry) in entries)
			{
				AudioBuffers[name] = MainBuffer.Slice(dataEntry.Start, (uint)dataEntry.Length);
			}
		}

		public AudioBuffer GetAudioBuffer(string name)
		{
			return AudioBuffers[name];
		}

		protected virtual unsafe void Dispose(bool disposing)
		{
			if (!IsDisposed)
			{
				if (disposing)
				{
					foreach (var sound in AudioBuffers.Values)
					{
						sound.Dispose();
					}

					MainBuffer.Dispose();
				}

				IsDisposed = true;
			}
		}

		~StaticAudioPack()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: false);
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
