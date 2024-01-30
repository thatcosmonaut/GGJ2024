using System.Collections.Generic;
using System.Security.Cryptography;
using System.IO;
using System;
using System.Text.Json;
using ContentProcessor;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Dynamic;
using System.Threading;

namespace ContentBuilderUI;

public enum DirectoryType
{
    SpriteTPage,
    AudioStatic,
    AudioStreaming,
    Fonts,
    Shaders,
    Textures,
    Data
}

public class TrackedDirectory
{
    public readonly string DirectoryPath;
    public byte[] ContentHash;
    public byte[] SavedHash;
    public DirectoryType DirectoryType;
    public BuildStatus BuildStatus { get; set; } = BuildStatus.InProgress;
    public string HashPath
    {
        get
        {
            var hashFileName = Path.GetFileNameWithoutExtension(DirectoryPath) + ".hash";
            return Path.Combine(Operations.PreferencesFolderLocation, hashFileName);
        }
    }

    public TrackedDirectory(string path, DirectoryType directoryType)
    {
        DirectoryPath = path;
        DirectoryType = directoryType;
    }

    public void SaveHashToDisk()
    {
        BuildStatus = BuildStatus.Complete;

        File.WriteAllBytes(
            HashPath,
            ContentHash
        );
    }

    public void LoadHashFromDisk()
    {
        BuildStatus = BuildStatus.Comparing;

        if (File.Exists(HashPath))
        {
            SavedHash = File.ReadAllBytes(HashPath);
        }
    }

    public void CalculateContentHash()
    {
        ContentHash = Operations.GetByteHash(DirectoryPath);
    }

    public void UpdateBuildStatus()
    {
        BuildStatus = BuildStatus.OutOfDate;

        if (ContentHash != null && SavedHash != null)
        {
            var contentHashSpan = new ReadOnlySpan<byte>(ContentHash);
            var savedHashSpan = new ReadOnlySpan<byte>(SavedHash);
            BuildStatus = contentHashSpan.SequenceEqual(savedHashSpan) ? BuildStatus.Complete : BuildStatus.OutOfDate;
        }
    }
}

public enum BuildStatus
{
    OutOfDate,
    InProgress,
    Complete,
    Comparing
}

public class Preferences
{
    public string SourceContentDirectoryPath { get; set; }
    public string GameDirectoryPath { get; set; }
}

public static class Operations
{
    public static ContentGroup Sprites;
    public static ContentGroup Audio;
    public static ContentGroup Fonts;
    public static ContentGroup Other;

    public static ConcurrentQueue<TrackedDirectory> AllTrackedDirectories = new();

    public static Preferences Preferences;

    public static string PreferencesFolderLocation =
        $"{Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "RollAndCashContentBuilder")}";

    private static string PreferencesFileLocation =
        Path.Combine(
            PreferencesFolderLocation,
            "preferences.json");

    public static void Initialize()
    {
        Preferences = new Preferences();
        Directory.CreateDirectory(PreferencesFolderLocation);
        LoadPreferences();
        Sprites = new ContentGroup("Sprites");
        Audio = new ContentGroup("Audio");
        Fonts = new ContentGroup("Fonts");
        Other = new ContentGroup("Other");
    }

    public static bool ValidateGameProjectDirectory(string path)
    {
        if (!Path.Exists(path)) return false;
        if (!Directory.Exists(path)) return false;

        if (File.Exists(Path.Combine(path, "RollAndCash.sln")))
        {
            Preferences.GameDirectoryPath = path;
            InitializeTrackedDirectories();
            SavePreferences();
            return true;
        }

        return false;
    }

    public static bool ValidateSourceContentDirectory(string path)
    {
        if (path == null) return false;

        if (Directory.Exists(Path.Combine(path, "Sprites")))
        {
            Preferences.SourceContentDirectoryPath = path;
            SavePreferences();
            return true;
        }

        return false;
    }

    private static void SavePreferences()
    {
        var json = JsonSerializer.Serialize(Preferences);
        File.WriteAllText(PreferencesFileLocation, json);
    }

    private static void LoadPreferences()
    {
        if (File.Exists(PreferencesFileLocation))
        {
            var data = JsonSerializer.Deserialize<Preferences>(File.ReadAllText(PreferencesFileLocation));
            if (data != null)
            {
                Preferences = data;
            }
        }
    }

    private static void InitializeTrackedDirectories()
    {
        AllTrackedDirectories.Clear();
        Sprites.Clear();
        Audio.Clear();

        var contentDir = Preferences.SourceContentDirectoryPath;

        // Sprites
        var spriteDir = new DirectoryInfo(Path.Combine(contentDir, "Sprites"));
        foreach (var directory in spriteDir.GetDirectories())
        {
            TrackDirectory(directory.FullName, DirectoryType.SpriteTPage);
        }

        // Audio
        TrackDirectory(Path.Combine(contentDir, "Audio", "Static"), DirectoryType.AudioStatic);
        TrackDirectory(Path.Combine(contentDir, "Audio", "Streaming"), DirectoryType.AudioStreaming);

        // Fonts
        var fontDir = new DirectoryInfo(Path.Combine(contentDir, "Fonts"));
        foreach (var directory in fontDir.GetDirectories())
        {
            TrackDirectory(directory.FullName, DirectoryType.Fonts);
        }

        // Shaders
        TrackDirectory(Path.Combine(contentDir, "Shaders"), DirectoryType.Shaders);

        // Data
        TrackDirectory(Path.Combine(contentDir, "Data"), DirectoryType.Data);
    }

