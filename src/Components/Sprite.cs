using MoonWorks.Graphics;
using MoonWorks.Math.Float;
using RollAndCash.Data;

namespace RollAndCash.Components;

public struct Sprite
{
	public TexturePageID TexturePageID { get; }
	public Rect SliceRect { get; } // the pixel slice on the texture page
	public Rect FrameRect { get; } // offset and original width to reproduce the transparency
	public UV UV { get; }

	public TexturePage TexturePage => TexturePage.FromID(TexturePageID);
	public Texture Texture => TexturePage.Texture;

	public Sprite(
		TexturePage texturePage,
		Rect sliceRect,
		Rect frameRect
	)
	{
		TexturePageID = texturePage.ID;
		SliceRect = sliceRect;
		FrameRect = frameRect;
		UV = new UV(
			new Vector2((float)sliceRect.X / texturePage.Width, (float)sliceRect.Y / texturePage.Height),
			new Vector2((float)sliceRect.W / texturePage.Width, (float)sliceRect.H / texturePage.Height)
		);
	}

	public Sprite Slice(int left, int top, int width, int height)
	{
		var sliceRect = new Rect
		{
			X = SliceRect.X + left + FrameRect.X,
			Y = SliceRect.Y + top + FrameRect.Y,
			W = width,
			H = height
		};

		// dont want slice to exceed the sprite itself
		if (sliceRect.X + width > SliceRect.X + SliceRect.W)
		{
			sliceRect.W -= (sliceRect.X + width) - (SliceRect.X + SliceRect.W);
		}

		if (sliceRect.Y + height > SliceRect.Y + SliceRect.H)
		{
			sliceRect.H -= (sliceRect.Y + height) - (SliceRect.Y + SliceRect.H);
		}

		var frameRect = new Rect
		{
			X = 0,
			Y = 0,
			W = width,
			H = height
		};

		return new Sprite(TexturePage, sliceRect, frameRect);
	}
}
