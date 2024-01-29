using System;
using GGJ2024.Content;
using GGJ2024.Messages;
using GGJ2024.Utility;
using MoonTools.ECS;
using MoonWorks.Audio;

namespace GGJ2024.Systems;

public class Audio : MoonTools.ECS.System
{
	AudioDevice AudioDevice;

	StreamingVoice MusicVoice;

	StreamingSoundID[] GameplaySongs;

	public Audio(World world, AudioDevice audioDevice) : base(world)
	{
		AudioDevice = audioDevice;

		GameplaySongs = new StreamingSoundID[]
		{
			StreamingAudio.attentiontwerkers,
			StreamingAudio.attention_shoppers_v1,
			StreamingAudio.attention_shoppers_v2,
		};

		var streamingAudioData = StreamingAudio.Lookup(StreamingAudio.attention_shoppers_v1);
		MusicVoice = AudioDevice.Obtain<StreamingVoice>(streamingAudioData.Format);
		MusicVoice.SetVolume(0.5f);
	}

	public override void Update(TimeSpan delta)
	{
		foreach (var staticSoundMessage in ReadMessages<PlayStaticSoundMessage>())
		{
			PlayStaticSound(
				staticSoundMessage.Sound,
				staticSoundMessage.Volume,
				staticSoundMessage.Pitch,
				staticSoundMessage.Pan
			);
		}

		foreach (var songMessage in ReadMessages<PlaySongMessage>())
		{
			var streamingAudioData = StreamingAudio.Lookup(Rando.GetRandomItem(GameplaySongs));

			MusicVoice.Stop();
			MusicVoice.Load(streamingAudioData);
			MusicVoice.Play();
		}
	}

	private void PlayStaticSound(
		AudioBuffer sound,
		float volume,
		float pitch,
		float pan
	) {
		var voice = AudioDevice.Obtain<TransientVoice>(sound.Format);
		voice.SetVolume(volume);
		voice.SetPitch(pitch);
		voice.SetPan(pan);
		voice.Submit(sound);
		voice.Play();
	}
}
