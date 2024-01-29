using MoonWorks.Math.Float;
using RollAndCash.Data;
using System;
using MoonWorks.Math;

namespace RollAndCash.Components;

public struct SpriteAnimation
{
	public SpriteAnimationInfoID SpriteAnimationInfoID { get; }
	public int FrameRate { get; }
	public bool Loop { get; }
	public Vector2 Origin { get; }
	public float RawFrameIndex { get; }

	// FIXME: should we cache this?
	public int FrameIndex
	{
		get
		{
			var integerIndex = (int)(MathF.Sign(RawFrameIndex) * MathF.Ceiling(MathF.Abs((RawFrameIndex))));
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
	public float TotalTime => SpriteAnimationInfo.Frames.Length / FrameRate;

	public int TimeOf(int frame)
	{
		return frame / FrameRate;
	}

	// FIXME: this isn't really necessary
	public static SpriteAnimation ForceFrame(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameIndex
	)
	{
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
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = true;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = 0;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		Vector2 origin
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = false;
		Origin = origin;
		RawFrameIndex = 0;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		bool loop
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = spriteAnimationInfo.FrameRate;
		Loop = loop;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = 0;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = true;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = 0;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = 0;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		int frameIndex
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = frameIndex;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		float rawFrameIndex
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = new Vector2(spriteAnimationInfo.OriginX, spriteAnimationInfo.OriginY);
		RawFrameIndex = rawFrameIndex;
	}

	public SpriteAnimation(
		SpriteAnimationInfo spriteAnimationInfo,
		int frameRate,
		bool loop,
		float rawFrameIndex,
		Vector2 origin
	)
	{
		SpriteAnimationInfoID = spriteAnimationInfo.ID;
		FrameRate = frameRate;
		Loop = loop;
		Origin = origin;
		RawFrameIndex = rawFrameIndex;
	}

	public SpriteAnimation Update(float dt)
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
