using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using CommandLine;
using JetBrains.Annotations;

namespace Oggify;

[UsedImplicitly]
internal class Program
{
    
    [Verb("convert", isDefault: true, HelpText = "Convert WAV or MP3 files to OGG format.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [UsedImplicitly]
    private class ConvertOptions
    {
        [Option("overwrite", HelpText = "Force overwrite existing OGG files.", SetName = "overwrite")]
        public bool Overwrite { get; set; }

        [Option("skip-existing", HelpText = "Skip conversion for files that already exist.", SetName = "overwrite")]
        public bool SkipExisting { get; set; }

        [Option("recursive", HelpText = "Process files in subdirectories.")]
        public bool Recursive { get; set; }

        [Option("replace", HelpText = "Delete the original WAV or MP3 files after conversion.")]
        public bool Replace { get; set; }

        [Option("from-mp3", HelpText = "Convert MP3 files instead of WAV files.")]
        public bool FromMp3 { get; set; }

        [Option("ffmpeg", HelpText = "Path to ffmpeg.exe.")]
        public string? FfmpegPath { get; set; }

        [Value(0, MetaName = "input_directory", HelpText = "Input directory containing WAV or MP3 files.")]
        public string? InputDirectory { get; set; }
    }

    [Verb("ffmpeg", HelpText = "Set or show the path to ffmpeg.exe.")]
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
    [UsedImplicitly]
    private class FfmpegOptions
    {
        [Value(0, MetaName = "ffmpeg_path", HelpText = "Path to ffmpeg.exe (leave empty to show current).")]
        public string? FfmpegPath { get; set; }
    }

    private static int Main(string[] args)
    {
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine(
            "This tool converts all WAV or MP3 files in a specified directory to OGG format using FFmpeg.");
        Console.WriteLine("Usage:");
        Console.WriteLine(
            "  Oggify.exe <input_directory> [--overwrite] [--skip-existing] [--recursive] [--replace] [--from-mp3]");
        Console.WriteLine("  Oggify.exe ffmpeg [<path_to_ffmpeg.exe>]");
        Console.WriteLine();

        // If no command-line args, start command prompt loop.
        // Otherwise, parse command-line args as a one-off command.
        if (args.Length != 0) return ParseAndExecute(args);
        InteractiveCommandLoop();
        return 0;
    }

    private static void InteractiveCommandLoop()
    {
        while (true)
        {
            Console.Write("Oggify> ");
            var input = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(input))
                continue;

            if (input.Trim().Equals("exit", StringComparison.OrdinalIgnoreCase) ||
                input.Trim().Equals("quit", StringComparison.OrdinalIgnoreCase))
                break;

            // Split input into args (simple split, does not handle quotes)
            var args = ParseArgs(input);

            if (args.Length == 0)
                continue;

            var exitCode = ParseAndExecute(args);
            if (exitCode != 0)
            {
                Console.WriteLine($"Command exited with code {exitCode}.");
            }
        }
    }

    static int ParseAndExecute(string[] args)
    {
        return Parser.Default.ParseArguments<ConvertOptions, FfmpegOptions>(args)
            .MapResult(
                (ConvertOptions opts) => RunConvert(opts),
                (FfmpegOptions opts) => RunFfmpeg(opts),
                _ =>
                {
                    // If no verb matched, show usage.
                    Console.WriteLine(
                        "Type 'convert <input_directory> [options]' to convert files, or 'ffmpeg [<ffmpeg_path>]' to manage ffmpeg path.\n" +
                        "Type 'exit' to quit."
                    );
                    return 1;
                });
    }

    private static int RunConvert(ConvertOptions opts)
    {
        while (string.IsNullOrWhiteSpace(opts.InputDirectory))
        {
            Console.Write("Enter input directory: ");
            var dir = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(dir))
                opts.InputDirectory = dir.Trim();
        }

        if (opts is { SkipExisting: true, Overwrite: true })
        {
            Console.WriteLine("Cannot use both --overwrite and --skip-existing flags together. Please choose one.");
            return 1;
        }

        if (!Directory.Exists(opts.InputDirectory))
        {
            Console.WriteLine($"The directory '{opts.InputDirectory}' does not exist.");
            return 1;
        }

        var ffmpegPath = ResolveFfmpegPath(opts.FfmpegPath);

        var inputExtension = opts.FromMp3 ? "*.mp3" : "*.wav";

        ConvertWavOrMp3ToOgg(
            opts.InputDirectory,
            opts.Overwrite,
            opts.SkipExisting,
            opts.Recursive,
            opts.Replace,
            ffmpegPath,
            inputExtension
        );

        Console.WriteLine("All files converted.");
        return 0;
    }

