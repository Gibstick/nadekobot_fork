using System.Globalization;

namespace NadekoBot.Core.Services
{
    public interface IBotStrings
    {
        public string GetText(string key, ulong? guildId = null, params object[] data);
        public string GetText(string key, CultureInfo locale, params object[] data);
        void Reload();
    }
}
