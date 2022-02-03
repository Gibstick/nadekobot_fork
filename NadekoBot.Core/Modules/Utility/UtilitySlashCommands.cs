using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Impl;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Drawing;
using System.Net.Http;
using DrawingColor = System.Drawing.Color;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Common;
using System.Net;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using Serilog;
using AngleSharp;

namespace NadekoBot.Modules.Utility
{
    public partial class UtilitySlashCommands : NadekoSlashModule
    {
        private readonly IHttpClientFactory _httpFactory;

        public UtilitySlashCommands(IHttpClientFactory factory)
        {
            _httpFactory = factory;
        }
        
        [NadekoSlash]
        public async Task Giframe([Summary("gifurl","url to gif")] string gifurl)
        {
            gifurl = gifurl?.Trim() ?? "";

            if (string.IsNullOrWhiteSpace(gifurl)){
                return;
            }
            await ctx.Interaction.DeferAsync().ConfigureAwait(false);
            try
            {
            byte[] imageBytes;
            ImageFrameCollection imgframe;
            // case for tenor urls
            if ((gifurl.EndsWith(".gif") == false) && (gifurl.Contains("tenor.com"))){
                var config = AngleSharp.Configuration.Default.WithDefaultLoader();
                var context = BrowsingContext.New(config);
                var document = await context.OpenAsync(gifurl).ConfigureAwait(false);
                string newgifurl = document.QuerySelectorAll("meta").Where(x=>x.GetAttribute("content").EndsWith(".gif")).Select(m=>m.GetAttribute("content")).FirstOrDefault();

                if (newgifurl == null){
                    await ctx.Interaction.SendErrorAsync("Could not download tenor gif");
                    return;
                }

                gifurl = newgifurl;

            }
            using (var http = _httpFactory.CreateClient()) {
                imageBytes = await http.GetByteArrayAsync(gifurl).ConfigureAwait(false);
            }

            using (SixLabors.ImageSharp.Image img = SixLabors.ImageSharp.Image.Load(imageBytes)){
                imgframe = img.Frames;
                
                int framecount = imgframe.Count;
                var rng = new NadekoRandom();
                int idx = rng.Next(framecount);
                var singleframe = imgframe.ExportFrame(idx);
                using (MemoryStream ms = new MemoryStream()){
                singleframe.SaveAsPng(ms, new PngEncoder()
                {
                    ColorType = PngColorType.RgbWithAlpha,
                    CompressionLevel = PngCompressionLevel.BestCompression
                });
                ms.Position = 0;
                await ctx.Interaction.FollowupWithFileAsync(ms, $"img.png", ctx.User.Mention).ConfigureAwait(false);
                }
            }
            }
            catch (System.Exception ex)
            {
            await ctx.Interaction.SendErrorAsync(ex.Message).ConfigureAwait(false); 
            }
        }



    }
}
