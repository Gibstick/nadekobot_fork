using Discord;
using Discord.Commands;
using NadekoBot.Common;
using NadekoBot.Common.Attributes;
using NadekoBot.Common.Replacements;
using NadekoBot.Core.Services;
using NadekoBot.Core.Services.Database.Models;
using NadekoBot.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Net.Http;
using Serilog;

namespace NadekoBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class QuoteCommands : NadekoSubmodule
        {
            private readonly DbService _db;
            private readonly IHttpClientFactory _httpFactory;

            public QuoteCommands(DbService db,IHttpClientFactory factory)
            {
                _db = db;
                _httpFactory = factory;
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(1)]
            public Task ListQuotes(OrderType order = OrderType.Keyword)
                => ListQuotes(1, order);

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [Priority(0)]
            public async Task ListQuotes(int page = 1, OrderType order = OrderType.Keyword)
            {
                page -= 1;
                if (page < 0)
                    return;

                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.GetGroup(ctx.Guild.Id, page, order);
                }

                if (quotes.Any())
                    await ctx.Channel.SendConfirmAsync(GetText("quotes_page", page + 1),
                            string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                        .ConfigureAwait(false);
                else
                    await ReplyErrorLocalizedAsync("quotes_page_none").ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuotePrint([Leftover] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote quote;
                using (var uow = _db.GetDbContext())
                {
                    quote = await uow.Quotes.GetRandomQuoteByKeywordAsync(ctx.Guild.Id, keyword);
                    //if (quote != null)
                    //{
                    //    quote.UseCount += 1;
                    //    uow.Complete();
                    //}
                }

                if (quote == null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await ctx.Channel.EmbedAsync(crembed.ToEmbed(), $"`#{quote.Id}` 📣 " + crembed.PlainText?.SanitizeAllMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await ctx.Channel.SendMessageAsync($"`#{quote.Id}` 📣 " + rep.Replace(quote.Text)?.SanitizeAllMentions()).ConfigureAwait(false);
            }
            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteRandom()
            {
            
            
                Quote quote;
                using (var uow = _db.GetDbContext())
                {
                    quote = await uow.Quotes.GetRandomQuoteAsync(ctx.Guild.Id);
                }

                if (quote == null)
                    return;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await ctx.Channel.EmbedAsync(crembed.ToEmbed(), $"`#{quote.Id} {quote.Keyword}` 📣 " + crembed.PlainText?.SanitizeAllMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await ctx.Channel.SendMessageAsync($"`#{quote.Id} {quote.Keyword}` 📣 " + rep.Replace(quote.Text)?.SanitizeAllMentions()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteShow(int id)
            {
                Quote quote;
                using (var uow = _db.GetDbContext())
                {
                    quote = uow.Quotes.GetById(id);
                    if (quote.GuildId != Context.Guild.Id)
                        quote = null;
                }

                if (quote is null)
                {
                    await ReplyErrorLocalizedAsync("quote_no_found_id");
                    return;
                }

                await ShowQuoteData(quote);
            }

            private async Task ShowQuoteData(Quote data)
            {
                await ctx.Channel.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("quote_id", $"#{data.Id}"))
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(data.Keyword))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(data.Text.Length > 1000
                        ? GetText("redacted_too_long")
                        : Format.Sanitize(data.Text)))
                    .WithFooter(GetText("created_by", $"{data.AuthorName} ({data.AuthorId})"))
                ).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch(string keyword, [Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote keywordquote;
                using (var uow = _db.GetDbContext())
                {
                    keywordquote = await uow.Quotes.SearchQuoteKeywordTextAsync(ctx.Guild.Id, keyword, text);
                }

                if (keywordquote == null)
                    return;

                await ctx.Channel.SendMessageAsync($"`#{keywordquote.Id}` 💬 " + keyword.ToLowerInvariant() + ":  " +
                                                       keywordquote.Text.SanitizeAllMentions()).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearchKey(string keyword)
            {

                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.SearchQuoteKeywordKeyTextAsync(ctx.Guild.Id, keyword);
                }

                if (quotes.Any()){
                    await ctx.Channel.SendConfirmAsync(GetText("quotes_page",1),
                            string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                        .ConfigureAwait(false);
                }
                else{
                    await ReplyErrorLocalizedAsync("quotes_page_none").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteAuthor(IGuildUser usr = null,int page =1)
            {
                --page;
                if (page <0){
                    return;
                }

                if (usr == null){
                usr = (IGuildUser)ctx.User;
                }

                
                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.SearchQuoteAuthorTextAsync(ctx.Guild.Id, usr.Id,page);
                }

                if (quotes.Any()){
                    await ctx.Channel.SendConfirmAsync(GetText("quotes_page",page+1),
                            string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                        .ConfigureAwait(false);
                }
                else{
                    await ReplyErrorLocalizedAsync("quotes_page_none").ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteId(int id)
            {
                if (id < 0)
                    return;

                Quote quote;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                using (var uow = _db.GetDbContext())
                {
                    quote = uow.Quotes.GetById(id);
                }

                if (quote is null || quote.GuildId != ctx.Guild.Id)
                {
                    await ctx.Channel.SendErrorAsync(GetText("quotes_notfound")).ConfigureAwait(false);
                    return;
                }

                var infoText = $"`#{quote.Id} added by {quote.AuthorName.SanitizeAllMentions()}` 🗯️ " + quote.Keyword.ToLowerInvariant().SanitizeAllMentions() + ":\n";

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);

                    await ctx.Channel.EmbedAsync(crembed.ToEmbed(), infoText + crembed.PlainText?.SanitizeAllMentions())
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Channel.SendMessageAsync(infoText + rep.Replace(quote.Text)?.SanitizeAllMentions())
                        .ConfigureAwait(false);
                }
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteAdd(string keyword, [Leftover] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;
            
                keyword = keyword.ToUpperInvariant();
            
                Quote q;
                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.Add(q = new Quote
                    {
                        AuthorId = ctx.Message.Author.Id,
                        AuthorName = ctx.Message.Author.Username,
                        GuildId = ctx.Guild.Id,
                        Keyword = keyword,
                        Text = text,
                    });
                    await uow.SaveChangesAsync();
                }
                await ReplyConfirmLocalizedAsync("quote_added_new", Format.Code(q.Id.ToString())).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteDelete(int id)
            {
                var isAdmin = ((IGuildUser)ctx.Message.Author).GuildPermissions.Administrator;

                var success = false;
                string response;
                using (var uow = _db.GetDbContext())
                {
                    var q = uow.Quotes.GetById(id);

                    if ((q?.GuildId != ctx.Guild.Id) || (!isAdmin && q.AuthorId != ctx.Message.Author.Id))
                    {
                        response = GetText("quotes_remove_none");
                    }
                    else
                    {
                        uow.Quotes.Remove(q);
                        await uow.SaveChangesAsync();
                        success = true;
                        response = GetText("quote_deleted", id);
                    }
                }
                if (success)
                    await ctx.Channel.SendConfirmAsync(response).ConfigureAwait(false);
                else
                    await ctx.Channel.SendErrorAsync(response).ConfigureAwait(false);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task QuoteDeleteAuthor(IGuildUser usr)
            {
                
                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.RemoveAllByAuthor(ctx.Guild.Id, usr.Id);
                    await uow.SaveChangesAsync();
        
                }
                var text = $"Deleted all quotes from {Format.Bold(usr.Username.SanitizeAllMentions())}";
                await ctx.Channel.SendConfirmAsync(Format.Bold(ctx.User.ToString()) + " " + text);
            }

            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task QuoteDeleteLinks()
            {
                int badlinks = 0;
                //get all links from database
                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.SearchQuoteLinkTextAsync(ctx.Guild.Id);


                    foreach(var q in quotes){
                        // check if valid url
                        Uri uriResult;
                        bool result = Uri.TryCreate(q.Text, UriKind.Absolute, out uriResult) && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
                        if (!result) {
                            continue;
                        }
                        using (var _http = _httpFactory.CreateClient("QuotesClient")) {
                            //check if link is dead
                            try {
                                await _http.GetStringAsync(q.Text);
                            }
                            catch (HttpRequestException ex) {
                                uow.Quotes.Remove(q);
                                await uow.SaveChangesAsync();
                                badlinks = badlinks+1;
                            }
                        }
                    }
                }

                await ctx.Channel.SendConfirmAsync(Format.Bold(ctx.User.ToString()) + " " + $"Successfully removed {badlinks.ToString()} dead links");
            }




            [NadekoCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [UserPerm(GuildPerm.Administrator)]
            public async Task DelAllQuotes([Leftover] string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.RemoveAllByKeyword(ctx.Guild.Id, keyword.ToUpperInvariant());

                    await uow.SaveChangesAsync();
                }

                await ReplyConfirmLocalizedAsync("quotes_deleted", Format.Bold(keyword.SanitizeAllMentions())).ConfigureAwait(false);
            }
        }
    }
}
