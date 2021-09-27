using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Searches.Services;
using NadekoBot.Extensions;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public class CryptoCommands : NadekoSubmodule<CryptoService>
        {
            [NadekoCommand, Usage, Description, Aliases]
            public async Task Crypto(string name)
            {
                name = name?.ToUpperInvariant();

                if (string.IsNullOrWhiteSpace(name))
                    return;

                var (crypto, nearest) = await _service.GetCryptoData(name).ConfigureAwait(false);
                // get single key in dictionary
                string key = "";
                foreach(var item in crypto.Keys)
                {
                    key = item;
                }
                //var key = keylist[0];
                var cryptodict = crypto[key];
                
                if (crypto == null)
                {
                    await ReplyErrorLocalizedAsync("crypto_not_found").ConfigureAwait(false);
                    return;
                }
                var sevenDay = decimal.TryParse(cryptodict.Quote.Usd.Percent_Change_7d, out var sd)
                        ? sd.ToString("F2")
                        : cryptodict.Quote.Usd.Percent_Change_7d;

                var lastDay = decimal.TryParse(cryptodict.Quote.Usd.Percent_Change_24h, out var ld)
                        ? ld.ToString("F2")
                        : cryptodict.Quote.Usd.Percent_Change_24h;

                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle($"{cryptodict.Name} ({cryptodict.Symbol})")
                    .WithUrl($"https://coinmarketcap.com/currencies/{cryptodict.Slug}/")
                    .WithThumbnailUrl($"https://s3.coinmarketcap.com/static/img/coins/128x128/{cryptodict.Id}.png")
                    .AddField(GetText("market_cap"), $"${cryptodict.Quote.Usd.Market_Cap:n0}", true)
                    .AddField(GetText("price"), $"${cryptodict.Quote.Usd.Price}", true)
                    .AddField(GetText("volume_24h"), $"${cryptodict.Quote.Usd.Volume_24h:n0}", true)
                    .AddField(GetText("change_7d_24h"), $"{sevenDay}% / {lastDay}%", true)
                    .WithImageUrl($"https://s3.coinmarketcap.com/generated/sparklines/web/7d/usd/{cryptodict.Id}.png")).ConfigureAwait(false);
            }
        }
    }
}
