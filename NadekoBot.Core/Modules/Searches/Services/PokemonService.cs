using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;
using PokeApiNet;
using System.Collections;

namespace NadekoBot.Modules.Searches.Services
{
    public class PokemonService : INService
    {
        private readonly PokeApiClient _client;
        
        public PokemonService()
        {
            
            _client =  new PokeApiClient();
        }

        public async Task<Pokemon> GetPokemon(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }
            try{
                Pokemon pok = await _client.GetResourceAsync<Pokemon>(name);
                return pok;

            }catch (Exception e){
                return null;

            }

        }

        public async Task<string> GetPokemonDescription(Pokemon pok){
             var stats = new Dictionary<string, int>();
             foreach (var stat in pok.Stats){
                stats.Add(stat.Stat.Name,stat.BaseStat);
            }

            return $@"💚**HP:**  {stats["hp"],-4} ⚔**ATK:** {stats["attack"],-4} 🛡**DEF:** {stats["defense"],-4}
✨**SPA:** {stats["special-attack"],-4} 🎇**SPD:** {stats["special-defense"],-4} 💨**SPE:** {stats["speed"],-4}";
        

        }

        public async Task<string> GetPokemonThumbnail(Pokemon pok){
            var pokid = pok.Id;
            return $"https://raw.githubusercontent.com/HybridShivam/Pokemon/master/assets/thumbnails-compressed/{pokid.ToString("D3")}.png";

        }

        public async Task<List<string>> GetPokemonTypes(Pokemon pok){
            List<string> poktypes = new List<string>();
            foreach (var x in pok.Types){
                poktypes.Add(x.Type.Name);
            }
            return poktypes;
        }

        public async Task<List<string>> GetPokemonAbilites(Pokemon pok){
            List<string> pokabilites = new List<string>();
            foreach (var y in pok.Abilities){
                pokabilites.Add(y.Ability.Name);

            }
            return pokabilites;

        }

        public async Task<Ability> GetAbility(string ability)
        {
            if (string.IsNullOrWhiteSpace(ability))
            {
                return null;
            }
            try{
                Ability pokability = await _client.GetResourceAsync<Ability>(ability);
                return pokability;

            }catch (Exception e){
                return null;

            }

        }


    }
}
