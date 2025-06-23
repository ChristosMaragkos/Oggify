using System.Diagnostics;

namespace Oggify;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Oggify - Convert WAV files to OGG format using FFmpeg");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("This tool converts all WAV files in a specified directory to OGG format using FFmpeg.");
        Console.WriteLine("Usage: Oggify.exe <input_directory> [--overwrite] [--skip-existing]");
        Console.WriteLine();

        var (inputDirectory, forceOverwrite, skipIfExists) = ParseInput(args);

        ConvertWavToOgg(inputDirectory, forceOverwrite, skipIfExists);

        Console.WriteLine("All files converted.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();
    }

    static (string inputDirectory, bool forceOverwrite, bool skipIfExists) ParseInput(string[] args)
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
            bool forceOverwrite = false;
            bool skipIfExists = false;

            if (inputArgs[0].Equals("help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("--help", StringComparison.OrdinalIgnoreCase) ||
                inputArgs[0].Equals("-h", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine("Usage: Oggify.exe <input_directory> [--overwrite] [--skip-existing]");
                Console.WriteLine("Options:");
                Console.WriteLine("  --overwrite       Force overwrite existing OGG files.");
                Console.WriteLine("  --skip-existing   Skip conversion for files that already exist.");
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
                    default:
                        Console.WriteLine($"Unknown argument: {inputArgs[i]}");
                        break;
                }
            }

            if (!Directory.Exists(inputDirectory))
            {
                Console.WriteLine($"The directory '{inputDirectory}' does not exist.");
                args = Array.Empty<string>(); // trigger retry
                continue;
            }

            return (inputDirectory, forceOverwrite, skipIfExists);
        }
    }

    static void ConvertWavToOgg(string inputDirectory, bool forceOverwrite, bool skipIfExists)
    {
        var ffmpegPath = @"C:\Program Files (x86)\FFmpeg for Audacity\ffmpeg.exe";
        var files = Directory.GetFiles(inputDirectory, "*.wav");

        if (files.Length == 0)
        {
            Console.WriteLine($"No WAV files found in {inputDirectory}.");
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

                Console.WriteLine(process.ExitCode == 0
                    ? $"Converted {inputFile} → {outputFile}"
                    : $"Error converting {inputFile}:\n{process.StandardError.ReadToEnd()}");
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