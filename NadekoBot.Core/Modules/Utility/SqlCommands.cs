using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Core.Modules.Utility.Services;
using System.Linq;

#if !GLOBAL_NADEKO
namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        public class SqlCommands : NadekoSubmodule<SqlCommandsService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            public Task SqlSelectQuery([Leftover]string sql)
            {
                try{
                var result = _service.SelectSql(sql);
                }catch{
                    return;
                }
                return ctx.SendPaginatedConfirmAsync(0, (cur) =>
                {
                    var items = result.Results.Skip(cur * 20).Take(20);

                    if (!items.Any())
                    {
                        return new EmbedBuilder()
                            .WithErrorColor()
                            .WithFooter(sql)
                            .WithDescription("-");
                    }

                    return new EmbedBuilder()
                        .WithOkColor()
                        .WithFooter(sql)
                        .WithTitle(string.Join(" ║ ", result.ColumnNames))
                        .WithDescription(string.Join('\n', items.Select(x => string.Join(" ║ ", x))));

                }, result.Results.Count, 20);
            }

        }
    }
}
#endif