using System;
using RollAndCash.Content;
using RollAndCash.Messages;
using RollAndCash.Utility;
using MoonTools.ECS;
using MoonWorks.Audio;
using RollAndCash.Data;

namespace RollAndCash.Systems;

public class Audio : MoonTools.ECS.System
{
	AudioDevice AudioDevice;

	StreamingVoice MusicVoice;

	StreamingSoundID[] GameplaySongs;

	PersistentVoice DroneVoice;

	public Audio(World world, AudioDevice audioDevice) : base(world)
	{
		AudioDevice = audioDevice;

		GameplaySongs =
		[
			StreamingAudio.attentiontwerkers,
			StreamingAudio.attention_shoppers_v1,
			StreamingAudio.attention_shoppers_v2,
		];

		var streamingAudioData = StreamingAudio.Lookup(StreamingAudio.attention_shoppers_v1);
		MusicVoice = AudioDevice.Obtain<StreamingVoice>(streamingAudioData.Format);
		MusicVoice.SetVolume(0.5f);

		DroneVoice = AudioDevice.Obtain<PersistentVoice>(StaticAudio.Lookup(StaticAudio.Drone1).Format);
		DroneVoice.SetVolume(0.5f);
	}

	public override void Update(TimeSpan delta)
	{
		foreach (var staticSoundMessage in ReadMessages<PlayStaticSoundMessage>())
		{
			PlayStaticSound(
				staticSoundMessage.Sound,
				staticSoundMessage.Volume,
				staticSoundMessage.Pitch,
				staticSoundMessage.Pan,
				staticSoundMessage.Category
			);
		}

		if (SomeMessage<PlaySongMessage>())
		{
			var streamingAudioData = StreamingAudio.Lookup(Rando.GetRandomItem(GameplaySongs));
			streamingAudioData.Seek(0);
			MusicVoice.Load(streamingAudioData);
			MusicVoice.Play();
		}

		if (SomeMessage<StopDroneSounds>())
		{
			DroneVoice.Stop();
		}
	}

	public void Cleanup()
	{
		MusicVoice.Unload();
		MusicVoice.Stop();
		MusicVoice.Dispose();

		DroneVoice.Stop();
		DroneVoice.Dispose();
	}

	private void PlayStaticSound(
		AudioBuffer sound,
		float volume,
		float pitch,
		float pan,
		SoundCategory soundCategory
	)
	{
		SourceVoice voice;
		if (soundCategory == SoundCategory.Drone)
		{
			voice = DroneVoice;
			voice.Stop(); // drones should interrupt their own lines
		}
		else
		{
			voice = AudioDevice.Obtain<TransientVoice>(sound.Format);
		}

		voice.SetVolume(volume);
		voice.SetPitch(pitch);
		voice.SetPan(pan);
		voice.Submit(sound);
		voice.Play();
	}
}
