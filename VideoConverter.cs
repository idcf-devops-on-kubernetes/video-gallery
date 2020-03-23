using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xabe.FFmpeg;
using Xabe.FFmpeg.Enums;

namespace VideoGallery
{
    public class VideoConverter
    {
        public static async Task ConvertToGif(string inputVideoPath, string outputGifPath, string durationPath)
        {
            // ffmpeg -ss 5 -t 10 -i input.mp4 -vf "fps=10,scale=320:-1:flags=lanczos,split[s0][s1];[s0]palettegen[p];[s1][p]paletteuse" -loop 0 output.gif
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FFMPEG_PATH")))
            {
                FFmpeg.ExecutablesPath = Environment.GetEnvironmentVariable("FFMPEG_PATH");
            }

            var mediaInfo = await MediaInfo.Get(inputVideoPath).ConfigureAwait(false);
            var video = mediaInfo.VideoStreams.First();
            await File.WriteAllTextAsync(durationPath, Math.Floor(mediaInfo.Duration.TotalSeconds).ToString());
                
            var ratio = (float)video.Width / video.Height;
            var videoStream = video
                .SetSize(new VideoSize((int)Math.Floor(ratio * 320), 320))
                .SetFramerate(10)
                .SetLoop(0);

            var conversion = Conversion.New()
                .AddStream(videoStream)
                .SetSeek(TimeSpan.FromSeconds(5))
                .SetOutputTime(TimeSpan.FromSeconds(5))
                .UseMultiThread(false)
                .SetOverwriteOutput(true)
                .SetOutputFormat(new MediaFormat("gif"))
                .SetOutput(outputGifPath);
              
            await conversion.Start().ConfigureAwait(false);
        }
        
    }
}