using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

class Program
{
    static async Task Main(string[] args)
    {
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine(".");
        Console.WriteLine(".");
        Console.WriteLine(@"########\ ##\                         ##\                                         ");
        Console.WriteLine(@"\__##  __|\__|                        ## |                                        ");
        Console.WriteLine(@"   ## |   ##\ ######\####\   ######\  ## | ######\   ######\   #######\  ######\  ");
        Console.WriteLine(@"   ## |   ## |##  _##  _##\ ##  __##\ ## | \____##\ ##  __##\ ##  _____|##  __##\ ");
        Console.WriteLine(@"   ## |   ## |## / ## / ## |######## |## | ####### |## /  ## |\######\  ######## |");
        Console.WriteLine(@"   ## |   ## |## | ## | ## |##   ____|## |##  __## |## |  ## | \____##\ ##   ____|");
        Console.WriteLine(@"   ## |   ## |## | ## | ## |\#######\ ## |\####### |#######  |#######  |\#######\ ");
        Console.WriteLine(@"   \__|   \__|\__| \__| \__| \_______|\__| \_______|##  ____/ \_______/  \_______|");
        Console.WriteLine(@"                                                    ## |                          ");
        Console.WriteLine(@"                                                    ## | V2                       ");
        Console.WriteLine(@"                                                    \__| By Ryan Appleyard        ");
        Console.WriteLine(".");

        string ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
        if (!File.Exists(ffmpegPath))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("ffmpeg not found. Downloading...");
            Console.ResetColor();
            await DownloadAndExtractFfmpeg(ffmpegPath);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("ffmpeg found!");
            Console.ResetColor();
        }

        Console.WriteLine(".");
        Console.Write("Enter the full path to the image directory: ");
        string directory = Console.ReadLine()?.Trim('"') ?? "";

        if (!Directory.Exists(directory))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Directory does not exist.");
            Console.ResetColor();
            return;
        }

        Console.Write("Enter the frame rate (default: 8): ");
        string frInput = Console.ReadLine();
        int framerate = int.TryParse(frInput, out var fr) ? fr : 8;

        Console.Write("Enter output file name (default: timelapse.mp4): ");
        string outputFile = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(outputFile))
            outputFile = "timelapse.mp4";

        var jpgFiles = Directory.GetFiles(directory, "*.jpg", SearchOption.AllDirectories)
                                .OrderBy(f => f)
                                .ToArray();

        if (jpgFiles.Length == 0)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("No .jpg files found in directory.");
            Console.ResetColor();
            return;
        }

        string listFilePath = Path.Combine(directory, "filelist.txt");
        using (var writer = new StreamWriter(listFilePath))
        {
            foreach (var file in jpgFiles)
            {
                writer.WriteLine($"file '{file.Replace("\\", "/")}'");
            }
        }

        string ffmpegArgs = $"-r {framerate} -f concat -safe 0 -i \"{listFilePath}\" -c:v libx264 -pix_fmt yuv420p \"{outputFile}\"";

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = ffmpegPath,
                Arguments = ffmpegArgs,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        Console.WriteLine("Processing...");
        process.Start();

        string stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        if (process.ExitCode == 0)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Done! Output saved to " + outputFile);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("ffmpeg failed:");
            Console.WriteLine(stderr);
        }

        Console.ResetColor();
        if (File.Exists(listFilePath))
            File.Delete(listFilePath);

        Console.ForegroundColor = ConsoleColor.Gray;
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    static async Task DownloadAndExtractFfmpeg(string targetPath)
    {
        string tempZip = Path.Combine(Path.GetTempPath(), "ffmpeg.zip");
        string extractDir = Path.Combine(Path.GetTempPath(), "ffmpeg_extract");

        using (HttpClient client = new HttpClient())
        {
            var url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
            var data = await client.GetByteArrayAsync(url);
            await File.WriteAllBytesAsync(tempZip, data);
        }

        if (Directory.Exists(extractDir))
            Directory.Delete(extractDir, true);

        ZipFile.ExtractToDirectory(tempZip, extractDir);

        string ffmpegExe = Directory.GetFiles(extractDir, "ffmpeg.exe", SearchOption.AllDirectories)[0];

        File.Copy(ffmpegExe, targetPath, overwrite: true);

        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("ffmpeg installed at: " + targetPath);
        Console.ResetColor();
    }
}
