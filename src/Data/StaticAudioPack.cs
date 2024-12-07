using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonWorks.AsyncIO;
using MoonWorks.Audio;

namespace RollAndCash.Data
{
	[JsonSerializable(typeof(Dictionary<string, StaticAudioPackDataEntry>))]
	internal partial class StaticAudioPackDictionaryContext : JsonSerializerContext
	{
	}

	public class StaticAudioPack : IDisposable
	{
		public FileInfo AudioFile { get; private set; }

		public AudioBuffer MainBuffer { get; private set; }
		private bool IsDisposed;

		private Dictionary<string, StaticAudioPackDataEntry> Entries;
		private Dictionary<string, AudioBuffer> AudioBuffers = new Dictionary<string, AudioBuffer>();

		private static JsonSerializerOptions serializerOptions = new JsonSerializerOptions
		{
			IncludeFields = true
		};

		private static StaticAudioPackDictionaryContext serializerContext = new StaticAudioPackDictionaryContext(serializerOptions);

		public void Init(AudioDevice audioDevice, string audioFilePath, string jsonFilePath)
		{
			AudioFile = new FileInfo(audioFilePath);
			Entries = JsonSerializer.Deserialize(
				File.ReadAllText(jsonFilePath),
				typeof(Dictionary<string, StaticAudioPackDataEntry>),
				serializerContext) as Dictionary<string, StaticAudioPackDataEntry>;
			MainBuffer = AudioBuffer.Create(audioDevice);
		}

		public void LoadAsync(AsyncFileLoader loader)
		{
			loader.EnqueueWavLoad(AudioFile.FullName, MainBuffer);
		}

		/// <summary>
		/// Call this after the audio buffer data is loaded.
		/// </summary>
		public void SliceBuffers()
		{
			foreach (var (name, dataEntry) in Entries)
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
