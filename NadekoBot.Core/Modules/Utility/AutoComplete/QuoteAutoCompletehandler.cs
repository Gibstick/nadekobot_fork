using Discord;
using Discord.Interactions;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Serilog;
namespace NadekoBot.Modules.Utility
{
    public class QuoteAutoCompleteHandler : AutocompleteHandler
    {
        IEnumerable<string> keywords = null;

        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext ctx, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services){
            try
            {
                var value = autocompleteInteraction.Data.Current.Value as string;
                value = value.ToUpperInvariant();
                // No suggestions for strings less than 3 characters
                if ((string.IsNullOrEmpty(value)) || (value.Length < 3)){
                    return Task.FromResult(AutocompletionResult.FromSuccess());
                }
                if ((keywords == null) || (value.Length == 3)){
                    // query for suggestions
                    DbService db = (DbService)services.GetService(typeof(DbService));
                    using (var uow = db.GetDbContext()){
                        keywords = uow.Quotes.SearchDistinctQuoteKeywordAsync(ctx.Guild.Id,value);
                    }
                }
                
                var matches = keywords.Where(x=>x.StartsWith(value)).Take(25).ToList();
                return Task.FromResult(AutocompletionResult.FromSuccess(matches.Select(x => new AutocompleteResult(x, x)))); 
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(AutocompletionResult.FromError(ex));
            }
        }
    }
}