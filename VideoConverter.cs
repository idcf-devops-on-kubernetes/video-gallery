using System;
using System.IO;
using System.Threading.Tasks;
using Xabe.FFmpeg;

namespace VideoGallery
{
    public class VideoConverter
    {
        public static async Task ConvertToGif(string inputVideoPath, string outputGifPath, string durationPath)
        {
            var outputPath = outputGifPath + "-creating.gif";
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FFMPEG_PATH")))
            {
                FFmpeg.ExecutablesPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
            }
            
            var mediaInfo = await MediaInfo.Get(inputVideoPath).ConfigureAwait(false);
            await File.WriteAllTextAsync(durationPath, Math.Floor(mediaInfo.Duration.TotalSeconds).ToString());
            
            var conversion = Conversion.New();
            conversion.OnDataReceived += (sender, args) => { Console.WriteLine("Message from ffmpeg:" + args.Data); };
            await conversion
                .Start($"-ss 5 -t 5 -i {inputVideoPath} -vf \"fps=10,scale=320:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse\" -loop 0 {outputPath}")
                .ConfigureAwait(false);
            
            if (File.Exists(outputPath))
            {
                File.Move(outputPath, outputGifPath);
            }
        }

    }
}