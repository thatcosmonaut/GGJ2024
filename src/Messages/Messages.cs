using GGJ2024.Content;
using GGJ2024.Systems;
using MoonWorks.Audio;

namespace GGJ2024.Messages;

public readonly record struct Action(float Value, Actions ActionType, ActionState ActionState, int index);

public readonly record struct PlayStaticSoundMessage(
	StaticSoundID StaticSoundID,
	float Volume = 1,
	float Pitch = 0,
	float Pan = 0
) {
	public AudioBuffer Sound => StaticAudio.Lookup(StaticSoundID);
}
