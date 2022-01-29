using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Caching.Memory;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using System.Collections.Generic;
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
using System.IO;
using NadekoBot.Modules.Administration.Services;
using Serilog;
using Configuration = AngleSharp.Configuration;

namespace NadekoBot.Modules.Searches
{

    public partial class SearchesSlashCommands : NadekoSlashModule<SearchesService>
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly GuildTimezoneService _tzSvc;
        public SearchesSlash(IHttpClientFactory factory,GuildTimezoneService tzSvc)
        {
            _httpFactory = factory;
            _tzSvc = tzSvc;
        }

        [NadekoSlash]
        public async Task Weather([Summary("city","name of the city")]string city,
        [Summary("country","optional name of country")]string country = "")
        {
            if (string.IsNullOrWhiteSpace(city)){
                return;
            }
            string query = city;
            await ctx.Interaction.DeferAsync().ConfigureAwait(false); 
            if (country != ""){
                (bool valid,string code) = await ValidateCountry(country).ConfigureAwait(false);
                if (valid){
                    query = $"{city},{code}"; 
                }else{
                    return;
                }
            }
            var embed = new EmbedBuilder();
            var data = await _service.GetWeatherDataAsync(query).ConfigureAwait(false);
            if (data == null)
            {
                embed.WithDescription(GetText("city_not_found"))
                    .WithErrorColor();
            }
            else
            {
                Func<double, double> f = StandardConversions.CelsiusToFahrenheit;
                
                var tz = Context.Guild is null
                    ? TimeZoneInfo.Utc
                    : _tzSvc.GetTimeZoneOrUtc(Context.Guild.Id);
                var sunrise = data.Sys.Sunrise.ToUnixTimestamp();
                var sunset = data.Sys.Sunset.ToUnixTimestamp();
                sunrise = sunrise.ToOffset(tz.GetUtcOffset(sunrise));
                sunset = sunset.ToOffset(tz.GetUtcOffset(sunset));
                var timezone = $"UTC{sunrise:zzz}";

                embed.AddField(fb => fb.WithName("🌍 " + Format.Bold(GetText("location"))).WithValue($"[{data.Name + ", " + data.Sys.Country}](https://openweathermap.org/city/{data.Id})").WithIsInline(true))
                    .AddField(fb => fb.WithName("📏 " + Format.Bold(GetText("latlong"))).WithValue($"{data.Coord.Lat}, {data.Coord.Lon}").WithIsInline(true))
                    .AddField(fb => fb.WithName("☁ " + Format.Bold(GetText("condition"))).WithValue(string.Join(", ", data.Weather.Select(w => w.Main))).WithIsInline(true))
                    .AddField(fb => fb.WithName("😓 " + Format.Bold(GetText("humidity"))).WithValue($"{data.Main.Humidity}%").WithIsInline(true))
                    .AddField(fb => fb.WithName("💨 " + Format.Bold(GetText("wind_speed"))).WithValue(data.Wind.Speed + " m/s").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌡 " + Format.Bold(GetText("temperature"))).WithValue($"{data.Main.Temp:F1}°C / {f(data.Main.Temp):F1}°F").WithIsInline(true))
                    .AddField(fb => fb.WithName("🔆 " + Format.Bold(GetText("min_max"))).WithValue($"{data.Main.TempMin:F1}°C - {data.Main.TempMax:F1}°C\n{f(data.Main.TempMin):F1}°F - {f(data.Main.TempMax):F1}°F").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌄 " + Format.Bold(GetText("sunrise"))).WithValue($"{sunrise:HH:mm} {timezone}").WithIsInline(true))
                    .AddField(fb => fb.WithName("🌇 " + Format.Bold(GetText("sunset"))).WithValue($"{sunset:HH:mm} {timezone}").WithIsInline(true))
                    .WithOkColor()
                    .WithFooter(efb => efb.WithText("Powered by openweathermap.org").WithIconUrl($"http://openweathermap.org/img/w/{data.Weather[0].Icon}.png"));
            }
            await ctx.Interaction.EmbedAsync(embed).ConfigureAwait(false);
        }

        public async Task<(bool valid,string code)> ValidateCountry(string country)
        {
            List<CountryCode> dd = JsonConvert.DeserializeObject<List<CountryCode>>(File.ReadAllText("data/country_codes.json"));
            var code = dd.Where(x=>x.Name.ToLowerInvariant()==country.ToLowerInvariant()).Select(e => e.Code).FirstOrDefault();
            if (code==null){
                await ctx.Interaction.SendErrorAsync("Country not found. Try again with a valid country name").ConfigureAwait(false);
                return (false,null);
            } 
            return (true,code);
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

