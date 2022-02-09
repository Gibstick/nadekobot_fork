using Discord;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using DrawingColor = System.Drawing.Color;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using System.IO;
using System.Text.RegularExpressions;
using AngleSharp;
using Discord.Commands;

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
        public async Task Giframe([Discord.Interactions.Summary("gifurl","url to gif")] string gifurl)
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
        public async Task ShowEmojies([Leftover] string text)
        {
            await ctx.Interaction.DeferAsync().ConfigureAwait(false);

            const string pattern = "<:\\w+:[0-9]+>";
            var rgx = new Regex(pattern);
            var matches = rgx.Matches(text);

            var emotes = new List<Emote>();
            matches.ForEach(match => emotes.Add(Emote.Parse(match.Value)));
            var result = string.Join("\n", emotes.Select(emote => GetText("showemojis", emote, emote.Url)));

            if (string.IsNullOrWhiteSpace(result))
                await ctx.Interaction.SendErrorAsync(GetText("showemojis_none")).ConfigureAwait(false);
            else
                await ctx.Interaction.FollowupAsync(result.TrimTo(2000)).ConfigureAwait(false);
        }

    }
}
