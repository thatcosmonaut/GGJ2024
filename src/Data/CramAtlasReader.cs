using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using MoonWorks.Graphics;

namespace RollAndCash.Data;

[JsonSerializable(typeof(CramTextureAtlasData))]
internal partial class CramTextureAtlasDataContext : JsonSerializerContext
{
}

public static class CramAtlasReader
{
	static JsonSerializerOptions options = new JsonSerializerOptions
	{
		PropertyNameCaseInsensitive = true
	};

	static CramTextureAtlasDataContext context = new CramTextureAtlasDataContext(options);

	public unsafe static void ReadTextureAtlas(GraphicsDevice graphicsDevice, TexturePage texturePage)
	{
        var data = (CramTextureAtlasData)JsonSerializer.Deserialize(File.ReadAllText(texturePage.JsonFilename), typeof(CramTextureAtlasData), context);
		texturePage.Load(graphicsDevice, data);
	}
}
