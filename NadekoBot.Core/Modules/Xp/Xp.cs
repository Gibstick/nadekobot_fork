﻿using Discord;
using Discord.Commands;
using Discord.WebSocket;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using NadekoBot.Modules.Xp.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Core.Modules.Gambling.Services;

namespace NadekoBot.Modules.Xp
{
    public partial class Xp : NadekoModule<XpService>
    {
        private readonly DownloadTracker _tracker;
        private readonly GamblingConfigService _gss;

        public Xp(DownloadTracker tracker, GamblingConfigService gss)
        {
            _tracker = tracker;
            _gss = gss;
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task Experience([Leftover] IUser user = null)
        {
            user = user ?? ctx.User;
            await ctx.Channel.TriggerTypingAsync().ConfigureAwait(false);
            var (img, fmt) = await _service.GenerateXpImageAsync((IGuildUser)user).ConfigureAwait(false);
            using (img)
            {
                await ctx.Channel.SendFileAsync(img, $"{ctx.Guild.Id}_{user.Id}_xp.{fmt.FileExtensions.FirstOrDefault()}")
                    .ConfigureAwait(false);
            }
        }

        [NadekoCommand, Usage, Description, Aliases]
        public async Task XpRewsReset()
        {
            var reply = await PromptUserConfirmAsync(new EmbedBuilder()
                .WithPendingColor()
                .WithDescription(GetText("xprewsreset_confirm")));

            if (!reply)
                return;

            await _service.ResetXpRewards(ctx.Guild.Id);
            await ctx.OkAsync();
        }
        
        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public Task XpLevelUpRewards(int page = 1)
        {
            page--;

            if (page < 0 || page > 100)
                return Task.CompletedTask;

            var allRewards = _service.GetRoleRewards(ctx.Guild.Id)
                .OrderBy(x => x.Level)
                .Select(x =>
                {
                    var str = ctx.Guild.GetRole(x.RoleId)?.ToString();
                    if (str != null)
                        str = GetText("role_reward", Format.Bold(str));
                    return (x.Level, RoleStr: str);
                })
                .Where(x => x.RoleStr != null)
                .Concat(_service.GetCurrencyRewards(ctx.Guild.Id)
                    .OrderBy(x => x.Level)
                    .Select(x => (x.Level, Format.Bold(x.Amount + _gss.Data.Currency.Sign))))
                .GroupBy(x => x.Level)
                .OrderBy(x => x.Key)
                .ToList();

            return Context.SendPaginatedConfirmAsync(page, cur =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("level_up_rewards"))
                    .WithOkColor();
                
                var localRewards = allRewards
                    .Skip(cur * 9)
                    .Take(9)
                    .ToList();

                if (!localRewards.Any())
                    return embed.WithDescription(GetText("no_level_up_rewards"));

                foreach (var reward in localRewards)
                {
                    embed.AddField(GetText("level_x", reward.Key),
                        string.Join("\n", reward.Select(y => y.Item2)));
                }
                
                return embed;
            }, allRewards.Count, 9);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.Administrator)]
        [RequireContext(ContextType.Guild)]
        public async Task XpRoleReward(int level, [Leftover] IRole role = null)
        {
            if (level < 1)
                return;

            _service.SetRoleReward(ctx.Guild.Id, level, role?.Id);

            if (role == null)
                await ReplyConfirmLocalizedAsync("role_reward_cleared", level).ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("role_reward_added", level, Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task XpCurrencyReward(int level, int amount = 0)
        {
            if (level < 1 || amount < 0)
                return;

            _service.SetCurrencyReward(ctx.Guild.Id, level, amount);
            var config = _gss.Data;

            if (amount == 0)
                await ReplyConfirmLocalizedAsync("cur_reward_cleared", level, config.Currency.Sign)
                    .ConfigureAwait(false);
            else
                await ReplyConfirmLocalizedAsync("cur_reward_added", 
                    level, Format.Bold(amount + config.Currency.Sign))
                    .ConfigureAwait(false);
        }

        public enum NotifyPlace
        {
            Server = 0,
            Guild = 0,
            Global = 1,
        }

        private string GetNotifLocationString(XpNotificationLocation loc)
        {
            if (loc == XpNotificationLocation.Channel)
            {
                return GetText("xpn_notif_channel");
            }

            if (loc == XpNotificationLocation.Dm)
            {
                return GetText("xpn_notif_dm");
            }

            return GetText("xpn_notif_disabled");
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpNotify()
        {
            var globalSetting = _service.GetNotificationType(ctx.User);
            var serverSetting = _service.GetNotificationType(ctx.User.Id, ctx.Guild.Id);

            var embed = new EmbedBuilder()
                .WithOkColor()
                .AddField(GetText("xpn_setting_global"), GetNotifLocationString(globalSetting))
                .AddField(GetText("xpn_setting_server"), GetNotifLocationString(serverSetting));

            await Context.Channel.EmbedAsync(embed);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpNotify(NotifyPlace place, XpNotificationLocation type)
        {
            if (place == NotifyPlace.Guild)
                await _service.ChangeNotificationType(ctx.User.Id, ctx.Guild.Id, type).ConfigureAwait(false);
            else
                await _service.ChangeNotificationType(ctx.User, type).ConfigureAwait(false);
            
            await ctx.OkAsync().ConfigureAwait(false);
        }

        public enum Server { Server };

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpExclude(Server _)
        {
            var ex = _service.ToggleExcludeServer(ctx.Guild.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(ctx.Guild.ToString())).ConfigureAwait(false);
        }

        public enum Role { Role };

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.ManageRoles)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Role _, [Leftover] IRole role)
        {
            var ex = _service.ToggleExcludeRole(ctx.Guild.Id, role.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(role.ToString())).ConfigureAwait(false);
        }

        public enum Channel { Channel };

        [NadekoCommand, Usage, Description, Aliases]
        [UserPerm(GuildPerm.ManageChannels)]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclude(Channel _, [Leftover] IChannel channel = null)
        {
            if (channel == null)
                channel = ctx.Channel;

            var ex = _service.ToggleExcludeChannel(ctx.Guild.Id, channel.Id);

            await ReplyConfirmLocalizedAsync((ex ? "excluded" : "not_excluded"), Format.Bold(channel.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpExclusionList()
        {
            var serverExcluded = _service.IsServerExcluded(ctx.Guild.Id);
            var roles = _service.GetExcludedRoles(ctx.Guild.Id)
                .Select(x => ctx.Guild.GetRole(x))
                .Where(x => x != null)
                .Select(x => $"`role`   {x.Mention}")
                .ToList();

            var chans = (await Task.WhenAll(_service.GetExcludedChannels(ctx.Guild.Id)
                .Select(x => ctx.Guild.GetChannelAsync(x)))
                .ConfigureAwait(false))
                    .Where(x => x != null)
                    .Select(x => $"`channel` <#{x.Id}>")
                    .ToList();

            var rolesStr = roles.Any() ? string.Join("\n", roles) + "\n" : string.Empty;
            var chansStr = chans.Count > 0 ? string.Join("\n", chans) + "\n" : string.Empty;
            var desc = Format.Code(serverExcluded
                ? GetText("server_is_excluded")
                : GetText("server_is_not_excluded"));

            desc += "\n\n" + rolesStr + chansStr;

            var lines = desc.Split('\n');
            await ctx.SendPaginatedConfirmAsync(0, curpage =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("exclusion_list"))
                    .WithDescription(string.Join('\n', lines.Skip(15 * curpage).Take(15)))
                    .WithOkColor();

                return embed;
            }, lines.Length, 15);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [NadekoOptions(typeof(LbOpts))]
        [Priority(0)]
        [RequireContext(ContextType.Guild)]
        public Task XpLeaderboard(params string[] args)
            => XpLeaderboard(1, args);

        [NadekoCommand, Usage, Description, Aliases]
        [NadekoOptions(typeof(LbOpts))]
        [Priority(1)]
        [RequireContext(ContextType.Guild)]
        public async Task XpLeaderboard(int page = 1, params string[] args)
        {
            if (--page < 0 || page > 100)
                return;

            var (opts, _) = OptionsParser.ParseFrom(new LbOpts(), args);

            await Context.Channel.TriggerTypingAsync();

            var socketGuild = ((SocketGuild)ctx.Guild);
            List<UserXpStats> allUsers = new List<UserXpStats>();
            if (opts.Clean)
            {
                await Context.Channel.TriggerTypingAsync().ConfigureAwait(false);
                await _tracker.EnsureUsersDownloadedAsync(ctx.Guild).ConfigureAwait(false);
                
                allUsers = _service.GetTopUserXps(ctx.Guild.Id, 1000)
                    .Where(user => !(socketGuild.GetUser(user.UserId) is null))
                    .ToList();
            }

            await ctx.SendPaginatedConfirmAsync(page, (curPage) =>
            {
                var embed = new EmbedBuilder()
                    .WithTitle(GetText("server_leaderboard"))
                    .WithOkColor();

                List<UserXpStats> users;
                if (opts.Clean)
                {
                    users = allUsers.Skip(curPage * 9).Take(9).ToList();
                }
                else
                {
                    users = _service.GetUserXps(ctx.Guild.Id, curPage);
                }

                if (!users.Any())
                    return embed.WithDescription("-");
                else
                {
                    for (int i = 0; i < users.Count; i++)
                    {
                        var levelStats = new LevelStats(users[i].Xp + users[i].AwardedXp);
                        var user = ((SocketGuild)ctx.Guild).GetUser(users[i].UserId);

                        var userXpData = users[i];

                        var awardStr = "";
                        if (userXpData.AwardedXp > 0)
                            awardStr = $"(+{userXpData.AwardedXp})";
                        else if (userXpData.AwardedXp < 0)
                            awardStr = $"({userXpData.AwardedXp})";

                        embed.AddField(
                            $"#{(i + 1 + curPage * 9)} {(user?.ToString() ?? users[i].UserId.ToString())}",
                            $"{GetText("level_x", levelStats.Level)} - {levelStats.TotalXp}xp {awardStr}");
                    }
                    return embed;
                }
            }, 900, 9, addPaginatedFooter: false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        public async Task XpGlobalLeaderboard(int page = 1)
        {
            if (--page < 0 || page > 99)
                return;
            var users = _service.GetUserXps(page);

            var embed = new EmbedBuilder()
                .WithTitle(GetText("global_leaderboard"))
                .WithOkColor();

            if (!users.Any())
                embed.WithDescription("-");
            else
            {
                for (int i = 0; i < users.Length; i++)
                {
                    var user = users[i];
                    embed.AddField(
                        $"#{i + 1 + page * 9} {(user.ToString())}",
                        $"{GetText("level_x", new LevelStats(users[i].TotalXp).Level)} - {users[i].TotalXp}xp");
                }
            }

            await ctx.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public async Task XpAdd(int amount, ulong userId)
        {
            if (amount == 0)
                return;

            _service.AddXp(userId, ctx.Guild.Id, amount);
            var usr = ((SocketGuild)ctx.Guild).GetUser(userId)?.ToString()
                ?? userId.ToString();
            await ReplyConfirmLocalizedAsync("modified", Format.Bold(usr), Format.Bold(amount.ToString())).ConfigureAwait(false);
        }

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [UserPerm(GuildPerm.Administrator)]
        public Task XpAdd(int amount, [Leftover] IGuildUser user)
            => XpAdd(amount, user.Id);

        [NadekoCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task XpTemplateReload()
        {
            _service.ReloadXpTemplate();
            await Task.Delay(1000).ConfigureAwait(false);
            await ReplyConfirmLocalizedAsync("template_reloaded").ConfigureAwait(false);
        }
    }
}
