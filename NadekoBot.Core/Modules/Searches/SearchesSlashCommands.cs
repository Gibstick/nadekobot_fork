using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Common;
using NadekoBot.Modules.Searches.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Serilog;
using Configuration = AngleSharp.Configuration;

namespace NadekoBot.Modules.Searches
{
    public partial class SearchesSlashCommands : NadekoSlashModule<SearchesService>
    {

        public SearchesSlashCommands()
        {
        }

        [NadekoSlash]
        public Task RandomImg([Summary("tag","Select random image tag")]SearchesService.ImageTag tag) => InternalRandomImage(tag);

        private Task InternalRandomImage(SearchesService.ImageTag tag)
        {
            var url = _service.GetRandomImageUrl(tag);
            return ctx.Interaction.RespondAsync(embed:new EmbedBuilder()
                .WithOkColor()
                .WithImageUrl(url).Build());
        }

    }
}
