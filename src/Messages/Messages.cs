using GGJ2024.Content;
using GGJ2024.Systems;
using MoonTools.ECS;
using MoonWorks.Audio;
using GGJ2024.Components;

namespace GGJ2024.Messages;

public readonly record struct PlayStaticSoundMessage(
	StaticSoundID StaticSoundID,
	float Volume = 1,
	float Pitch = 0,
	float Pan = 0
)
{
	public AudioBuffer Sound => StaticAudio.Lookup(StaticSoundID);
}

public readonly record struct SetAnimationMessage(
	Entity Entity,
	SpriteAnimation Animation,
	bool ForceUpdate = false
);