    private static int RunFfmpeg(FfmpegOptions opts)
    {
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_path.txt");
        if (!string.IsNullOrWhiteSpace(opts.FfmpegPath))
        {
            if (File.Exists(opts.FfmpegPath))
            {
                File.WriteAllText(configFile, opts.FfmpegPath);
                Console.WriteLine($"[ffmpeg path updated] -> {opts.FfmpegPath}");
                return 0;
            }

            Console.WriteLine($"Provided ffmpeg path '{opts.FfmpegPath}' does not exist.");
            return 1;
        }

        Console.WriteLine(File.Exists(configFile)
            ? $"Current ffmpeg path: {File.ReadAllText(configFile)}"
            : "No ffmpeg path is set.");
        return 0;
    }

    private static string ResolveFfmpegPath(string? cliFfmpegPath)
    {
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_path.txt");

        // 1. CLI override
        if (!string.IsNullOrEmpty(cliFfmpegPath) && File.Exists(cliFfmpegPath))
        {
            File.WriteAllText(configFile, cliFfmpegPath);
            Console.WriteLine($"[ffmpeg path updated] -> {cliFfmpegPath}");
            return cliFfmpegPath;
        }

        // 2. Load from config
        if (File.Exists(configFile))
        {
            var savedPath = File.ReadAllText(configFile).Trim();
            if (File.Exists(savedPath))
            {
                return savedPath;
            }

            Console.WriteLine("Saved ffmpeg path is invalid.");
        }

        // 3. Ask user
        while (true)
        {
            Console.Write("Enter path to ffmpeg.exe: ");
            var userInput = Console.ReadLine()?.Trim('"');
            if (!string.IsNullOrEmpty(userInput) && File.Exists(userInput))
            {
                File.WriteAllText(configFile, userInput);
                Console.WriteLine($"[ffmpeg path set] -> {userInput}");
                return userInput;
            }

            Console.WriteLine("Invalid path. Try again.");
        }
    }

    private static void ConvertWavOrMp3ToOgg(string inputDirectory, bool forceOverwrite, bool skipIfExists,
        bool recursive, bool deleteWavAfterConversion, string ffmpegPath, string inputExtension)
    {
        if (!File.Exists(ffmpegPath))
        {
            Console.WriteLine($"FFmpeg not found at path: {ffmpegPath}");
            return;
        }

        var files = Directory.GetFiles(inputDirectory, inputExtension,
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            Console.WriteLine($"No WAV/MP3 files found in {inputDirectory}.");
            Console.WriteLine(!recursive
                ? "(Consider using the --recursive flag to search subdirectories.)"
                : "(Including subdirectories).");
            return;
        }

        foreach (var inputFile in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var outputFile = Path.Combine(Path.GetDirectoryName(inputFile)!, $"{fileName}.ogg");

            if (File.Exists(outputFile))
            {
                if (skipIfExists)
                {
                    Console.WriteLine($"Skipping {inputFile} (already exists).");
                    continue;
                }

                if (!forceOverwrite)
                {
                    var shouldOverwrite = AskUserConfirmation($"File '{outputFile}' already exists. Overwrite? (y/n)");
                    if (!shouldOverwrite)
                    {
                        Console.WriteLine($"Skipping {inputFile}.");
                        continue;
                    }
                }

                Console.WriteLine($"Overwriting {outputFile}.");
                File.Delete(outputFile);
            }

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = $"-i \"{inputFile}\" -c:a libvorbis -qscale:a 5 \"{outputFile}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            try
            {
                process.Start();
                process.WaitForExit();

                if (process.ExitCode == 0)
                {
                    Console.WriteLine($"Converted {inputFile} to {outputFile}");

                    if (deleteWavAfterConversion)
                    {
                        try
                        {
                            File.Delete(inputFile);
                            Console.WriteLine($"Deleted original audio file: {inputFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete original audio file {inputFile}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Console.WriteLine($"Failed to convert {inputFile}.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing {inputFile}: {ex.Message}");
            }
        }
    }

    private static bool AskUserConfirmation(string message)
    {
        while (true)
        {
            Console.WriteLine(message);
            var key = Console.ReadKey();
            Console.WriteLine();

            switch (key.KeyChar)
            {
                case 'y':
                case 'Y':
                    return true;
                case 'n':
                case 'N':
                    return false;
                default:
                    Console.WriteLine("Please enter 'y' or 'n'.");
                    break;
            }
        }
    }

    /// <summary>
    /// Split command-line input into arguments, handling quoted strings.
    /// </summary>
    private static string[] ParseArgs(string input)
    {
        var args = new List<string>();
        var inQuotes = false;
        var current = new System.Text.StringBuilder();

        foreach (var c in input)
        {
            if (c == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (char.IsWhiteSpace(c) && !inQuotes)
            {
                if (current.Length <= 0) continue;
                args.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }
        if (current.Length > 0)
            args.Add(current.ToString());

        return args.ToArray();
    }
}