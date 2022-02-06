using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
namespace NadekoBot.Modules.Searches
{
    public class BibleAutoCompleteHandler : AutocompleteHandler
    {
        public IEnumerable<string> books = new List<string>(){"Genesis","Exodus","Leviticus","Numbers","Deuteronomy","Joshua","Judges",
        "Ruth","1Samuel","2Samuel","1Kings","2Kings","1Chronicles","2Chronicles","Ezra","Nehemiah","Esther",
        "Job","Psalms","Proverbs","Ecclesiastes","Song","Isaiah","Jeremiah","Lamentations","Ezekiel","Daniel","Hosea","Joel","Amos",
        "Obadiah","Jonah","Micah","Nahum","Habakkuk","Zephaniah","Haggai","Zechariah","Malachi","Matthew","Mark",
        "Luke","John","Acts","Romans","1Corinthians","2Corinthians","Galatians","Ephesians","Philippians",
        "Colossians","1Thessalonians","2Thessalonians","1Timothy","2Timothy","Titus","Philemon","Hebrews","James","1Peter",
        "2Peter","1John","2John","3John","Jude","Revelation"};
        public override Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction, IParameterInfo parameter, IServiceProvider services){
            try
            {
                var value = autocompleteInteraction.Data.Current.Value as string;

                if (string.IsNullOrEmpty(value))
                    return Task.FromResult(AutocompletionResult.FromSuccess());
                
                var matches = books.Where(x=>x.ToLowerInvariant().StartsWith(value.ToLowerInvariant())).Take(20).ToList();
                
                return Task.FromResult(AutocompletionResult.FromSuccess(matches.Select(x => new AutocompleteResult(x, x)))); 
            }
            catch (System.Exception ex)
            {
                return Task.FromResult(AutocompletionResult.FromError(ex));
            }
        }
    }
}