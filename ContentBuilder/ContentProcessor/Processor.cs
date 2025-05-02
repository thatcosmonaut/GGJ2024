using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ContentProcessor
{
	public static class Processor
	{
		public static bool UpdateFontClass = false;
		public static List<string> Output = new();
		public static void WriteOutput(string str)
		{
			Console.WriteLine(str);
			Output.Add(str);
		}

		public static void ProcessShaders(DirectoryInfo sourceDir, DirectoryInfo outputDir)
		{
			WriteOutput("Processing shaders...");
			var shaderDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Shaders"));

			var shaderOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Shaders"));
			CreateOrClearDirectory(shaderOutputDir);

#if WINDOWS
			var compilerExectuable = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "shadercross.exe"));
#elif LINUX || OSX // linux
			var compilerExectuable = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "shadercross"));
#endif

			foreach (var file in shaderDir.EnumerateFiles())
			{
				var arguments = $"{file.FullName} -o {Path.Combine(shaderOutputDir.FullName, file.Name)}.spv";

				var process = new Process();
				process.StartInfo.FileName = compilerExectuable.FullName;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = false;
				process.StartInfo.RedirectStandardError = true;
				process.ErrorDataReceived += (sendingProcess, outLine) => Console.WriteLine(outLine.Data);
				process.Start();
				process.BeginErrorReadLine();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					throw new System.SystemException("Shader compilation failed!");
				}
			}
		}

		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			// Get information about the source directory
			var dir = new DirectoryInfo(sourceDir);

			// Check if the source directory exists
			if (!dir.Exists)
				throw new DirectoryNotFoundException($"Source directory not found: {dir.FullName}");

			// Cache directories before we start copying
			DirectoryInfo[] dirs = dir.GetDirectories();

			// Create the destination directory
			Directory.CreateDirectory(destinationDir);

			// Get the files in the source directory and copy to the destination directory
			foreach (FileInfo file in dir.GetFiles())
			{
				string targetFilePath = Path.Combine(destinationDir, file.Name);
				file.CopyTo(targetFilePath, true);
			}

			// If recursive and copying subdirectories, recursively call this method
			if (recursive)
			{
				foreach (DirectoryInfo subDir in dirs)
				{
					string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
					CopyDirectory(subDir.FullName, newDestinationDir, true);
				}
			}
		}

		public static void CreateOrClearDirectory(DirectoryInfo directory)
		{
			if (directory.Exists)
			{
				foreach (FileInfo file in directory.EnumerateFiles())
				{
					file.Delete();
				}
				foreach (DirectoryInfo subdirectory in directory.EnumerateDirectories())
				{
					subdirectory.Delete(true);
				}
			}
			else
			{
				directory.Create();
			}
		}

		public static void ProcessSprites(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			WriteOutput("Processing sprites into texture pages...");

			var spriteDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Sprites"));

			var textureOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Textures"));
			CreateOrClearDirectory(textureOutputDir);

			foreach (var directory in spriteDir.GetDirectories())
			{
				ProcessTexturePage(directory, textureOutputDir);
			}

			GenerateTextureAtlasesClass(outputDir, classOutputDir);
			GenerateSpriteAnimationsClass(spriteDir, outputDir, classOutputDir);
		}

		public static void ProcessTextures(DirectoryInfo sourceDir, DirectoryInfo outputDir)
		{
			WriteOutput("Processing textures...");

			var textureOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Textures"));
			if (!textureOutputDir.Exists)
			{
				CreateOrClearDirectory(textureOutputDir);
			}

			var textureDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Textures"));
			if (textureDir.Exists)
			{
				foreach (var file in textureDir.EnumerateFiles())
				{
					// Clear if existing file
					var destination = Path.Combine(textureOutputDir.FullName, file.Name);
					if (File.Exists(destination))
					{
						File.Delete(destination);
					}

					File.Copy(file.FullName, destination);
				}
			}
		}

		public static void ProcessSpriteFolder(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir, string subFolder)
		{
			var spriteDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Sprites"));
			var textureOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Textures"));

			var subdirectory = new DirectoryInfo(Path.Combine(spriteDir.FullName, subFolder));

			ProcessTexturePage(subdirectory, textureOutputDir);

			GenerateTextureAtlasesClass(outputDir, classOutputDir);
			GenerateSpriteAnimationsClass(spriteDir, outputDir, classOutputDir);
		}

		public static void ProcessLevels(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			WriteOutput("Processing levels...");

			var levelDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Levels"));
			var levelOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Levels"));
			CreateOrClearDirectory(levelOutputDir);

			GenerateLevelsClass(levelDir, levelOutputDir, classOutputDir);
		}

		record struct WaveHeaderData
		{
			public int FileLength;
			public short FormatTag;
			public short Channels;
			public int SampleRate;
			public short BitsPerSample;
			public int DataLength;
		}

		class WavePack
		{
			public WaveHeaderData HeaderData;
			public readonly List<FileInfo> Files = new List<FileInfo>();
			public readonly Dictionary<string, AudioPackEntry> Entries = new Dictionary<string, AudioPackEntry>();
		}

		record struct AudioPackEntry
		{
			public int Start; // in bytes
			public int Length; // in bytes
		}

		static WaveHeaderData ReadWaveHeader(string path)
		{
			WaveHeaderData headerData;
			var fileInfo = new FileInfo(path);
			using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read);
			using BinaryReader br = new BinaryReader(fs);

			headerData.FileLength = (int)fileInfo.Length - 8;
			fs.Position = 20;
			headerData.FormatTag = br.ReadInt16();
			fs.Position = 22;
			headerData.Channels = br.ReadInt16();
			fs.Position = 24;
			headerData.SampleRate = br.ReadInt32();
			fs.Position = 34;
			headerData.BitsPerSample = br.ReadInt16();
			fs.Position = 40;
			headerData.DataLength = br.ReadInt32();

			return headerData;
		}

		static readonly char[] RIFF_HEADER = new char[4] { 'R', 'I', 'F', 'F' };
		static readonly char[] WAVE_FMT_HEADER = new char[8] { 'W', 'A', 'V', 'E', 'f', 'm', 't', ' ' };
		static readonly char[] DATA_HEADER = new char[4] { 'd', 'a', 't', 'a' };

		static void WriteWaveHeader(string path, WaveHeaderData headerData)
		{
			using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.Write);
			using BinaryWriter bw = new BinaryWriter(fs);
			bw.Write(RIFF_HEADER);

			bw.Write(headerData.FileLength);

			bw.Write(WAVE_FMT_HEADER);

			bw.Write((int)16);

			bw.Write(headerData.FormatTag);
			bw.Write(headerData.Channels);

			bw.Write(headerData.SampleRate);

			bw.Write((int)(headerData.SampleRate * ((headerData.BitsPerSample * headerData.Channels) / 8)));

			bw.Write((short)((headerData.BitsPerSample * headerData.Channels) / 8));

			bw.Write(headerData.BitsPerSample);

			bw.Write(DATA_HEADER);
			bw.Write(headerData.DataLength);
		}

		static JsonSerializerOptions audioPackSerializerOptions = new JsonSerializerOptions
		{
			IncludeFields = true,
			WriteIndented = true
		};

		static void PackWaveFiles(FileInfo[] files, DirectoryInfo outDir)
		{
			Dictionary<(short, short, short, int), WavePack> packs = new Dictionary<(short, short, short, int), WavePack>();

			foreach (var fileInfo in files)
			{
				var header = ReadWaveHeader(fileInfo.FullName);
				var key = (formatTag: header.FormatTag, bitsPerSample: header.BitsPerSample, channels: header.Channels, sampleRate: header.SampleRate);

				if (!packs.ContainsKey(key))
				{
					var wavePack = new WavePack();
					wavePack.HeaderData = new WaveHeaderData
					{
						FileLength = 36, // 44 bytes for total header minus 8 bytes for RIFF header
						DataLength = 0,
						FormatTag = header.FormatTag,
						BitsPerSample = header.BitsPerSample,
						Channels = header.Channels,
						SampleRate = header.SampleRate
					};

					packs.Add(key, wavePack);
				}


				var pack = packs[key];

				pack.Files.Add(fileInfo);
				pack.Entries.Add(Path.GetFileNameWithoutExtension(fileInfo.FullName), new AudioPackEntry
				{
					Start = pack.HeaderData.DataLength,
					Length = header.DataLength
				});

				pack.HeaderData.FileLength += header.DataLength;
				pack.HeaderData.DataLength += header.DataLength;
			}

			var index = 0;
			foreach (var (key, pack) in packs)
			{
				var packFilePath = Path.Combine(outDir.FullName, $"pack_{index}.wav");
				var metadataFilePath = Path.Combine(outDir.FullName, $"pack_{index}.json");
				index += 1;

				WriteWaveHeader(packFilePath, pack.HeaderData);

				using FileStream fo = new FileStream(packFilePath, FileMode.Append, FileAccess.Write);
				foreach (var fileInfo in pack.Files)
				{
					var header = ReadWaveHeader(fileInfo.FullName);
					using FileStream fs = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read);
					var bytes = new byte[header.DataLength];
					fs.Position = 44;
					fs.Read(bytes, 0, header.DataLength);
					fo.Write(bytes);
				}

				File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(pack.Entries, audioPackSerializerOptions));
			}
		}

		static void ConvertStreamingAudio(FileInfo[] files, DirectoryInfo outputDir, FileInfo qoaConvExe)
		{
			foreach (var fileInfo in files)
			{
				var arguments = $"{fileInfo.FullName} {Path.Combine(outputDir.FullName, Path.GetFileNameWithoutExtension(fileInfo.Name))}.qoa";

				var process = new Process();
				process.StartInfo.FileName = qoaConvExe.FullName;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = false;
				process.StartInfo.RedirectStandardError = false;
				process.Start();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					throw new System.SystemException("QOA conversion failed!");
				}
			}
		}

		public static void ProcessAudio(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			ProcessStaticAudio(sourceDir, outputDir, classOutputDir);
			ProcessStreamingAudio(sourceDir, outputDir, classOutputDir);
			ProcessMusicStems(sourceDir, outputDir, classOutputDir);
		}

		public static void ProcessStaticAudio(DirectoryInfo sourceDir, DirectoryInfo outputDir,
			DirectoryInfo classOutputDir)
		{
			WriteOutput("Processing static audio...");
			var staticAudioDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Audio", "Static"));

			var staticAudioOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Static"));
			CreateOrClearDirectory(staticAudioOutputDir);

			PackWaveFiles(staticAudioDir.GetFiles(), staticAudioOutputDir);
			GenerateStaticAudioPacksClass(outputDir, classOutputDir);
			GenerateStaticAudioClass(staticAudioOutputDir, classOutputDir);
		}

		public static void ProcessStreamingAudio(DirectoryInfo sourceDir, DirectoryInfo outputDir,
			DirectoryInfo classOutputDir)
		{
			WriteOutput("Processing streaming audio...");
			var streamingAudioDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Audio", "Streaming"));

			var streamingAudioOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Streaming"));
			CreateOrClearDirectory(streamingAudioOutputDir);

#if WINDOWS
			var qoaExe = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "qoaconv.exe"));
