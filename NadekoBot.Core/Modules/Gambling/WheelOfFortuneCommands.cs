using Discord;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using NadekoBot.Core.Services;
using System.Threading.Tasks;
using Wof = NadekoBot.Modules.Gambling.Common.WheelOfFortune.WheelOfFortuneGame;
using NadekoBot.Modules.Gambling.Services;
using NadekoBot.Core.Modules.Gambling.Common;
using NadekoBot.Core.Common;
using System.Collections.Immutable;
using NadekoBot.Core.Modules.Gambling.Services;

namespace NadekoBot.Modules.Gambling
{
    public partial class Gambling
    {
        public class WheelOfFortuneCommands : GamblingSubmodule<GamblingService>
        {
            private static readonly ImmutableArray<string> _emojis = new string[] {
            "⬆",
            "↖",
            "⬅",
            "↙",
            "⬇",
            "↘",
            "➡",
            "↗" }.ToImmutableArray();

            private readonly ICurrencyService _cs;
            private readonly DbService _db;

            public WheelOfFortuneCommands(ICurrencyService cs, DbService db, GamblingConfigService gamblingConfService)
                : base(gamblingConfService)
            {
                _cs = cs;
                _db = db;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task WheelOfFortune(ShmartNumber amount)
            {
                if (!await CheckBetMandatory(amount).ConfigureAwait(false))
                    return;

                if (!await _cs.RemoveAsync(ctx.User.Id, "Wheel Of Fortune - bet", amount, gamble: true).ConfigureAwait(false))
                {
                    await ReplyErrorLocalizedAsync("not_enough", CurrencySign).ConfigureAwait(false);
                    return;
                }

                var result = await _service.WheelOfFortuneSpinAsync(ctx.User.Id, amount).ConfigureAwait(false);

                var wofMultipliers = _config.WheelOfFortune.Multipliers;
                var fileName = $"data/images/wheel/wheel_{result.Index}.png";
                var embed = new EmbedBuilder().WithOkColor().WithDescription(Format.Bold($@"{ctx.User.ToString()} won: {result.Amount + CurrencySign}"));
                embed.WithImageUrl($"attachment://{fileName}"); 
                await Context.Channel.SendFileAsync(fileName, embed: embed.Build());
             } 
        }
    }
}
