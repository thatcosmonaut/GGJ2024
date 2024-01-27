using System;
using GGJ2024.Messages;
using MoonTools.ECS;
using MoonWorks.Audio;

namespace GGJ2024.Systems;

public class Audio : MoonTools.ECS.System
{
	AudioDevice AudioDevice;

	public Audio(World world, AudioDevice audioDevice) : base(world)
	{
		AudioDevice = audioDevice;
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
