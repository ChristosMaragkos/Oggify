using System.Diagnostics;

namespace Oggify;

class Program
{
    
    static void Main(string[] args)
    {
        
        Console.WriteLine("Oggify - Convert WAV files to OGG format using FFmpeg");
        Console.WriteLine("--------------------------------------------------");
        Console.WriteLine("This tool converts all WAV files in a specified directory to OGG format using FFmpeg.");
        Console.WriteLine("Please provide the path to or drag and drop the folder containing the WAV files you would like to convert.");

        string inputDirectory;
        if (args.Length == 0)
        {
            Console.Write("Enter path to input folder: ");
            inputDirectory = Console.ReadLine()?.Trim('"') ?? "";
        }
        else
        {
            inputDirectory = args[0];
        }
        if (!Directory.Exists(inputDirectory))
        {
            Console.WriteLine($"The directory '{inputDirectory}' does not exist.");
            Console.WriteLine("Please provide a valid directory.");
            Console.WriteLine("Usage: Oggify.exe <input_directory>");
        }

        var ffmpegPath = @"C:\Program Files (x86)\FFmpeg for Audacity\ffmpeg.exe";
        
        var files = Directory.GetFiles(inputDirectory, "*.wav");

        if (files.Length == 0)
        {
            Console.WriteLine($"No files to convert in {inputDirectory}.");
            Environment.Exit(0);
        }

        foreach (var inputFile in files)
        {
            var fileName = Path.GetFileNameWithoutExtension(inputFile);
            var outputFile = Path.Combine(inputDirectory, $"{fileName}.ogg");


            if (File.Exists(outputFile))
            {
                Console.WriteLine($"File '{outputFile}' already exists. Overwrite? (y/n)");
                var response = Console.ReadKey();
                Console.WriteLine();

                if (response.KeyChar != 'y' && response.KeyChar != 'Y')
                {
                    Console.WriteLine($"Skipping {inputFile}.");
                    continue;
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
                    ? $"Converted {inputFile} to {outputFile}"
                    : $"Error converting {inputFile}: {process.StandardError.ReadToEnd()}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred while processing {inputFile}: {ex.Message}");
            }
        }

        Console.WriteLine("All files converted.");
        Console.WriteLine("Press any key to exit.");
        Console.ReadKey();

    }
    
}