    private static void TrackDirectory(string path, DirectoryType directoryType)
    {
        var trackedDirectory = new TrackedDirectory(path, directoryType);
        AllTrackedDirectories.Enqueue(trackedDirectory);

        if (directoryType == DirectoryType.SpriteTPage)
        {
            Sprites.Add(trackedDirectory);
        }
        else if (
            directoryType == DirectoryType.AudioStatic ||
            directoryType == DirectoryType.AudioStreaming
        )
        {
            Audio.Add(trackedDirectory);
        }
        else if (directoryType == DirectoryType.Fonts)
        {
            Fonts.Add(trackedDirectory);
        }
        else
        {
            Other.Add(trackedDirectory);
        }

        Task.Run(() =>
        {
            trackedDirectory.LoadHashFromDisk();
            trackedDirectory.CalculateContentHash();
            trackedDirectory.UpdateBuildStatus();
        });
    }

    /// <summary>
    /// Will build any asset directory that has an outdated or missing hash
    /// </summary>
    public static void BuildOutOfDate()
    {
        // check if all directories are up to date
        var everythingUpToDate = true;
        foreach (var trackedDirectory in AllTrackedDirectories)
        {
            if (trackedDirectory.BuildStatus != BuildStatus.Complete)
            {
                everythingUpToDate = false;
                break;
            }
        }

        if (everythingUpToDate)
        {
            WriteOutput("All asset folders are up to date");
            return;
        }

        // Build
        WriteOutput("Building any outdated asset folders");

        Task.Run(() => Parallel.ForEach(AllTrackedDirectories, new ParallelOptions { MaxDegreeOfParallelism = 8 }, (trackedDirectory) =>
        {
            WriteOutput("Processing: " + trackedDirectory.DirectoryPath);
            ProcessTrackedDir(trackedDirectory);
        }));
    }

    /// <summary>
    /// This Thread is spun up for every asset directory that's getting updated
    /// </summary>
    public static void ProcessTrackedDir(TrackedDirectory trackedDirectory)
    {
        trackedDirectory.BuildStatus = BuildStatus.InProgress;

        var subFolderName = Path.GetFileNameWithoutExtension(trackedDirectory.DirectoryPath);

        var source = new DirectoryInfo(Preferences.SourceContentDirectoryPath);
        var output = new DirectoryInfo(Path.Combine(Preferences.GameDirectoryPath, "Content"));
        var classOutput =
            new DirectoryInfo(Path.Combine(Preferences.GameDirectoryPath, "src", "Generated"));

        switch (trackedDirectory.DirectoryType)
        {
            case DirectoryType.SpriteTPage:
                WriteOutput("Cramming Sprites: " + subFolderName);
                Processor.ProcessSpriteFolder(source, output, classOutput, subFolderName);
                break;

            case DirectoryType.AudioStatic:
                WriteOutput("Processing Static Audio");
                Processor.ProcessStaticAudio(source, output, classOutput);
                break;

            case DirectoryType.AudioStreaming:
                WriteOutput("Processing Streaming Audio");
                Processor.ProcessStreamingAudio(source, output, classOutput);
                break;

            case DirectoryType.Fonts:
                WriteOutput("Packing Font: " + subFolderName);
                Processor.ProcessFontFolder(source, output, subFolderName);
                break;

            case DirectoryType.Shaders:
                WriteOutput("Processing Shaders: " + subFolderName);
                Processor.ProcessShaders(source, output);
                break;

            case DirectoryType.Textures:
                Processor.ProcessTextures(source, output);
                break;

            case DirectoryType.Data:
                Processor.CopyData(source, output);
                break;

            default:
                trackedDirectory.BuildStatus = BuildStatus.OutOfDate;
                return;
        }

        // Write to .hash file in the directory we just processed
        // TODO: Hacky since this will run for every font file if generating out of date assets but shrug
        if (Processor.UpdateFontClass)
        {
            Processor.GenerateFontsClass(output, classOutput);
        }

        trackedDirectory.SaveHashToDisk();
    }

    #region Hashing

    public static byte[] GetByteHash(string directoryPath)
    {
        using var sha256 = SHA256.Create();
        var files = Directory.GetFiles(directoryPath, "*.*", SearchOption.AllDirectories);

        using var allFilesStream = new MemoryStream();
        foreach (var file in files)
        {
            using var fileStream = File.OpenRead(file);
            var fileHash = sha256.ComputeHash(fileStream);
            allFilesStream.Write(fileHash, 0, fileHash.Length);
            fileStream.Close();
        }

        allFilesStream.Position = 0;
        return sha256.ComputeHash(allFilesStream);
    }

    #endregion

    public static void WriteOutput(string str)
    {
        Console.WriteLine(str);
    }
}
