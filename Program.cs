using System.Diagnostics;

namespace Oggify;

class Program
{
    static void Main(string[] args)
    {
        
        Console.WriteLine("Oggify - Convert WAV (or MP3) files to OGG format using FFmpeg");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("This tool converts all WAV or MP3 files in a specified directory to OGG format using FFmpeg.");
        Console.WriteLine("Usage: Oggify.exe <input_directory> [--overwrite] [--skip-existing] [--recursive] [--replace] [--from-mp3]");
        Console.WriteLine("Alternatively: ffmpeg <path_to_ffmpeg.exe> to change the FFmpeg path.");
        Console.WriteLine();

        var ffmpegPath = ResolveFfmpegPath(args);

        var (inputDirectory, forceOverwrite, 
            skipIfExists, recursive, 
            deleteWavAfterConversion, inputExtension) = ParseInput(args, ref ffmpegPath);

        ConvertWavOrMp3ToOgg(inputDirectory, forceOverwrite, 
            skipIfExists, recursive, 
            deleteWavAfterConversion, ffmpegPath,
            inputExtension);

        Console.WriteLine("All files converted.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    static string ResolveFfmpegPath(string[] args)
    {
        
        string? ffmpegPath = null;
        var configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_path.txt");
        
        // 1. Override via CLI args
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].Equals("ffmpeg", StringComparison.CurrentCultureIgnoreCase)) continue;
            var possiblePath = args[i + 1].Trim('"');
            if (File.Exists(possiblePath))
            {
                ffmpegPath = possiblePath;
                File.WriteAllText(configFile, ffmpegPath);
                Console.WriteLine($"[ffmpeg path updated] -> {ffmpegPath}");
                break;
            }
            else
            {
                Console.WriteLine($"Provided ffmpeg path '{possiblePath}' does not exist.");
                Environment.Exit(1);
            }
        }

// 2. Load from config if not already set
        if (ffmpegPath == null && File.Exists(configFile))
        {
            var savedPath = File.ReadAllText(configFile).Trim();
            if (File.Exists(savedPath))
            {
                ffmpegPath = savedPath;
            }
            else
            {
                Console.WriteLine("Saved ffmpeg path is invalid.");
            }
        }

        // 3. Ask user if still not resolved
        while (ffmpegPath == null || !File.Exists(ffmpegPath))
        {
            Console.Write("Enter path to ffmpeg.exe: ");
            var userInput = Console.ReadLine()?.Trim('"');
            if (File.Exists(userInput))
            {
                Console.WriteLine($"[ffmpeg path set] -> {userInput}");
                ffmpegPath = userInput;
                File.WriteAllText(configFile, ffmpegPath);
            }
            else
            {
                Console.WriteLine("Invalid path. Try again.");
            }
        }
        
        return ffmpegPath;
    }

    static (string inputDirectory, bool forceOverwrite, 
        bool skipIfExists, bool recursive, 
        bool deleteWavAfterConversion, string inputExtension) 
        ParseInput(string[] args, ref string ffmpegPath)
    {
        while (true)
        {
            string[] inputArgs;

            if (args.Length == 0)
            {
                Console.Write("Enter input folder and optional flags: ");
                var inputLine = Console.ReadLine()?.Trim() ?? "";
                inputArgs = inputLine.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            }
            else
            {
                inputArgs = args;
            }

            if (inputArgs.Length == 0)
            {
                Console.WriteLine("No input provided.");
                args = Array.Empty<string>(); // reset for next loop
                continue;
            }

            var inputDirectory = inputArgs[0].Trim('"');
            var forceOverwrite = false;
            var skipIfExists = false;
            var recursive = false;
            var deleteWavAfterConversion = false;
            var inputExtension = "*.wav";

            var invalidArgs = false;

            if (inputArgs[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: Oggify.exe <input_directory>");
                Console.WriteLine("Options:");
                Console.WriteLine("  --overwrite       Force overwrite existing OGG files.");
                Console.WriteLine("  --skip-existing   Skip conversion for files that already exist.");
                Console.WriteLine("  --recursive       Process files in subdirectories.");
                Console.WriteLine("  --replace         Delete the original WAV files after conversion.");
                Console.WriteLine("  --from-mp3        Convert MP3 files instead of WAV files.");
                Console.WriteLine("  ffmpeg <path_to_ffmpeg.exe> to change the FFmpeg path.");
                args = Array.Empty<string>(); // reset for next loop
                continue;
            }
            
            if (inputArgs[0].Equals("ffmpeg", StringComparison.OrdinalIgnoreCase))
            {
                if (inputArgs.Length < 2)
                {
                    Console.WriteLine("Usage: ffmpeg <path_to_ffmpeg.exe>");
                    args = Array.Empty<string>();
                    continue;
                }

                var possiblePath = inputArgs[1].Trim('"');

                if (File.Exists(possiblePath))
                {
                    ffmpegPath = possiblePath;
                    File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_path.txt"), ffmpegPath);
                    Console.WriteLine($"[ffmpeg path updated] -> {ffmpegPath}");
                }
                else
                {
                    Console.WriteLine($"Provided ffmpeg path '{possiblePath}' does not exist.");
                }

                args = Array.Empty<string>();
                continue;
            }

            for (int i = 1; i < inputArgs.Length; i++)
            {
                switch (inputArgs[i].ToLower())
                {
                    case "--overwrite":
                        forceOverwrite = true;
                        break;
                    case "--skip-existing":
                        skipIfExists = true;
                        break;
                    case "--recursive":
                        recursive = true;
                        break;
                    case "--replace":
                        deleteWavAfterConversion = true;
                        break;
                    case "--from-mp3":
                        inputExtension = "*.mp3";
                        break;
                    default:
                        Console.WriteLine($"Unknown argument: {inputArgs[i]}");
                        invalidArgs = true;
                        break;
                }
            }
            
            if (invalidArgs)
            {
                Console.WriteLine("Use --help for usage.");
                args = Array.Empty<string>(); // reset for next loop
                continue;
            }
            
            if (skipIfExists && forceOverwrite)
            {
                Console.WriteLine("Cannot use both --overwrite and --skip-existing flags together. Please choose one.");
                args = Array.Empty<string>(); // trigger retry
                continue;
            }

            if (Directory.Exists(inputDirectory))
                return (inputDirectory, forceOverwrite, skipIfExists, recursive, deleteWavAfterConversion, inputExtension);
            Console.WriteLine($"The directory '{inputDirectory}' does not exist.");
            args = Array.Empty<string>(); // trigger retry
        }
    }

    static void ConvertWavOrMp3ToOgg(string inputDirectory, bool forceOverwrite, bool skipIfExists, 
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

    static bool AskUserConfirmation(string message)
    {
        while (true)
        {
            Console.WriteLine(message);
            var key = Console.ReadKey();
            Console.WriteLine();

            switch (key.KeyChar)
            {
                case 'y' or 'Y':
                    return true;
                case 'n' or 'N':
                    return false;
                default:
                    Console.WriteLine("Please enter 'y' or 'n'.");
                    break;
            }
        }
    }
}