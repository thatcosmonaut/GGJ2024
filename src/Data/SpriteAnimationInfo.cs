using System.Collections.Generic;
using GGJ2024.Components;

namespace GGJ2024.Data;

public readonly record struct SpriteAnimationInfoID(int ID);

public class SpriteAnimationInfo
{
	// FIXME: maaaaybe this shouldn't be static
	static List<SpriteAnimationInfo> IDLookup = new List<SpriteAnimationInfo>();
	public readonly SpriteAnimationInfoID ID;

	public string Name { get; }
	public Sprite[] Frames { get; }
	public int FrameRate { get; }
	public int OriginX { get; }
	public int OriginY { get; }

	public static SpriteAnimationInfo FromID(SpriteAnimationInfoID id)
	{
		return IDLookup[id.ID];
	}

	public SpriteAnimationInfo(
		string name,
		Sprite[] frames,
		int frameRate,
		int originX,
		int originY
	)
	{
		Name = name;
		Frames = frames;
		FrameRate = frameRate;
		OriginX = originX;
		OriginY = originY;

		lock(IDLookup)
		{
			ID = new SpriteAnimationInfoID(IDLookup.Count);
			IDLookup.Add(this);
		}
	}
}
