using System.Collections.Generic;

namespace NadekoBot.Core.Modules.Searches.Common
{
    public class OmdbSearch
    {
        public List<OmdbObj> Search { get; set; }
        public string totalResults { get; set; }
        public string Response {get; set;}
    }

    public class OmdbObj
    {
        public string imdbID {get; set;}
    }
}
