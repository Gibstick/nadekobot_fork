using NadekoBot.Core.Services;
using System;
using System.Collections.Concurrent;

namespace NadekoBot.Core.Common
{
    public class DownloadTracker : INService
    {
        public ConcurrentDictionary<ulong, DateTime> LastDownloads { get; } = new ConcurrentDictionary<ulong, DateTime>();
    }
}
