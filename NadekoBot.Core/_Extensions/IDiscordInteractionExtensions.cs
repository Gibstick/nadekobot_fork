using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NadekoBot.Extensions
{
    public static class IDiscordInteractionExtensions
    {
        public static Task<IUserMessage> EmbedAsync(this IDiscordInteraction di, EmbedBuilder embed, string msg = "")
            {
            return di.ModifyOriginalResponseAsync(x=>{
                        x.Embed = embed.Build();
                        x.Content = msg;
                    },options: new RequestOptions() { RetryMode  = RetryMode.AlwaysRetry });
            }

        public static Task<IUserMessage> SendErrorAsync(this IDiscordInteraction di, string title, string error, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithErrorColor().WithDescription(error)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return di.ModifyOriginalResponseAsync(x=>{
                        x.Embed = eb.Build();
                        x.Content = "";
            });
        }

        public static Task<IUserMessage> SendErrorAsync(this IDiscordInteraction di, string error)
             => di.ModifyOriginalResponseAsync(x=> x.Embed =new EmbedBuilder().WithErrorColor().WithDescription(error).Build());

        public static Task<IUserMessage> SendPendingAsync(this IDiscordInteraction di, string message)
            => di.ModifyOriginalResponseAsync(x=>x.Embed = new EmbedBuilder().WithPendingColor().WithDescription(message).Build());
        
        public static Task<IUserMessage> SendConfirmAsync(this IDiscordInteraction di, string title, string text, string url = null, string footer = null)
        {
            var eb = new EmbedBuilder().WithOkColor().WithDescription(text)
                .WithTitle(title);
            if (url != null && Uri.IsWellFormedUriString(url, UriKind.Absolute))
                eb.WithUrl(url);
            if (!string.IsNullOrWhiteSpace(footer))
                eb.WithFooter(efb => efb.WithText(footer));
            return di.ModifyOriginalResponseAsync(x=>{
                        x.Embed = eb.Build();
                        x.Content = "";
            });
        }

        public static Task<IUserMessage> SendConfirmAsync(this IDiscordInteraction di, string text)
             => di.ModifyOriginalResponseAsync(x=>x.Embed = new EmbedBuilder().WithOkColor().WithDescription(text).Build());

        public static Task<IUserMessage> SendTableAsync<T>(this IDiscordInteraction di, string seed, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3)
        {
            return di.ModifyOriginalResponseAsync(x=>{
                x.Content = $@"{seed}```css
                {string.Join("\n", items.Chunk(columns)
                        .Select(ig => string.Concat(ig.Select(el => howToPrint(el)))))}
                ```";
            });
        }

        public static Task<IUserMessage> SendTableAsync<T>(this IDiscordInteraction di, IEnumerable<T> items, Func<T, string> howToPrint, int columns = 3) =>
            di.SendTableAsync("", items, howToPrint, columns);
        
        private static readonly IEmote arrow_left = new Emoji("⬅");
        private static readonly IEmote arrow_right = new Emoji("➡");
        public static Task SendScrollingButtonAsync(this IInteractionContext ctx,
            int currentPage, Func<int, EmbedBuilder> pageFunc, int totalElements,
            int itemsPerPage){
            return ctx.SendScrollingButtonInternalAsync(currentPage,
                (x) => Task.FromResult(pageFunc(x)), totalElements, itemsPerPage);    
            }
        public static async Task SendScrollingButtonInternalAsync(this IInteractionContext ctx,
            int currentPage, Func<int, Task<EmbedBuilder>> pageFunc, int totalElements,
            int itemsPerPage){
            var embed = await pageFunc(currentPage).ConfigureAwait(false);

            var lastPage = (totalElements - 1) / itemsPerPage;

            embed.AddPaginatedFooter(currentPage, lastPage);
            
            string leftuuid = Guid.NewGuid().ToString();
            string rightuuid = Guid.NewGuid().ToString();
            string firstuuid = Guid.NewGuid().ToString();
            string lastuuid = Guid.NewGuid().ToString();
            var builder = new ComponentBuilder()
            .WithButton(customId: firstuuid,emote: new Emoji("⏮️"))
            .WithButton(customId: leftuuid,emote: new Emoji("◀️"))
            .WithButton(customId:rightuuid,emote:new Emoji("▶️"))
            .WithButton(customId:lastuuid,emote:new Emoji("⏭️")).Build();
            
            var msg = await ctx.Interaction.ModifyOriginalResponseAsync(x=>{
                x.Embed = embed.Build();
                x.Components= builder;
            });
            var _client = ctx.Client as DiscordSocketClient;
            _client.ButtonExecuted += ButtonHandler;
            async Task ButtonHandler (SocketMessageComponent r){
                if (r.User.Id != ctx.User.Id){
                    return;
                }
                if (r.Data.CustomId == firstuuid){
                    currentPage = 0;
                }else if (r.Data.CustomId == leftuuid) {
                    currentPage = Math.Max(0,currentPage-1);
                } else if (r.Data.CustomId == rightuuid){
                    currentPage = Math.Min(lastPage,currentPage+1);
                }else if (r.Data.CustomId == lastuuid){
                    currentPage = lastPage;
                }
                var newembed = await pageFunc(currentPage).ConfigureAwait(false);
                newembed.AddPaginatedFooter(currentPage, lastPage);
                await r.UpdateAsync(x=> x.Embed = newembed.Build());
            };

            while (true){
                var response = await Discord.Interactions.InteractionUtility.WaitForMessageComponentAsync(_client,msg,new TimeSpan(0,0,30));
                if (response == null){
                    await msg.ModifyAsync(x =>{
                        x.Content = " ";
                        x.Components = null;
                    });
                    break;

                }
            }
            _client.ButtonExecuted -= ButtonHandler;
            }
    }
}
