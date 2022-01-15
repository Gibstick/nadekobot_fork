using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using System.Globalization;
using System.Threading.Tasks;

namespace NadekoBot.Modules
{
    public abstract class NadekoSlashModule : InteractionModuleBase<IInteractionContext>
    {
        protected CultureInfo _cultureInfo { get; set; }
        public IBotStrings Strings { get; set; }
        public ILocalization Localization { get; set; }

        
        protected IInteractionContext ctx => Context;

        protected NadekoSlashModule()
        {
        }

        public override void BeforeExecute(ICommandInfo cmd)
        {
            _cultureInfo = Localization.GetCultureInfo(ctx.Guild?.Id);
        }

        protected string GetText(string key) =>
            Strings.GetText(key, _cultureInfo);

        protected string GetText(string key, params object[] args) =>
            Strings.GetText(key, _cultureInfo, args);

        public Task<IUserMessage> ErrorLocalizedAsync(string textKey, params object[] args)
        {
            var text = GetText(textKey, args);
            return ctx.Interaction.SendErrorAsync(text);
        }

        public Task<IUserMessage> ReplyErrorLocalizedAsync(string textKey, params object[] args)
        {
            var text = GetText(textKey, args);
            return ctx.Interaction.SendErrorAsync(Format.Bold(ctx.User.ToString()) + " " + text);
        }
        public Task<IUserMessage> ReplyPendingLocalizedAsync(string textKey, params object[] args)
        {
            var text = GetText(textKey, args);
            return ctx.Interaction.SendPendingAsync(Format.Bold(ctx.User.ToString()) + " " + text);
        }

        public Task<IUserMessage> ConfirmLocalizedAsync(string textKey, params object[] args)
        {
            var text = GetText(textKey, args);
            return ctx.Interaction.SendConfirmAsync(text);
        }

        public Task<IUserMessage> ReplyConfirmLocalizedAsync(string textKey, params object[] args)
        {
            var text = GetText(textKey, args);
            return ctx.Interaction.SendConfirmAsync(Format.Bold(ctx.User.ToString()) + " " + text);
        }
        
    }

    public abstract class NadekoSlashModule<TService> : NadekoSlashModule
    {
        public TService _service { get; set; }

        protected NadekoSlashModule() : base()
        {
        }
    }

    public abstract class NadekoSlashSubmodule : NadekoSlashModule
    {
        protected NadekoSlashSubmodule() : base() { }
    }

    public abstract class NadekoSlashSubmodule<TService> : NadekoSlashModule<TService>
    {
        protected NadekoSlashSubmodule() : base()
        {
        }
    }
}