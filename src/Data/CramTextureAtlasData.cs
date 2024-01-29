using System.Collections.Generic;
using System.IO;

namespace RollAndCash.Data;

public struct CramTextureAtlasFile
{
	public FileInfo File { get; }
	public CramTextureAtlasData Data { get; }

	public CramTextureAtlasFile(FileInfo file, CramTextureAtlasData data)
	{
		File = file;
		Data = data;
	}
}

public struct CramTextureAtlasData
{
	// these are the fields generated by Cram
	public string Name { get; set; }
	public int Width { get; set; }
	public int Height { get; set; }
	public CramTextureAtlasImageData[] Images { get; set; }

	// some extra data, just for us
	public Dictionary<string, CramTextureAtlasAnimationData> Animations { get; set; }
}

public struct CramTextureAtlasImageData
{
	public string Name { get; set; }
	public int X { get; set; }
	public int Y { get; set; }
	public int W { get; set; }
	public int H { get; set; }
	public int TrimOffsetX { get; set; }
	public int TrimOffsetY { get; set; }
	public int UntrimmedWidth { get; set; }
	public int UntrimmedHeight { get; set; }
}

public struct CramTextureAtlasAnimationData
{
	public string[] Frames { get; set; }
	public int FrameRate { get; set; }
	public int XOrigin { get; set; }
	public int YOrigin { get; set; }
}