#elif LINUX || OSX
			var qoaExe = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "qoaconv"));
#endif
			ConvertStreamingAudio(streamingAudioDir.GetFiles("*.flac", new EnumerationOptions { RecurseSubdirectories = true }), streamingAudioOutputDir, qoaExe);
			GenerateStreamingAudioClass(outputDir, classOutputDir);
		}

		public static void ProcessHitboxes(DirectoryInfo sourceDir, DirectoryInfo outputDir)
		{
			WriteOutput("Copying hitboxes...");

			var hitboxDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Hitbox"));

			var hitboxOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Hitbox"));
			CreateOrClearDirectory(hitboxOutputDir);

			CopyDirectory(hitboxDir.FullName, hitboxOutputDir.FullName, true);
		}

		public static void ProcessFonts(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			WriteOutput("Copying fonts...");

			var fontDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Fonts"));

			var fontOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Fonts"));
			CreateOrClearDirectory(fontOutputDir);

			foreach (var dir in Directory.GetDirectories(fontDir.FullName))
			{
				ProcessFontFolder(sourceDir, outputDir, Path.GetFileName(dir));
			}
		}

		public static void ProcessFontFolder(DirectoryInfo sourceDir, DirectoryInfo outputDir, string subFolder)
		{
			UpdateFontClass = true;

			var fontDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Fonts"));
			var fontOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Fonts"));

			var subdirectory = new DirectoryInfo(Path.Combine(fontDir.FullName, subFolder));

			ProcessFont(subdirectory, fontOutputDir);
		}

		public static void ProcessFont(DirectoryInfo fontDir, DirectoryInfo fontOutputDir)
		{
			var inputDir = fontDir.FullName;
			fontOutputDir.Create();

#if WINDOWS
			var msdfAtlasGenInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "msdf-atlas-gen.exe"));
