using Discord.Commands;
using NadekoBot.Common.Attributes;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using Discord;
using NadekoBot.Core.Modules.Utility.Services;
using System.Linq;
using System.Collections.Generic;
using Serilog;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        public class SqlCommands : NadekoSubmodule<SqlCommandsService>
        {

            [NadekoCommand, Usage, Description, Aliases]
            public async Task SqlSelectQuery([Leftover]string sql)
            {
 
                try{
                    // var tablest = _service.tables();
                    // foreach(var tt in tablest){
                    //     Log.Information(tt);
                    // }
                    var tabresult = _service.SelectSql("SELECT name FROM sqlite_master WHERE type = \"table\"");
                    var tables = tabresult.Results.Take(100).Select(x => x[0]).ToList();
                    if (validatesql(sql,tables)== false){
                        return;
                    }
                    var result = _service.SelectSql(sql);
                    await ctx.SendPaginatedConfirmAsync(0, (cur) =>
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
                }catch (Exception e){
                    await ctx.Channel.SendErrorAsync(e.Message);
                }
            }

            public Boolean validatesql(string sql,List<string> tables){
                var sqllower = sql.ToLowerInvariant();
                var excludetable = true;
                var includetable=false;
                foreach(var table in tables){
                    if ((table == "Quotes") & (sqllower.Contains("quotes"))){
                        includetable=true;

                    }else if (sqllower.Contains(table.ToLowerInvariant())){
                        excludetable = false; 
                    }
                }

                return (excludetable) & (includetable);
            }

        }
    }
}
