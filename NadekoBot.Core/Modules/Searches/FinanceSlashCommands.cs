using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using NadekoBot.Modules.Searches.Services;
using NadekoBot.Extensions;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Modules.Searches.Services;
using NadekoBot.Core.Modules.Searches;
using System.Globalization;
using Discord;
using Discord.Interactions;
using Serilog;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        public partial class FinanceSlashCommands : NadekoSlashModule<CryptoService>
        {
            private readonly IStockDataService _stocksService;
            private readonly IStockChartDrawingService _stockDrawingService;

            public FinanceSlashCommands(IStockDataService stocksService, IStockChartDrawingService stockDrawingService)
            {
                _stocksService = stocksService;
                _stockDrawingService = stockDrawingService;
            }
            
            [NadekoSlash]
            public async Task StockSymbol(string query)
            {
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                // by symbol
                var stock = await _stocksService.GetStockDataAsync(query);

                if (stock is null)
                {
                    await ReplyErrorLocalizedAsync("not_found");
                    return;
                }
                await StockInternal(ctx,query,stock);
            }

            [NadekoSlash]
            public async Task StockName(string query){

                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                
                var symbols = await _stocksService.SearchSymbolAsync(query);

                if (symbols.Count == 0)
                {
                    await ReplyErrorLocalizedAsync("not_found");
                    return;
                }

                var symbol = symbols.First();

                query = symbol.Symbol;
                var stock = await _stocksService.GetStockDataAsync(query);

                if (stock is null)
                {
                    await ReplyErrorLocalizedAsync("not_found");
                    return;
                }

                await StockInternal(ctx,query,stock);
            }

            private async Task StockInternal(IInteractionContext ctx,string query,StockData stock)
            {
                var candles = await _stocksService.GetCandleDataAsync(query);
                var stockImageTask = _stockDrawingService.GenerateCombinedChartAsync(candles);
                
                var localCulture = (CultureInfo)_cultureInfo.Clone();
                localCulture.NumberFormat.CurrencySymbol = "$";

                var sign = stock.Price >= stock.Close
                    ? "\\🔼"
                    : "\\🔻";

                var change = (stock.Price - stock.Close).ToString("N2", _cultureInfo);
                var changePercent = (1 - (stock.Close / stock.Price)).ToString("P1", _cultureInfo);
                
                var sign50 = stock.Change50d >= 0
                    ? "\\🔼"
                    : "\\🔻";

                var change50 = (stock.Change50d).ToString("P1", _cultureInfo);
                
                var sign200 = stock.Change200d >= 0
                    ? "\\🔼"
                    : "\\🔻";
                
                var change200 = (stock.Change200d).ToString("P1", _cultureInfo);
                
                var price = stock.Price.ToString("C2", localCulture);

                var eb = new EmbedBuilder()
                            .WithOkColor()
                            .WithAuthor(stock.Symbol)
                            .WithUrl($"https://www.tradingview.com/chart/?symbol={stock.Symbol}")
                            .WithTitle(stock.Name)
                            .AddField(GetText("price"), $"{sign} **{price}**", true)
                            .AddField(GetText("market_cap"), stock.MarketCap.ToString("C0", localCulture), true)
                            .AddField(GetText("volume_24h"), stock.DailyVolume.ToString("C0", localCulture), true)
                            .AddField("Change", $"{change} ({changePercent})", true)
                            .AddField("Change 50d", $"{sign50}{change50}", true)
                            .AddField("Change 200d", $"{sign200}{change200}", true)
                            .WithFooter(stock.Exchange);
                
                var message = await ctx.Interaction.EmbedAsync(eb);
                await using var imageData = await stockImageTask;
                if (imageData is null)
                    return;

                var fileName = $"{query}-sparkline.{imageData.Extension}";
                await message.ModifyAsync(mp =>
                {
                    mp.Attachments =
                        new(new[]
                        {
                            new FileAttachment(
                                imageData.FileData,
                                fileName
                            )
                        });

                    mp.Embed = eb.WithImageUrl($"attachment://{fileName}").Build();
                });

            }
            
        }
    }
}