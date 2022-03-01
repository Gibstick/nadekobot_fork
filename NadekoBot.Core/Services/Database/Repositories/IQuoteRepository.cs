using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NadekoBot.Core.Services.Database.Repositories
{
    public interface IQuoteRepository : IRepository<Quote>
    {
        Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword);
        Task<Quote> GetRandomQuoteAsync(ulong guildId);
        Task<Quote> SearchQuoteKeywordTextAsync(ulong guildId, string keyword, string text);
        IEnumerable<Quote> GetGroup(ulong guildId, int page, OrderType order);
        int GetGroupCount(ulong guildId);
        IEnumerable<Quote> SearchQuoteKeywordKeyTextAsync(ulong guildId, string keyword,int page);
        int SearchQuoteKeywordKeyTextCount(ulong guildId, string keyword);
        IEnumerable<Quote> SearchQuoteAuthorTextAsync(ulong guildId, ulong Authorid,int page);
        int SearchQuoteAuthorTextCount(ulong guildId, ulong Authorid);
        IEnumerable<Quote> SearchQuoteLinkTextAsync(ulong guildId);
        IEnumerable<string> SearchDistinctQuoteKeywordAsync(ulong guildId, string keyword);
        void RemoveAllByKeyword(ulong guildId, string keyword);
        void RemoveAllByAuthor(ulong guildId, ulong Authorid);
    }
}
