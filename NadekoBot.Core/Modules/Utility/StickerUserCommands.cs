using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Net.Http;
using System;
using System.IO;
using Serilog;
using NadekoBot.Core.Services.Impl;

namespace NadekoBot.Modules.Utility
{
    public partial class StickerUserCommands : NadekoSlashModule
    {
        private readonly DiscordSocketClient _client;
        public StickerUserCommands(DiscordSocketClient client)
        {
            _client = client;
        }

        [MessageCommand("StickerInfo")]
        public async Task StickerInfo(IUserMessage msg)
        {
            try{
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                var isticker = msg.Stickers.FirstOrDefault();
                if (isticker == null){
                    await ctx.Interaction.SendErrorAsync("Message has no stickers").ConfigureAwait(false);
                    return;
                }
                var sticker = await _client.GetStickerAsync(isticker.Id).ConfigureAwait(false);
                var embed = new EmbedBuilder().WithOkColor()
                    .AddField(fb=>fb.WithName("Name").WithValue(sticker.Name));
                if (string.IsNullOrEmpty(sticker.Description)==false){
                    embed.AddField(fb=>fb.WithName("Description").WithValue(sticker.Description));
                }
                if (sticker.Tags.Count()>0){
                    embed.AddField(fb=>fb.WithName("Tags").WithValue(string.Join(",",sticker.Tags)));
                }
                embed.AddField(fb=>fb.WithName("Url").WithValue(sticker.GetStickerUrl()));
                await ctx.Interaction.EmbedAsync(embed).ConfigureAwait(false);

            }catch (Exception e){
                await ctx.Interaction.SendErrorAsync(e.Message);
                return;
            }
        }
        
    }
}
