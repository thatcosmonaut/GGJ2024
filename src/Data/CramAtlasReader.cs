using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

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

	public static TexturePage ReadTextureAtlas(string path)
	{
		var data = (CramTextureAtlasData)JsonSerializer.Deserialize(File.ReadAllText(path), typeof(CramTextureAtlasData), context);
		return new TexturePage(new CramTextureAtlasFile(new FileInfo(path), data));
	}
}
