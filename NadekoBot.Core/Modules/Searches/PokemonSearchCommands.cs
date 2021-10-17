using Discord;
using Discord.Commands;
using NadekoBot.Extensions;
using NadekoBot.Modules.Searches.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NadekoBot.Common.Attributes;
using NadekoBot.Core.Common.Pokemon;
using NadekoBot.Core.Services;
using System;
using PokeApiNet;

namespace NadekoBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class PokemonSearchCommands : NadekoSubmodule<PokemonService>
        {
            private readonly IDataCache _cache;

            public IReadOnlyDictionary<string, SearchPokemon> Pokemons => _cache.LocalData.Pokemons;
            public IReadOnlyDictionary<string, SearchPokemonAbility> PokemonAbilities => _cache.LocalData.PokemonAbilities;

            public PokemonSearchCommands(IDataCache cache)
            {
                _cache = cache;
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task Pokemon([Leftover] string pokemon = null)
            {
                pokemon = pokemon?.Trim().ToLowerInvariant();
                if (string.IsNullOrWhiteSpace(pokemon))
                    return;
                Pokemon pok = await _service.GetPokemon(pokemon);
                if (pok == null){
                    await ReplyErrorLocalizedAsync("pokemon_none").ConfigureAwait(false);
                    return;
                }
                string desc = await _service.GetPokemonDescription(pok);
                string thumbnail = await _service.GetPokemonThumbnail(pok);
                var poktypes = await _service.GetPokemonTypes(pok);
                int height = pok.Height/10;
                int weight = pok.Weight/10;
                var abilities = await _service.GetPokemonAbilites(pok);
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                            .WithTitle(pokemon.ToTitleCase())
                            .WithDescription(desc)
                            .WithThumbnailUrl(thumbnail)
                            .AddField(efb => efb.WithName(GetText("types")).WithValue(string.Join("\n", poktypes)).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("height_weight")).WithValue(GetText("height_weight_val", height, weight)).WithIsInline(true))
                            .AddField(efb => efb.WithName(GetText("abilities")).WithValue(string.Join("\n", abilities)).WithIsInline(true))).ConfigureAwait(false);
                return;
                
            }

            [NadekoCommand, Usage, Description, Aliases]
            public async Task PokemonAbility([Leftover] string ability = null)
            {
                ability = ability?.Trim().ToLowerInvariant().Replace(" ", "", StringComparison.InvariantCulture);
                if (string.IsNullOrWhiteSpace(ability))
                    return;
                Ability pokability = await _service.GetAbility(ability);
                if (pokability == null){
                    await ReplyErrorLocalizedAsync("pokemon_ability_none").ConfigureAwait(false);
                    return;
                }
                string desc="";
                string shortdesc="";
                foreach(var p in pokability.EffectEntries){
                    if (p.Language.Name == "en"){
                    desc = p.Effect;
                    shortdesc = p.ShortEffect;
                    break;
                }
                }
                await ctx.Channel.EmbedAsync(new EmbedBuilder().WithOkColor()
                        .WithTitle(ability.ToTitleCase())
                        .WithDescription(string.IsNullOrWhiteSpace(desc)
                                ? shortdesc
                                : desc)
                        ).ConfigureAwait(false);
                return;
               
                
            }
        }
    }
}