#elif LINUX || OSX
			var msdfAtlasGenInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "msdf-atlas-gen"));
#endif

			var charsetFile = new FileInfo(Path.Combine(fontDir.FullName, "charset.txt"));

			var fontPath = Path.Combine(fontDir.FullName, fontDir.Name + ".ttf");
			if (!File.Exists(fontPath))
			{
				fontPath = Path.Combine(fontDir.FullName, fontDir.Name + ".otf");
			}
			if (!File.Exists(fontPath))
			{
				WriteOutput("Failed to find font file!");
				return;
			}

			var textureOutputPath = Path.Combine(fontOutputDir.FullName, fontDir.Name + ".png");
			var jsonOutputPath = Path.Combine(fontOutputDir.FullName, fontDir.Name + ".json");

			var arguments = $"-font {fontPath} -yorigin top -imageout {textureOutputPath} -json {jsonOutputPath}";

			var process = new Process();
			process.StartInfo.FileName = msdfAtlasGenInfo.FullName;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.CreateNoWindow = true;
			process.Start();
			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				throw new System.SystemException("Font packing failed!");
			}

			File.Copy(fontPath, Path.Combine(fontOutputDir.FullName, Path.GetFileNameWithoutExtension(fontPath) + ".font"), true);
		}

		public static void ProcessVideos(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			WriteOutput("Copying videos...");

			var videoDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Videos"));

			var videoOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Videos"));
			CreateOrClearDirectory(videoOutputDir);

#if WINDOWS
			var ffmpegExecutablePath = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "ffmpeg.exe"));
#elif LINUX
			// on linux, just assume system has ffmpeg installed
			var ffmpegExecutablePath = new FileInfo("/usr/bin/ffmpeg");
#elif OSX
			var ffmpegExecutablePath = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "ffmpeg"));
