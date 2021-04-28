using System.Threading.Tasks;

namespace NadekoBot.Core.Common
{
    public interface IPub
    {
        public Task Pub<TData>(in TypedKey<TData> key, TData data);
    }
}