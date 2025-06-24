using System.Diagnostics;

namespace Oggify;

class Program
{
    static void Main(string[] args)
    {
        
        Console.WriteLine("Oggify - Convert WAV files to OGG format using FFmpeg");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("This tool converts all WAV files in a specified directory to OGG format using FFmpeg.");
        Console.WriteLine("Usage: Oggify.exe <input_directory> [--overwrite] [--skip-existing] [--recursive] [--replace]");
        Console.WriteLine("Alternatively: ffmpeg <path_to_ffmpeg.exe> to change the FFmpeg path.");
        Console.WriteLine();

        var ffmpegPath = ResolveFfmpegPath(args);

        var (inputDirectory, forceOverwrite, skipIfExists, recursive, deleteWavAfterConversion) = ParseInput(args);

        ConvertWavToOgg(inputDirectory, forceOverwrite, skipIfExists, recursive, deleteWavAfterConversion, ffmpegPath);

        Console.WriteLine("All files converted.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    static string ResolveFfmpegPath(string[] args)
    {
        
        string? ffmpegPath = null;
        string configFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg_path.txt");
        
        // 1. Override via CLI args
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i].ToLower() == "ffmpeg")
            {
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

    static (string inputDirectory, bool forceOverwrite, bool skipIfExists, bool recursive, bool deleteWavAfterConversion) ParseInput(string[] args)
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

            var invalidArgs = false;

            if (inputArgs[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: Oggify.exe <input_directory> [--overwrite] [--skip-existing]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --overwrite       Force overwrite existing OGG files.");
                Console.WriteLine("  --skip-existing   Skip conversion for files that already exist.");
                Console.WriteLine("  --recursive       Process files in subdirectories.");
                Console.WriteLine("  --replace         Delete the original WAV files after conversion.");
                args = Array.Empty<string>(); // reset for next loop
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
                return (inputDirectory, forceOverwrite, skipIfExists, recursive, deleteWavAfterConversion);
            Console.WriteLine($"The directory '{inputDirectory}' does not exist.");
            args = Array.Empty<string>(); // trigger retry
        }
    }

    static void ConvertWavToOgg(string inputDirectory, bool forceOverwrite, bool skipIfExists, 
        bool recursive, bool deleteWavAfterConversion, string ffmpegPath)
    {
        
        if (!File.Exists(ffmpegPath))
        {
            Console.WriteLine($"FFmpeg not found at path: {ffmpegPath}");
            return;
        }
        
        var files = Directory.GetFiles(inputDirectory, "*.wav",
            recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);

        if (files.Length == 0)
        {
            Console.WriteLine($"No WAV files found in {inputDirectory}.");
            Console.WriteLine(!recursive
                ? "(Consider using the --recursive flag to search subdirectories.)"
                : "(Including subdirectories).");
            return;
        }

        foreach (var inputFile in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var outputFile = Path.Combine(inputDirectory, $"{fileName}.ogg");

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
                            Console.WriteLine($"Deleted original WAV: {inputFile}");
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to delete WAV file {inputFile}: {ex.Message}");
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