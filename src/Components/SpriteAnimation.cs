using MoonWorks.Math;
using MoonWorks.Math.Fixed;
using Float = MoonWorks.Math.Float;
using GGJ2024.Data;

namespace GGJ2024.Components;

public struct SpriteAnimation
{
	public SpriteAnimationInfoID SpriteAnimationInfoID { get; }
	public int FrameRate { get; }
	public bool Loop { get; }
	public Float.Vector2 Origin { get; }
	public Fix64 RawFrameIndex { get; }

	// FIXME: should we cache this?
	public int FrameIndex
	{
		get
		{
			var integerIndex = (int) (Fix64.Sign(RawFrameIndex) * Fix64.Ceiling(Fix64.Abs((RawFrameIndex))));
			var framesLength = SpriteAnimationInfo.Frames.Length;
			if (Loop)
			{
				return ((integerIndex % framesLength) + framesLength) % framesLength;
			}
			else
			{
				return MathHelper.Clamp(integerIndex, 0, SpriteAnimationInfo.Frames.Length - 1);
			}
		}
	}

	public SpriteAnimationInfo SpriteAnimationInfo => SpriteAnimationInfo.FromID(SpriteAnimationInfoID);
	public Sprite CurrentSprite => SpriteAnimationInfo.Frames[FrameIndex];
	public bool Finished => !Loop && FrameRate != 0 && RawFrameIndex >= SpriteAnimationInfo.Frames.Length - 1;
	public Fix64 TotalTime => Fix64.FromFraction(SpriteAnimationInfo.Frames.Length, FrameRate);

	public int TimeOf(int frame)
	{
		return frame / FrameRate;
	}

	// FIXME: this isn't really necessary
	public static SpriteAnimation ForceFrame(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameIndex
	) {
		return new SpriteAnimation(
			spriteAnimationInfo,
			0,
			false,
			frameIndex
		);
	}

	public SpriteAnimation ChangeFramerate(int frameRate)
	{
		return new SpriteAnimation(
			SpriteAnimationInfo,
			frameRate,
			Loop,
			RawFrameIndex,
			Origin);
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = true;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = Fix64.Zero;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		Float.Vector2 origin
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = false;
		Origin = origin;
		RawFrameIndex = Fix64.Zero;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		bool loop
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = loop;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = Fix64.Zero;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = true;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = Fix64.Zero;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = Fix64.Zero;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		int frameIndex
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = new Fix64(frameIndex);
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		Fix64 rawFrameIndex
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Float.Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = rawFrameIndex;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		Fix64 rawFrameIndex,
		Float.Vector2 origin
	) {
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = origin;
		RawFrameIndex = rawFrameIndex;
	}

	public SpriteAnimation Update(Fix64 dt)
	{
		return new SpriteAnimation(
			SpriteAnimationInfo,
			FrameRate,
			Loop,
			RawFrameIndex + (FrameRate * dt),
			Origin
		);
	}
}
