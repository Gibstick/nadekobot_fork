using NadekoBot.Core.Services.Database.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NadekoBot.Common;

namespace NadekoBot.Core.Services.Database.Repositories.Impl
{
    public class QuoteRepository : Repository<Quote>, IQuoteRepository
    {
        public QuoteRepository(DbContext context) : base(context)
        {
        }

        public IEnumerable<Quote> GetGroup(ulong guildId, int page, OrderType order)
        {
            var q = _set.AsQueryable().Where(x => x.GuildId == guildId);
            if (order == OrderType.Keyword){
                q = q.OrderBy(x => x.Keyword);
            }
            else if (order == OrderType.AuthorName){
                q = q.OrderBy(x => x.AuthorName);
            }
            else{
                q = q.OrderBy(x => x.Id);
            }

            return q.Skip(15 * page).Take(15).ToArray();
        }

        public int GetGroupCount(ulong guildId){
            return _set.AsQueryable().Where(x => x.GuildId == guildId).Count();
        }

        public async Task<Quote> GetRandomQuoteByKeywordAsync(ulong guildId, string keyword)
        {
            var rng = new NadekoRandom();
            return (await _set.AsQueryable()
                .Where(q => q.GuildId == guildId && q.Keyword == keyword)
                .ToListAsync())
                .OrderBy(q => rng.Next())
                .FirstOrDefault();
        }

        public async Task<Quote> GetRandomQuoteAsync(ulong guildId)
        {
            var rng = new NadekoRandom();
            return (await _set.AsQueryable()
                .Where(q => q.GuildId == guildId)
                .ToListAsync())
                .OrderBy(q => rng.Next())
                .FirstOrDefault();
        }

        public async Task<Quote> SearchQuoteKeywordTextAsync(ulong guildId, string keyword, string text)
        {
            var rngk = new NadekoRandom();
            return (await _set.AsQueryable()
                .Where(q => q.GuildId == guildId
                            && q.Keyword == keyword
                            && EF.Functions.Like(q.Text.ToUpper(), $"%{text.ToUpper()}%")
                            // && q.Text.Contains(text, StringComparison.OrdinalIgnoreCase)
                            )
                .ToListAsync())
                .OrderBy(q => rngk.Next())
                .FirstOrDefault();
        }

        public IEnumerable<Quote> SearchQuoteKeywordKeyTextAsync(ulong guildId, string keyword,int page)
        {
            var q = _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                            && EF.Functions.Like(x.Keyword.ToUpper(), $"%{keyword.ToUpper()}%")
                           );
                
            q =  q.OrderBy(x => x.Keyword);
            return  q.Skip(15*page).Take(15).ToArray();
            
        }

        public IEnumerable<string> SearchDistinctQuoteKeywordAsync(ulong guildId, string keyword)
        {
            var q = _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                            && x.Keyword.StartsWith(keyword)
                           );
            return q.Select(z=>z.Keyword).Distinct().ToList();
            
        }
        public int SearchQuoteKeywordKeyTextCount(ulong guildId, string keyword){
            return _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                            && EF.Functions.Like(x.Keyword.ToUpper(), $"%{keyword.ToUpper()}%")
                           ).Count();
        }

        public IEnumerable<Quote> SearchQuoteLinkTextAsync(ulong guildId)
        {
            var q = _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                            && EF.Functions.Like(x.Text, $"http%")
                           );
                
            return  q.ToArray();
            
        }

        public IEnumerable<Quote> SearchQuoteAuthorTextAsync(ulong guildId, ulong Authorid,int page)
        {
            var q = _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                    && x.AuthorId == Authorid);
                
            q =  q.OrderBy(x => x.AuthorName);
            return  q.Skip(15*page).Take(15).ToArray();
            
        }
        public int SearchQuoteAuthorTextCount(ulong guildId, ulong Authorid){
            return _set.AsQueryable()
                .Where(x => x.GuildId == guildId
                    && x.AuthorId == Authorid).Count();
        }

        public void RemoveAllByKeyword(ulong guildId, string keyword)
        {
            _set.RemoveRange(_set.AsQueryable().Where(x => x.GuildId == guildId && x.Keyword.ToUpper() == keyword));
        }

        public void RemoveAllByAuthor(ulong guildId, ulong Authorid)
        {
            _set.RemoveRange(_set.AsQueryable().Where(x => x.GuildId == guildId && x.AuthorId == Authorid));
        }

    }
}
