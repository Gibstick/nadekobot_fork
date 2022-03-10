using System;
using System.Threading.Tasks;
using Discord.Commands;
using Discord;
using NadekoBot.Modules.Administration.Services;
using NadekoBot.Core.Common.TypeReaders;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;

namespace NadekoBot.Common.TypeReaders{
    public class EmoteTypeReader : NadekoTypeReader<Emote>
    {
        public EmoteTypeReader(DiscordSocketClient client, CommandService cmds) : base(client, cmds)
        {
        }
        public override Task<TypeReaderResult> ReadAsync(ICommandContext ctx, string input, IServiceProvider services)
        {
        if (!Emote.TryParse(input, out var emote)){
            return Task.FromResult(TypeReaderResult.FromError(CommandError.ParseFailed, "Input is not a valid emote"));
        }
        return Task.FromResult(TypeReaderResult.FromSuccess(emote));
        }
    }
}
