﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Discord;
using NadekoBot.Common;
using Newtonsoft.Json;
using System.IO;
using YamlDotNet.Serialization;

namespace NadekoBot.Core.Services.Impl
{
    public class Localization : ILocalization
    {
        private readonly BotConfigService _bss;
        private readonly DbService _db;

        public ConcurrentDictionary<ulong, CultureInfo> GuildCultureInfos { get; }
        public CultureInfo DefaultCultureInfo => _bss.Data.DefaultLocale;

        public static IDeserializer _deserializer = new DeserializerBuilder().Build();

        private static readonly Dictionary<string, CommandStrings> _commandData = _deserializer.Deserialize<Dictionary<string, CommandStrings>>(
                File.ReadAllText("./data/strings/commands/commands.en-US.yml"));
        

        public Localization(BotConfigService bss, NadekoBot bot, DbService db)
        {
            _bss = bss;
            _db = db;

            var cultureInfoNames = bot.AllGuildConfigs
                .ToDictionary(x => x.GuildId, x => x.Locale);
            
            GuildCultureInfos = new ConcurrentDictionary<ulong, CultureInfo>(cultureInfoNames.ToDictionary(x => x.Key, x =>
              {
                  CultureInfo cultureInfo = null;
                  try
                  {
                      if (x.Value == null)
                          return null;
                      cultureInfo = new CultureInfo(x.Value);
                  }
                  catch { }
                  return cultureInfo;
              }).Where(x => x.Value != null));
        }

        public void SetGuildCulture(IGuild guild, CultureInfo ci) =>
            SetGuildCulture(guild.Id, ci);

        public void SetGuildCulture(ulong guildId, CultureInfo ci)
        {
            if (ci.Name == _bss.Data.DefaultLocale.Name)
            {
                RemoveGuildCulture(guildId);
                return;
            }

            using (var uow = _db.GetDbContext())
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set);
                gc.Locale = ci.Name;
                uow.SaveChanges();
            }

            GuildCultureInfos.AddOrUpdate(guildId, ci, (id, old) => ci);
        }

        public void RemoveGuildCulture(IGuild guild) =>
            RemoveGuildCulture(guild.Id);

        public void RemoveGuildCulture(ulong guildId)
        {

            if (GuildCultureInfos.TryRemove(guildId, out var _))
            {
                using (var uow = _db.GetDbContext())
                {
                    var gc = uow.GuildConfigs.ForId(guildId, set => set);
                    gc.Locale = null;
                    uow.SaveChanges();
                }
            }
        }

        public void SetDefaultCulture(CultureInfo ci)
        {
            _bss.ModifyConfig(bs =>
            {
                bs.DefaultLocale = ci;
            });
        }

        public void ResetDefaultCulture() =>
            SetDefaultCulture(CultureInfo.CurrentCulture);

        public CultureInfo GetCultureInfo(IGuild guild) =>
            GetCultureInfo(guild?.Id);

        public CultureInfo GetCultureInfo(ulong? guildId)
        {
            if (guildId is null || !GuildCultureInfos.TryGetValue(guildId.Value, out var info) || info is null)
                return _bss.Data.DefaultLocale;
            
            return info;
        }

        public static CommandStrings LoadCommand(string key)
        {
            _commandData.TryGetValue(key, out var toReturn);

            if (toReturn == null)
                return new CommandStrings
                {
                    Desc = key,
                    Args = new[] { key },
                };

            return toReturn;
        }
    }
}
