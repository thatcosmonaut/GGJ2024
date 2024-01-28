using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using MoonWorks;
using MoonWorks.Graphics;
using GGJ2024.Components;

namespace GGJ2024.Data;

public readonly record struct TexturePageID(int ID);

public class TexturePage
{
	static List<TexturePage> IDLookup = new List<TexturePage>();

	public readonly TexturePageID ID;
	CramTextureAtlasFile AtlasFile { get; }
	public Texture Texture { get; private set; } = null;
	public bool Loaded => Texture != null;
	public uint Width => (uint) AtlasFile.Data.Width;
	public uint Height => (uint) AtlasFile.Data.Height;

	private Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
	private Dictionary<string, SpriteAnimationInfo> animationInfos = new Dictionary<string, SpriteAnimationInfo>();

	public static TexturePage FromID(TexturePageID id)
	{
		return IDLookup[id.ID];
	}

	public TexturePage(CramTextureAtlasFile file)
	{
		lock (IDLookup)
		{
			ID = new TexturePageID(IDLookup.Count);
			IDLookup.Add(this);
		}

		AtlasFile = file;
		foreach (var image in AtlasFile.Data.Images)
		{
			AddSprite(image);
		}

		foreach (var (name, spriteAnimation) in AtlasFile.Data.Animations)
		{
			var frames = new List<Sprite>();

			foreach (var frame in spriteAnimation.Frames)
			{
				frames.Add(GetSprite(frame));
			}

			var spriteAnimationInfo = new SpriteAnimationInfo(
				name,
				frames.ToArray(),
				spriteAnimation.FrameRate,
				spriteAnimation.XOrigin,
				spriteAnimation.YOrigin
			);

			animationInfos.Add(name, spriteAnimationInfo);
		}
	}

	public void Load(GraphicsDevice graphicsDevice, CommandBuffer commandBuffer)
	{
		if (Loaded)
		{
			Logger.LogWarn("Texture already loaded!");
		}

		var atlasData = AtlasFile.Data;

		var compressedLocation = Path.Combine(AtlasFile.File.DirectoryName, atlasData.Name + ".ctex");
		if (File.Exists(compressedLocation))
		{
			// this is a compressed DDS file, unpack and load
			using (var compressedFileStream = File.Open(compressedLocation, FileMode.Open, FileAccess.Read))
			using (var decompressor = new DeflateStream(compressedFileStream, CompressionMode.Decompress))
			{
				Texture = Texture.LoadDDS(
					graphicsDevice,
					commandBuffer,
					decompressor
				);
			}
		}
		else
		{
			Texture = Texture.FromImageFile(
				graphicsDevice,
				commandBuffer,
				Path.Combine(AtlasFile.File.DirectoryName, atlasData.Name + ".png")
			);
		}
	}

	private void Unload()
	{
		Texture.Dispose();
		Texture = null;
	}

	private void AddSprite(CramTextureAtlasImageData imageData)
	{
		var sliceRect = new Rect
		{
			X = imageData.X,
			Y = imageData.Y,
			W = imageData.W,
			H = imageData.H
		};
		var frameRect = new Rect
		{
			X = imageData.TrimOffsetX,
			Y = imageData.TrimOffsetY,
			W = imageData.UntrimmedWidth,
			H = imageData.UntrimmedHeight
		};
		var sprite = new Sprite(this, sliceRect, frameRect);

		sprites.Add(imageData.Name, sprite);
	}

	public SpriteAnimationInfo GetSpriteAnimationInfo(string name)
	{
		return animationInfos[name];
	}

	public Sprite GetSprite(string name)
	{
		return sprites[name];
	}
}