#endif
			ConvertVideos(videoDir, videoOutputDir, ffmpegExecutablePath);
			GenerateVideosClass(videoDir, videoOutputDir, classOutputDir, ffmpegExecutablePath);
		}

		public static void ProcessMusicStems(DirectoryInfo sourceDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			WriteOutput("Copying stems...");

			var stemDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Audio", "Stems"));

			var stemOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Stems"));
			CreateOrClearDirectory(stemOutputDir);

			CopyDirectory(stemDir.FullName, stemOutputDir.FullName, true);
			GenerateMusicStemsClass(outputDir, classOutputDir);
		}

		public static void HandleBuild(
			DirectoryInfo sourceDir,
			DirectoryInfo outputDir,
			DirectoryInfo classOutputDir
		)
		{
			ProcessShaders(sourceDir, outputDir);
			ProcessSprites(sourceDir, outputDir, classOutputDir);
			ProcessLevels(sourceDir, outputDir, classOutputDir);
			ProcessAudio(sourceDir, outputDir, classOutputDir);
			ProcessHitboxes(sourceDir, outputDir);
			ProcessFonts(sourceDir, outputDir, classOutputDir);
			ProcessVideos(sourceDir, outputDir, classOutputDir);
		}

		public static void ProcessTexturePage(DirectoryInfo texturePageDir, DirectoryInfo textureOutputDir)
		{
			var inputDir = texturePageDir.FullName;
			textureOutputDir.Create();

#if WINDOWS
			var cramInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "cramcli.exe"));
#elif LINUX || OSX
			var cramInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "cramcli"));
#endif

			var textureAtlasOptionsFile = new FileInfo(Path.Combine(texturePageDir.FullName, texturePageDir.Name + ".json"));
			var textureAtlasOptionsSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true, IncludeFields = true };
			var textureAtlasOptions = JsonSerializer.Deserialize<TextureAtlasOptions>(File.ReadAllText(textureAtlasOptionsFile.FullName), textureAtlasOptionsSerializerOptions);

			var textureOutputName = Path.Combine(textureOutputDir.FullName, texturePageDir.Name);

			var arguments = $"{inputDir} {textureOutputDir.FullName} {texturePageDir.Name}";
			arguments += " --padding " + textureAtlasOptions.Padding;

			if (textureAtlasOptions.Premultiply)
			{
				arguments += " --premultiply ";
			}

			WriteOutput(arguments);

			var process = new Process();
			process.StartInfo.FileName = cramInfo.FullName;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = false;
			process.StartInfo.RedirectStandardError = false;
			process.Start();
			process.WaitForExit();

			if (process.ExitCode != 0)
			{
				throw new System.SystemException("Texture packing failed!");
			}

			var textureAtlasMetadataFile = new FileInfo(textureOutputName + ".json");
			var jsonSerializerOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
			var textureAtlasData = JsonSerializer.Deserialize<CramTextureAtlasData>(File.ReadAllText(textureAtlasMetadataFile.FullName), jsonSerializerOptions);

			var animations = new Dictionary<string, CramTextureAtlasAnimationData>();

			foreach (var directory in texturePageDir.EnumerateDirectories())
			{
				//var animationMetadataPath = Path.Combine(directory.FullName, directory.Name + ".json");
				var animationMetadataPath = directory.GetFiles("*.json")[0].FullName;
				var animationMetadata = JsonSerializer.Deserialize<CramTextureAtlasAnimationData>(File.ReadAllText(animationMetadataPath), jsonSerializerOptions);

				var frameList = new List<string>();

				foreach (var imageFile in directory.EnumerateFiles("*.png").OrderBy(f => f.Name))
				{
					var spritePath = directory.Name + "/" + imageFile.Name;
					frameList.Add(spritePath);
				}

				var newAnimationMetaData = new CramTextureAtlasAnimationData
				{
					Frames = frameList.ToArray(),
					FrameRate = animationMetadata.FrameRate,
					XOrigin = animationMetadata.XOrigin,
					YOrigin = animationMetadata.YOrigin
				};

				animations.Add(directory.Name, newAnimationMetaData);
			}

			textureAtlasData.Animations = animations;

			ExportResource(textureAtlasData, textureAtlasMetadataFile);

			if (textureAtlasOptions.Compress)
			{
#if WINDOWS
				var compressionEncoderInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "bc7enc.exe"));
#elif LINUX || OSX
				var compressionEncoderInfo = new FileInfo(Path.Combine(System.AppContext.BaseDirectory, "bc7enc"));
