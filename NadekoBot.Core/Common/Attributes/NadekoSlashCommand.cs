using System;
using System.Runtime.CompilerServices;
using Discord.Interactions;
using NadekoBot.Core.Services.Impl;


namespace NadekoBot.Common.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class NadekoSlashAttribute : SlashCommandAttribute
    {
        public NadekoSlashAttribute([CallerMemberName] string memberName="",string description="") 
            : base(CommandNameLoadHelper.GetCommandNameFor(memberName).ToLowerInvariant(),Localization.LoadCommand(memberName.ToLowerInvariant()).Desc)
        {
            this.MethodName = memberName.ToLowerInvariant();
        }

        public string MethodName { get; }
    }
}
