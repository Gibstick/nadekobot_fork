using System;
using System.Threading.Tasks;

namespace NadekoBot.Core.Common
{
    public interface ISub
    {
        public Task Sub<TData>(in TypedKey<TData> key, Func<TData, Task> action);
    }
}