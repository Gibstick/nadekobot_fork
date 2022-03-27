using System.Threading.Tasks;
using System.IO;
using System;

namespace NadekoBot.Modules.Searches
{

    public record ImageData(string Extension, Stream FileData) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
            => FileData.DisposeAsync();
    }
}