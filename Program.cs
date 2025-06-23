using System.Diagnostics;

namespace Oggify;

class Program
{
    
    static void Main(string[] args)
    {
        var ffmpegPath = @"C:\Program Files (x86)\FFmpeg for Audacity\ffmpeg.exe";
        var inputPath = @"D:\Sounds\input.wav";
        var outputPath = @"D:\Sounds\output.ogg";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = $"-i \"{inputPath}\" -c:a libvorbis -qscale:a 5 \"{outputPath}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }

        };

    }
    
}