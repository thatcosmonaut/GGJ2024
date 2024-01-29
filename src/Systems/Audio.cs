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
	StreamingVoice TitleMusicVoice;

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

		TitleMusicVoice = AudioDevice.Obtain<StreamingVoice>(streamingAudioData.Format);
		TitleMusicVoice.SetVolume(0.5f);
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

		if (SomeMessage<PlaySongMessage>())
		{
			var streamingAudioData = StreamingAudio.Lookup(Rando.GetRandomItem(GameplaySongs));

			TitleMusicVoice.Stop();
			MusicVoice.Stop();
			MusicVoice.Load(streamingAudioData);
			MusicVoice.Play();
		}

		if (SomeMessage<PlayTitleMusic>())
		{
			var streamingAudioData = StreamingAudio.Lookup(StreamingAudio.roll_n_cash_grocery_lords);

			MusicVoice.Stop();
			TitleMusicVoice.Stop();
			TitleMusicVoice.Load(streamingAudioData);
			TitleMusicVoice.Play();
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
