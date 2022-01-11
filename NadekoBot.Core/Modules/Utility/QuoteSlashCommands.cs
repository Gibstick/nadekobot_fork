using Discord;
using Discord.Interactions;
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

        public class QuoteSlashCommands : NadekoSlashSubmodule
        {
            private readonly DbService _db;
            private readonly IHttpClientFactory _httpFactory;

            public QuoteSlashCommands(DbService db,IHttpClientFactory factory)
            {
                _db = db;
                _httpFactory = factory;
            }


            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task ListQuotes([Summary("page","page number")][MinValue(1)]int page = 1,
            [Summary("order","Sorting Column")] OrderType order = OrderType.Keyword)
            {
                page -= 1;
                if (page < 0)
                    return;
                int quotecount;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext()){
                    quotecount = uow.Quotes.GetGroupCount(ctx.Guild.Id);
                }
                await ctx.SendScrollingButtonAsync(page,(cur)=>{
                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.GetGroup(ctx.Guild.Id, cur, order);
                }

                if (quotes.Any()){
                    return new EmbedBuilder()
                    .WithOkColor()
                    .WithDescription(string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                    .WithTitle(GetText("quotes_page", cur + 1));
                }else{
                    return new EmbedBuilder()
                    .WithErrorColor()
                    .WithDescription(Format.Bold(ctx.User.ToString()) + " " + GetText("quotes_page_none"));
                    
                }},quotecount,15);

            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuotePrint([Summary("keyword","Name of the quote")]string keyword)
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote quote;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    quote = await uow.Quotes.GetRandomQuoteByKeywordAsync(ctx.Guild.Id, keyword);
                }

                if (quote == null){
                    await ReplyErrorLocalizedAsync("quotes_notfound_key");
                    return;
                }

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await ctx.Interaction.EmbedAsync(crembed.ToEmbed(), $"`#{quote.Id}` 📣 " + crembed.PlainText?.SanitizeAllMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await ctx.Interaction.ModifyOriginalResponseAsync(x=>x.Content = $"`#{quote.Id}` 📣 " + rep.Replace(quote.Text)?.SanitizeAllMentions()).ConfigureAwait(false);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteRandom()
            {
            
            
                Quote quote;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    quote = await uow.Quotes.GetRandomQuoteAsync(ctx.Guild.Id);
                }

                if (quote == null){
                    await ReplyErrorLocalizedAsync("quote_no_found_id");
                    return;
                }

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);
                    await ctx.Interaction.EmbedAsync(crembed.ToEmbed(), $"`#{quote.Id} {quote.Keyword}` 📣 " + crembed.PlainText?.SanitizeAllMentions() ?? "")
                        .ConfigureAwait(false);
                    return;
                }
                await ctx.Interaction.ModifyOriginalResponseAsync(x=>{
                    x.Content = $"`#{quote.Id} {quote.Keyword}` 📣 " + rep.Replace(quote.Text)?.SanitizeAllMentions();
                }).ConfigureAwait(false);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteShow([Summary("id","Quote Id number")][MinValue(1)]int id)
            {
                Quote quote;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    quote = uow.Quotes.GetById(id);
                    if (quote == null || quote.GuildId != Context.Guild.Id){
                        await ReplyErrorLocalizedAsync("quotes_notfound");
                        return;
                    }
                }
                await ShowQuoteData(quote);
            }

            private async Task ShowQuoteData(Quote data)
            {
                await ctx.Interaction.EmbedAsync(new EmbedBuilder()
                    .WithOkColor()
                    .WithTitle(GetText("quote_id", $"#{data.Id}"))
                    .AddField(efb => efb.WithName(GetText("trigger")).WithValue(data.Keyword))
                    .AddField(efb => efb.WithName(GetText("response")).WithValue(data.Text.Length > 1000
                        ? GetText("redacted_too_long")
                        : Format.Sanitize(data.Text)))
                    .WithFooter(GetText("created_by", $"{data.AuthorName} ({data.AuthorId})"))
                ).ConfigureAwait(false);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearch([Summary("keyword","Quote keyword")]string keyword,
             [Summary("text","Searching text")] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;

                keyword = keyword.ToUpperInvariant();

                Quote keywordquote;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    keywordquote = await uow.Quotes.SearchQuoteKeywordTextAsync(ctx.Guild.Id, keyword, text);
                }

                if (keywordquote == null){
                    await ReplyErrorLocalizedAsync("quotes_notfound_key");
                    return;

                }

                await ctx.Interaction.ModifyOriginalResponseAsync(x=> x.Content = $"`#{keywordquote.Id}` 💬 " + keyword.ToLowerInvariant() + ":  " +
                                                       keywordquote.Text.SanitizeAllMentions()).ConfigureAwait(false);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteSearchKey([Summary("keyword","quote keyword")]string keyword,
            [Summary("page","search results page")][MinValue(1)]int page=1)
            {
                page -= 1;
                if (page < 0)
                    return;

                if (string.IsNullOrWhiteSpace(keyword))
                    return;

                keyword = keyword.ToUpperInvariant();
                int quotescount;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext()){
                    quotescount = uow.Quotes.SearchQuoteKeywordKeyTextCount(ctx.Guild.Id,keyword);
                }

                
                await ctx.SendScrollingButtonAsync(page,(cur)=>{
                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.SearchQuoteKeywordKeyTextAsync(ctx.Guild.Id, keyword,cur);
                }
                if (quotes.Any()){
                    return new EmbedBuilder()
                    .WithOkColor()
                    .WithDescription(string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                    .WithTitle(GetText("quotes_page", cur + 1));
                }else{
                    return new EmbedBuilder()
                    .WithErrorColor()
                    .WithDescription(Format.Bold(ctx.User.ToString()) + " " + GetText("quotes_page_none"));
                    
                }

                },quotescount,15);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteAuthor([Summary("user","Author of quotes")]IGuildUser usr = null,
            [Summary("page","page number")][MinValue(1)]int page =1)
            {
                --page;
                if (page <0){
                    return;
                }

                if (usr == null){
                usr = (IGuildUser)ctx.User;
                }

                int quotescount;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext()){
                    quotescount = uow.Quotes.SearchQuoteAuthorTextCount(ctx.Guild.Id,usr.Id);
                }

                await ctx.SendScrollingButtonAsync(page,(cur)=>{
                IEnumerable<Quote> quotes;
                using (var uow = _db.GetDbContext())
                {
                    quotes = uow.Quotes.SearchQuoteAuthorTextAsync(ctx.Guild.Id, usr.Id,cur);
                }

                if (quotes.Any()){
                    return new EmbedBuilder()
                    .WithOkColor()
                    .WithDescription(string.Join("\n", quotes.Select(q => $"`#{q.Id}` {Format.Bold(q.Keyword.SanitizeAllMentions()),-20} by {q.AuthorName.SanitizeAllMentions()}")))
                    .WithTitle(GetText("quotes_page", cur + 1));
                }else{
                    return new EmbedBuilder()
                    .WithErrorColor()
                    .WithDescription(Format.Bold(ctx.User.ToString()) + " " + GetText("quotes_page_none"));
                    
                }

                },quotescount,15);

            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteId([Summary("id","Quote id")][MinValue(1)]int id)
            {
                if (id < 0)
                    return;

                Quote quote;

                var rep = new ReplacementBuilder()
                    .WithDefault(Context)
                    .Build();
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    quote = uow.Quotes.GetById(id);
                }

                if (quote is null || quote.GuildId != ctx.Guild.Id)
                {
                    await ctx.Interaction.SendErrorAsync(GetText("quotes_notfound")).ConfigureAwait(false);
                    return;
                }

                var infoText = $"`#{quote.Id} added by {quote.AuthorName.SanitizeAllMentions()}` 🗯️ " + quote.Keyword.ToLowerInvariant().SanitizeAllMentions() + ":\n";

                if (CREmbed.TryParse(quote.Text, out var crembed))
                {
                    rep.Replace(crembed);

                    await ctx.Interaction.EmbedAsync(crembed.ToEmbed(), infoText + crembed.PlainText?.SanitizeAllMentions())
                        .ConfigureAwait(false);
                }
                else
                {
                    await ctx.Interaction.ModifyOriginalResponseAsync(x=>x.Content=infoText + rep.Replace(quote.Text)?.SanitizeAllMentions())
                        .ConfigureAwait(false);
                }
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteAdd([Summary("keyword","Name of quote")]string keyword, [Summary("text","Content of Quote")] string text)
            {
                if (string.IsNullOrWhiteSpace(keyword) || string.IsNullOrWhiteSpace(text))
                    return;
            
                keyword = keyword.ToUpperInvariant();
            
                Quote q;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    uow.Quotes.Add(q = new Quote
                    {
                        AuthorId = ctx.User.Id,
                        AuthorName = ctx.User.Username,
                        GuildId = ctx.Guild.Id,
                        Keyword = keyword,
                        Text = text,
                    });
                    await uow.SaveChangesAsync();
                }
                await ReplyConfirmLocalizedAsync("quote_added_new", Format.Code(q.Id.ToString())).ConfigureAwait(false);
            }

            [NadekoSlash]
            [RequireContext(ContextType.Guild)]
            public async Task QuoteDelete([Summary("id","quote id")]int id)
            {
                var isAdmin = ((IGuildUser)ctx.User).GuildPermissions.Administrator;

                var success = false;
                string response;
                await ctx.Interaction.DeferAsync().ConfigureAwait(false);
                using (var uow = _db.GetDbContext())
                {
                    var q = uow.Quotes.GetById(id);

                    if ((q?.GuildId != ctx.Guild.Id) || (!isAdmin && q.AuthorId != ctx.User.Id))
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
                    await ctx.Interaction.SendConfirmAsync(response).ConfigureAwait(false);
                else
                    await ctx.Interaction.SendErrorAsync(response).ConfigureAwait(false);
            }
        }
    }
}