#endif

				var compressionProcess = new Process();
				compressionProcess.StartInfo.FileName = compressionEncoderInfo.FullName;
				compressionProcess.StartInfo.Arguments = textureOutputName + ".png -o -g";
				compressionProcess.StartInfo.CreateNoWindow = true;
				compressionProcess.StartInfo.UseShellExecute = false;
				compressionProcess.StartInfo.RedirectStandardOutput = false;
				compressionProcess.StartInfo.RedirectStandardError = false;
				compressionProcess.Start();
				compressionProcess.WaitForExit();

				if (compressionProcess.ExitCode != 0)
				{
					// If you are getting this you may need to install C++ Redistributable because bc7enc.exe needs it to run
					// https://learn.microsoft.com/en-us/cpp/windows/latest-supported-vc-redist?view=msvc-170
					throw new System.SystemException("Compression failed!");
				}

				// compress the DDS file using zlib compression
				using (FileStream ddsFile = File.Open(textureOutputName + ".dds", FileMode.Open))
				using (FileStream compressedFileStream = File.Create(textureOutputName + ".ctex"))
				using (DeflateStream compressor = new DeflateStream(compressedFileStream, CompressionLevel.Optimal))
				{
					ddsFile.CopyTo(compressor);
				}

				// delete source file
				File.Delete(textureOutputName + ".dds");
			}
		}

		public static void GenerateFontsClass(DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			UpdateFontClass = false;

			var fontsDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Fonts"));
			fontsDir.Create();

			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();

			foreach (var file in fontsDir.GetFiles("*.font"))
			{
				var name = Path.GetFileNameWithoutExtension(file.Name);
				var ID = name.Replace("-", "") + "ID";
				definitionStrings.Add($"public static FontID {ID};");
				assignmentStrings.Add($"{ID} = LoadFont(graphicsDevice, titleStorage, $\"{{FontContentPath}}/{name}.font\");");
			}

			var fontsClassCode = $@"
using System.Collections.Generic;
using System.IO;
using MoonWorks.Graphics;
using MoonWorks.Graphics.Font;
using MoonWorks.Storage;

namespace RollAndCash.Content
{{
    public static class Fonts
    {{
        public static string FontContentPath = ""Content/Fonts"";

		{string.Join("\n\t\t", definitionStrings)}

        private static List<Font> FontStorage = new List<Font>();

        public static void LoadAll(GraphicsDevice graphicsDevice, TitleStorage titleStorage)
        {{
            var commandBuffer = graphicsDevice.AcquireCommandBuffer();

			{string.Join("\n\t\t\t", assignmentStrings)}

            graphicsDevice.Submit(commandBuffer);
        }}

        public static FontID LoadFont(GraphicsDevice graphicsDevice, TitleStorage titleStorage, string path)
        {{
            var index = FontStorage.Count;
            FontStorage.Add(Font.Load(graphicsDevice, titleStorage, path));
            return new FontID(index);
        }}

        public static Font FromID(FontID fontID)
        {{
            return FontStorage[fontID.ID];
        }}
    }}

    public readonly record struct FontID(int ID);
}}
			";

			classOutputDir.Create();

			var classPath = Path.Combine(classOutputDir.FullName, "Fonts.cs");
			File.WriteAllText(classPath, fontsClassCode);
		}

		private static void GenerateTextureAtlasesClass(DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			var textureDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Textures"));

			var readStrings = new List<string>();
			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();

			foreach (var file in textureDir.GetFiles("*.json"))
			{
				var name = Path.GetFileNameWithoutExtension(file.Name);
				readStrings.Add($"CramAtlasReader.ReadTextureAtlas(GraphicsDevice, {name});");
				assignmentStrings.Add($"asyncFileLoader.EnqueueCompressedImageLoad(Path.ChangeExtension({name}.JsonFilename, \".png\"), {name}.Texture);");
				definitionStrings.Add($"public static TexturePage {name} = new TexturePage(Path.Combine(TextureContentPath, \"{file.Name}\"));");
			}

			var textureAtlasesClassCode = $@"
using System.IO;
using RollAndCash.Data;
using MoonWorks.AsyncIO;
using MoonWorks.Graphics;

namespace RollAndCash.Content
{{
	public static class TextureAtlases
	{{
		public static GraphicsDevice GraphicsDevice {{ get; private set; }}
		public static string TextureContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Textures"");

		public static void Init(GraphicsDevice graphicsDevice)
		{{
			GraphicsDevice = graphicsDevice;
			{string.Join("\n\t\t\t", readStrings)}
		}}

		public static void EnqueueLoadAllImages(AsyncFileLoader asyncFileLoader)
		{{
			{string.Join("\n\t\t\t", assignmentStrings)}
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
			";

			classOutputDir.Create();

			var classPath = Path.Combine(classOutputDir.FullName, "TextureAtlases.cs");
			File.WriteAllText(classPath, textureAtlasesClassCode);
		}

		static void GenerateSpriteAnimationsClass(DirectoryInfo spriteDir, DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();
			var lookupStrings = new List<string>();

			foreach (var textureGroupDir in spriteDir.GetDirectories())
			{
				var textureName = textureGroupDir.Name;

				foreach (var spriteAnimationDir in textureGroupDir.GetDirectories())
				{
					var spriteAnimationName = spriteAnimationDir.Name;

					var definitionString = $"public static SpriteAnimationInfo {Path.GetFileNameWithoutExtension(spriteAnimationName)};";
					definitionStrings.Add(definitionString);

					var assignmentString = $"{Path.GetFileNameWithoutExtension(spriteAnimationName)} = TextureAtlases.{textureName}.GetSpriteAnimationInfo(\"{spriteAnimationName}\");";
					assignmentStrings.Add(assignmentString);

					var lookupString = $"{{ \"{spriteAnimationName}\", {spriteAnimationName} }}";
					lookupStrings.Add(lookupString);
				}
			}

			var spriteAnimationsClassCode = $@"
using System.Collections.Generic;
using RollAndCash.Data;

namespace RollAndCash.Content
{{
	public static class SpriteAnimations
	{{
		public static bool Loaded = false;
		private static Dictionary<string, SpriteAnimationInfo> lookup;

		public static SpriteAnimationInfo Lookup(string name)
		{{
			return lookup[name];
		}}

		public static IEnumerable<string> Names => lookup.Keys;

		public static void LoadAll()
		{{
			{string.Join("\n\t\t\t", assignmentStrings)}
			lookup = new Dictionary<string, SpriteAnimationInfo>
			{{
				{string.Join(",\n\t\t\t\t", lookupStrings)}
			}};
			Loaded = true;
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
			";

			classOutputDir.Create();

			var classPath = Path.Combine(classOutputDir.FullName, "SpriteAnimations.cs");
			File.WriteAllText(classPath, spriteAnimationsClassCode);
		}

		static void GenerateLevelsClass(DirectoryInfo levelDir, DirectoryInfo levelOutputDir, DirectoryInfo classOutputDir)
		{
			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();
			var versusYieldStrings = new List<string>();
			var adventureYieldStrings = new List<string>();
			var showdownYieldStrings = new List<string>();

			var versusLevelDir = new DirectoryInfo(Path.Combine(levelDir.FullName, "Versus"));

			foreach (var file in versusLevelDir.EnumerateFiles())
			{
				File.Copy(file.FullName, Path.Combine(levelOutputDir.FullName, file.Name), true);

				definitionStrings.Add($"public static Level {Path.GetFileNameWithoutExtension(file.Name)};");
				assignmentStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)} = Level.FromImportLevel(Path.Combine(LevelContentPath, \"{file.Name}\"));");

				versusYieldStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)}");
			}

			var adventureLevelDir = new DirectoryInfo(Path.Combine(levelDir.FullName, "Adventure"));

			foreach (var file in adventureLevelDir.EnumerateFiles())
			{
				File.Copy(file.FullName, Path.Combine(levelOutputDir.FullName, file.Name), true);

				definitionStrings.Add($"public static Level {Path.GetFileNameWithoutExtension(file.Name)};");
				assignmentStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)} = Level.FromImportLevel(Path.Combine(LevelContentPath, \"{file.Name}\"));");

				adventureYieldStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)}");
			}

			var showdownLevelDir = new DirectoryInfo(Path.Combine(levelDir.FullName, "Showdown"));

			foreach (var file in showdownLevelDir.EnumerateFiles())
			{
				File.Copy(file.FullName, Path.Combine(levelOutputDir.FullName, file.Name), true);

				definitionStrings.Add($"public static Level {Path.GetFileNameWithoutExtension(file.Name)};");
				assignmentStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)} = Level.FromImportLevel(Path.Combine(LevelContentPath, \"{file.Name}\"));");

				showdownYieldStrings.Add($"{Path.GetFileNameWithoutExtension(file.Name)}");
			}

			var textureAtlasesClassCode = $@"
using System.IO;
using RollAndCash.Data;
using System.Collections.Generic;

namespace RollAndCash.Content
{{
	public static class Levels
	{{
		public static string LevelContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Levels"");

		public static Level[] VersusLevels;
		public static Level[] AdventureLevels;
		public static Level[] ShowdownLevels;
		public static Level[] AllLevels;

		public static bool Loaded = false;

		public static void LoadAll()
		{{
			{string.Join("\n\t\t\t", assignmentStrings)}

			VersusLevels = new Level[]
			{{
				{string.Join(",\n\t\t\t\t", versusYieldStrings)}
			}};

			AdventureLevels = new Level[]
			{{
				{string.Join(",\n\t\t\t\t", adventureYieldStrings)}
			}};

			ShowdownLevels = new Level[]
			{{
				{string.Join(",\n\t\t\t\t", showdownYieldStrings)}
			}};

			AllLevels = new Level[]
			{{
				{string.Join(",\n\t\t\t\t", versusYieldStrings)},
				{string.Join(",\n\t\t\t\t", adventureYieldStrings)},
				{string.Join(",\n\t\t\t\t", showdownYieldStrings)}
			}};

			Loaded = true;
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
			";

			classOutputDir.Create();

			var classPath = Path.Combine(classOutputDir.FullName, "Levels.cs");
			File.WriteAllText(classPath, textureAtlasesClassCode);
		}

		static void ConvertVideos(DirectoryInfo videoDir, DirectoryInfo videoOutputDir, FileInfo ffmpegInfo)
		{
			foreach (var videoFile in videoDir.EnumerateFiles("*.mp4"))
			{
				var outputPath = Path.Combine(videoOutputDir.FullName, Path.ChangeExtension(videoFile.Name, ".obu"));
				var arguments = $"-i {videoFile.FullName} -c:v libsvtav1 -preset 8 -crf 35 {outputPath}";

				var process = new Process();
				process.StartInfo.FileName = ffmpegInfo.FullName;
				process.StartInfo.Arguments = arguments;
				process.StartInfo.CreateNoWindow = true;
				process.StartInfo.UseShellExecute = false;
				process.StartInfo.RedirectStandardOutput = false;
				process.StartInfo.RedirectStandardError = false;
				process.Start();
				process.WaitForExit();

				if (process.ExitCode != 0)
				{
					throw new System.SystemException("Video conversion failed!");
				}
			}
		}

		static void GetFPSStringFromFFmpeg(string output, ref int fps)
		{
			if (output != null)
			{
				var match = Regex.Match(output, @"(\d+) fps");

				if (match.Success)
				{
					fps = int.Parse(match.Groups[1].Value);
				}
			}
		}

		static int GetFPSFromMP4(FileInfo ffmpegInfo, FileInfo videoFile)
		{
			int fps = 0;
			var arguments = $"-i {videoFile.FullName}";

			var process = new Process();
			process.StartInfo.FileName = ffmpegInfo.FullName;
			process.StartInfo.Arguments = arguments;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.RedirectStandardOutput = true;
			process.StartInfo.RedirectStandardError = true;
			process.OutputDataReceived += (s, e) => GetFPSStringFromFFmpeg(e.Data, ref fps);
			process.ErrorDataReceived += (s, e) => GetFPSStringFromFFmpeg(e.Data, ref fps);
			process.Start();
			process.BeginOutputReadLine();
			process.BeginErrorReadLine();
			process.WaitForExit();

			if (fps == 0)
			{
				throw new System.SystemException("Failed to parse FPS count!");
			}

			return fps;
		}

		static void GenerateVideosClass(DirectoryInfo videoInputDir, DirectoryInfo videoOutputDir, DirectoryInfo classOutputDir, FileInfo ffmpegInfo)
		{
			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();

			foreach (var videoFile in videoOutputDir.EnumerateFiles("*.obu"))
			{
				var name = Path.GetFileNameWithoutExtension(videoFile.Name);
				var definitionString = $"public static VideoAV1 {name};";
				definitionStrings.Add(definitionString);

				var fps = GetFPSFromMP4(ffmpegInfo, new FileInfo(Path.Combine(videoInputDir.FullName, Path.ChangeExtension(videoFile.Name, "mp4"))));

				var assignmentString = $"{name} = new VideoAV1(graphicsDevice, Path.Combine(VideoContentPath, \"{videoFile.Name}\"), {fps});";
				assignmentStrings.Add(assignmentString);
			}

			var videosClassCode = $@"
using System.IO;
using MoonWorks.Video;
using MoonWorks.Graphics;

namespace RollAndCash.Content
{{
	public static class Videos
	{{
		private static string VideoContentPath = Path.Combine(""Content"", ""Videos"");

		{string.Join("\n\t\t", definitionStrings)}

		public static void LoadAll(GraphicsDevice graphicsDevice)
		{{
			{string.Join("\n\t\t\t", assignmentStrings)}
		}}
	}}
}}
";

			classOutputDir.Create();
			var classPath = Path.Combine(classOutputDir.FullName, "Videos.cs");
			File.WriteAllText(classPath, videosClassCode);
		}

		static void GenerateStaticAudioPacksClass(DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			var staticAudioOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Static"));

			var initStrings = new List<string>();
			var loadStrings = new List<string>();
			var definitionStrings = new List<string>();

			foreach (var jsonFile in staticAudioOutputDir.EnumerateFiles("*.json"))
			{
				var name = Path.GetFileNameWithoutExtension(jsonFile.FullName);
				initStrings.Add($"{name}.Init(audioDevice, Path.Combine(StaticAudioContentPath, \"{name}.wav\"), Path.Combine(StaticAudioContentPath, \"{name}.json\"));");
				loadStrings.Add($"{name}.LoadAsync(asyncFileLoader);");
				definitionStrings.Add($"public static StaticAudioPack {name} = new StaticAudioPack();");
			}

			var staticAudioClassCode = $@"
using System.IO;
using MoonWorks.AsyncIO;
using MoonWorks.Audio;
using RollAndCash.Data;

namespace RollAndCash.Content
{{
	public static class StaticAudioPacks
	{{
		public static string StaticAudioContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Audio"", ""Static"");

		public static void Init(AudioDevice audioDevice)
		{{
			{string.Join("\n\t\t\t", initStrings)}
		}}

		public static void LoadAsync(AsyncFileLoader asyncFileLoader)
		{{
			{string.Join("\n\t\t\t", loadStrings)}
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
";

			classOutputDir.Create();
			var classPath = Path.Combine(classOutputDir.FullName, "StaticAudioPacks.cs");
			File.WriteAllText(classPath, staticAudioClassCode);
		}

		static void GenerateStaticAudioClass(
			DirectoryInfo staticAudioOutputDir,
			DirectoryInfo classOutputDir)
		{
			var definitionStrings = new List<string>();
			var assignmentStrings = new List<string>();
			var lookupStrings = new List<string>();

			var id = 0;
			foreach (var audioPackJsonFile in staticAudioOutputDir.EnumerateFiles("*.json"))
			{
				var packName = Path.GetFileNameWithoutExtension(audioPackJsonFile.Name);

				var entries = JsonSerializer.Deserialize<Dictionary<string, AudioPackEntry>>(
					File.ReadAllText(audioPackJsonFile.FullName),
					audioPackSerializerOptions);

				foreach (var (name, entry) in entries)
				{
					var definitionString = $"public static StaticSoundID {name} = new StaticSoundID({id});";
					definitionStrings.Add(definitionString);

					var lookupString = $"{{{id}, StaticAudioPacks.{packName}.GetAudioBuffer(\"{name}\") }}";
					lookupStrings.Add(lookupString);

					id += 1;
				}
			}

			var staticAudioClassCode = $@"
using System.IO;
using MoonWorks.Audio;
using System.Collections.Generic;

namespace RollAndCash.Content
{{
	public record struct StaticSoundID(int ID);

	public static class StaticAudio
	{{
		private static string StaticAudioContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Audio"", ""Static"");

		public static bool Loaded = false;

		public static Dictionary<int, AudioBuffer> IDToSound;

		public static AudioBuffer Lookup(StaticSoundID id)
		{{
			return IDToSound[id.ID];
		}}

		public static void LoadAll()
		{{
			IDToSound = new Dictionary<int, AudioBuffer>
			{{
				{string.Join(",\n\t\t\t\t", lookupStrings)}
			}};
			Loaded = true;
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
";

			classOutputDir.Create();
			var classPath = Path.Combine(classOutputDir.FullName, "StaticAudio.cs");
			File.WriteAllText(classPath, staticAudioClassCode);
		}

		static void GenerateStreamingAudioClass(DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			var streamingAudioOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Streaming"));

			var definitionStrings = new List<string>();
			var lookupStrings = new List<string>();

			var id = 0;
			foreach (var file in streamingAudioOutputDir.EnumerateFiles())
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);

				definitionStrings.Add($"public static StreamingSoundID {fileNameWithoutExtension} = new StreamingSoundID({id});");
				lookupStrings.Add($"{{{id}, new QoaFile(Path.Combine(StreamingAudioContentPath, \"{file.Name}\"), AudioDataQoa.Create(audioDevice)) }}");

				id += 1;
			}

			var streamingAudioClassCode = $@"
using System.IO;
using MoonWorks.AsyncIO;
using MoonWorks.Audio;
using System.Collections.Generic;

namespace RollAndCash.Content
{{
	public record struct StreamingSoundID(int ID);
	public record class QoaFile(string FilePath, AudioDataQoa AudioData);

	public static class StreamingAudio
	{{
		private static string StreamingAudioContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Audio"", ""Streaming"");
		private static Dictionary<int, QoaFile> IDToQoaFile;

		public static AudioDataQoa Lookup(StreamingSoundID id)
		{{
			return IDToQoaFile[id.ID].AudioData;
		}}

		public static void Init(AudioDevice audioDevice)
		{{
			IDToQoaFile = new Dictionary<int, QoaFile>
			{{
				{string.Join(",\n\t\t\t\t", lookupStrings)}
			}};
		}}

		public static void LoadAsync(AsyncFileLoader loader)
		{{
			foreach (var (id, qoaFile) in IDToQoaFile)
			{{
				loader.EnqueueQoaStreamingLoad(qoaFile.FilePath, qoaFile.AudioData);
			}}
		}}

		{string.Join("\n\t\t", definitionStrings)}
	}}
}}
			";

			classOutputDir.Create();
			var classPath = Path.Combine(classOutputDir.FullName, "StreamingAudio.cs");
			File.WriteAllText(classPath, streamingAudioClassCode);
		}

		static void GenerateMusicStemsClass(DirectoryInfo outputDir, DirectoryInfo classOutputDir)
		{
			var stemsOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Audio", "Stems"));

			var definitionStrings = new List<string>();
			var loadStrings = new List<string>();
			var allArrayStrings = new List<string>();

			foreach (var file in stemsOutputDir.EnumerateFiles())
			{
				var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name);
				definitionStrings.Add($"public static MusicStemGroup {fileNameWithoutExtension} = new MusicStemGroup(Path.Combine(MusicStemsContentPath, \"{file.Name}\"));");
				loadStrings.Add($"{fileNameWithoutExtension}.Load();");
				allArrayStrings.Add(fileNameWithoutExtension);
			}

			var musicStemsClassClode = $@"
using System.IO;

namespace RollAndCash.Content
{{
	public static class MusicStems
	{{
		public static string MusicStemsContentPath = Path.Combine(System.AppContext.BaseDirectory, ""Content"", ""Audio"", ""Stems"");

		public static bool Loaded = false;

		public static void LoadAll()
		{{
			{string.Join("\n\t\t\t", loadStrings)}
			Loaded = true;
		}}

		{string.Join("\n\t\t", definitionStrings)}

		public static MusicStemGroup[] All = new MusicStemGroup[]
		{{
			{string.Join(",\n\t\t\t", allArrayStrings)}
		}};
	}}
}}
			";

			classOutputDir.Create();
			var classPath = Path.Combine(classOutputDir.FullName, "MusicStems.cs");
			File.WriteAllText(classPath, musicStemsClassClode);
		}

		public static void CopyData(DirectoryInfo sourceDir, DirectoryInfo outputDir)
		{
			var dataDir = new DirectoryInfo(Path.Combine(sourceDir.FullName, "Data"));
			var dataOutputDir = new DirectoryInfo(Path.Combine(outputDir.FullName, "Data"));

			dataOutputDir.Create();

			foreach (var file in dataDir.GetFiles())
			{
				File.Copy(file.FullName, Path.Combine(dataOutputDir.FullName, file.Name), true);
			}
		}

		public static void ExportResource<T>(T resource, FileInfo dest)
		{
			var stream = new FileStream(dest.FullName, FileMode.Create, FileAccess.Write);
			var writer = new Utf8JsonWriter(stream, new JsonWriterOptions
			{
				Indented = true
			});
			JsonSerializer.Serialize<T>(writer, resource);
		}
	}
}
