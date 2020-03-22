using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;

namespace VideoGallery
{
    public class Startup
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("应用正在启动中，请稍后...");
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            Thread.Sleep(new Random().Next(8 * 1000, 20 * 1000));
            Console.WriteLine($"启动完成，一共花费时间 {stopWatch.Elapsed.TotalSeconds}s");
            stopWatch.Stop();
            
            
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>().UseKestrel(options =>
                    {
                        options.Limits.KeepAliveTimeout = TimeSpan.FromSeconds(1);
                        options.Limits.MaxRequestBodySize = 1024 * 1024 * 100; // 100 MB
                    });
                })
                .Build()
                .Run();
        }
        
        
       

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        { 
            var storageBasePath = Path.Combine(Directory.GetCurrentDirectory(), "storage");
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await using var indexStream = typeof(Startup).Assembly.GetManifestResourceStream("VideoGallery.index.html");
                    var indexContent = new StreamReader(indexStream).ReadToEnd();

                    context.Response.ContentType = "text/html";
                    await context.Response.WriteAsync(indexContent);
                }); 
                
                endpoints.MapGet("/videos", async context =>
                {
                    context.Response.ContentType = "application/json";
                    var videoRecordPath = Path.Combine(storageBasePath, "videos.json");
                    if (!File.Exists(videoRecordPath))
                    {
                        await context.Response.WriteAsync("[]");
                        return;
                    }
                    
                    var videoRecordContent = await File.ReadAllTextAsync(videoRecordPath);
                    await context.Response.WriteAsync(videoRecordContent);
                });
                
                endpoints.MapGet("/gif", async context =>
                {
                    var videoId = context.Request.Query["video"].Single();
                    var mappedPath = Path.Combine(storageBasePath, "gif", $"{videoId}.gif");
                    
                    if (!File.Exists(mappedPath))
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        return;
                    }
                    
                    context.Response.ContentType = "image/gif";
                    await context.Response.SendFileAsync(mappedPath);
                }); 
                
                endpoints.MapPost("/upload", async context =>
                {
                    var videoId = Guid.NewGuid().ToString("N");
                    await SaveVideoContent(storageBasePath, videoId, context);

                    var videoRecord = new VideoRecord()
                    {
                        id = videoId,
                        title = "Video " + videoId.Substring(0, 8)
                    };
                    await SaveVideoRecord(storageBasePath, videoRecord);
                    
                    context.Response.StatusCode = (int)HttpStatusCode.Created;
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(videoRecord));
                });
               
            });
        }

        private static async Task SaveVideoContent(string storageBasePath, string videoId, HttpContext context)
        {
            var mappedPath = Path.Combine(storageBasePath, "video", $"{videoId}.mp4");

            if (!Directory.Exists(Path.GetDirectoryName(mappedPath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mappedPath));
                Directory.CreateDirectory(Path.Combine(storageBasePath, "gif"));
            }

            await using var saveAsVideoFile = new FileStream(mappedPath, FileMode.Create);
            var file = context.Request.Form.Files.Single();
            
            await using var readStream = file.OpenReadStream();
            await readStream.CopyToAsync(saveAsVideoFile);
            var _ = VideoConverter.ConvertToGif(mappedPath, Path.Combine(storageBasePath, "gif", $"{videoId}.gif"));
        }

        private static async Task SaveVideoRecord(string storageBasePath, VideoRecord videoRecord)
        {
            List<VideoRecord> allVideos;
            var videoRecordPath = Path.Combine(storageBasePath, "videos.json");
            if (File.Exists(videoRecordPath))
            {
                var jsonText = await File.ReadAllTextAsync(videoRecordPath);
                allVideos = JsonConvert.DeserializeObject<List<VideoRecord>>(jsonText);
            }
            else
            {
                allVideos = new List<VideoRecord>();
            }
                    
            allVideos.Add(videoRecord);
            await File.WriteAllTextAsync(videoRecordPath, JsonConvert.SerializeObject(allVideos));
        }
    }
    
    public class VideoRecord
    {
        public string id { get; set; }
        
        public string title { get; set; }
        
    }
}
