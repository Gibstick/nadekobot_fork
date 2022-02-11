using Discord;
using Discord.Interactions;
using Discord.Rest;
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
using SixLabors.ImageSharp.Processing;
using SixLabors.Fonts;
using System.IO;
using Serilog;
using AngleSharp;

namespace NadekoBot.Modules.Utility
{
    public partial class UtilitySlashCommands : NadekoSlashModule
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly DiscordSocketClient _client;
        private readonly FontProvider _fonts;


        public UtilitySlashCommands(DiscordSocketClient client,IHttpClientFactory factory,FontProvider fonts)
        {
            _httpFactory = factory;
            _client = client;
            _fonts = fonts;
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

        [NadekoSlash]
        public async Task Banner([Summary("user","Name of user to get banner")] IGuildUser usr)
        {
            await ctx.Interaction.DeferAsync().ConfigureAwait(false);

            var restuser = await _client.Rest.GetUserAsync(usr.Id).ConfigureAwait(false);
            var bannerurl = restuser?.GetBannerUrl(size:2048);
            
            if (bannerurl == null){
                await ctx.Interaction.ModifyOriginalResponseAsync(yy=>yy.Content =$"No Banner for this user").ConfigureAwait(false);
                return;
            }
            await ctx.Interaction.ModifyOriginalResponseAsync(xx=>{
                xx.Embed =new EmbedBuilder().WithOkColor()
                .AddField(efb => efb.WithName("Username").WithValue(usr.ToString()).WithIsInline(false))
                .WithImageUrl(bannerurl.ToString()).Build();
                }).ConfigureAwait(false);
        }

        public async Task Scale([Summary("images","url to 9 images seperated by space")] string images){

            var imagelist = images.Split(" ");
            if (imagelist.Count()<9){
                await ctx.Interaction.RespondAsync("Please provide link to 9 images");
                return;
            }
            int imagelength = 600;
            int imagewidth = 600;
            int fontsize = 50;
            var font = _fonts.NotoSans.CreateFont(fontsize,SixLabors.Fonts.FontStyle.Bold);
            try
            {
            await ctx.Interaction.DeferAsync().ConfigureAwait(false);
            
            // read all images into list of byte array
            var imgbyteslist = new List<byte[]>();
            using (var http = _httpFactory.CreateClient()){
                for (int ii =0;ii<9;ii++){
                    imgbyteslist.Add(await http.GetByteArrayAsync(imagelist[ii]).ConfigureAwait(false));
                }
            }

            using (var backimage = new Image<Rgba32>(imagelength,imagewidth)){
                // make background pixels white
                for (int x =0; x<imagelength;x++){
                    for (int y =0; y<imagewidth;y++){
                        backimage[x,y] = new Rgba32(255,255,255);
                        }
                }
                backimage.Mutate(xx =>{
                    for (int jj=0;jj<9;jj++){
                        int xcord = jj%3;
                        int ycord = jj/3;
                        using (var img2 = SixLabors.ImageSharp.Image.Load(imgbyteslist[jj])){
                            //resize image to square
                            img2.Mutate(yy=>yy.Resize(imagelength/3,imagelength/3));
                            //stack image on background
                            xx.DrawImage(img2,new SixLabors.ImageSharp.Point(xcord*200,ycord*200),1f);
                            //add number in corner
                            xx.DrawText(new TextGraphicsOptions(),jj.ToString(),
                            font,
                            SixLabors.ImageSharp.Color.White,
                            new SixLabors.ImageSharp.PointF(xcord*200, ycord*200));
                        }
                    }
                });
                //save image to stream
                using (MemoryStream ms = new MemoryStream()){
                backimage.SaveAsPng(ms, new PngEncoder()
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
                await ctx.Interaction.SendErrorAsync(ex.Message);
            }
        }
        
    }
}
