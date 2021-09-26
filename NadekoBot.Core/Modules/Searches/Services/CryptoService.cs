using NadekoBot.Core.Modules.Searches.Common;
using NadekoBot.Core.Services;
using NadekoBot.Extensions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace NadekoBot.Core.Modules.Searches.Services
{
    public class CryptoService : INService
    {
        private readonly IDataCache _cache;
        private readonly IHttpClientFactory _httpFactory;
        private readonly IBotCredentials _creds;
        
        public CryptoService(IDataCache cache, IHttpClientFactory httpFactory, IBotCredentials creds)
        {
            _cache = cache;
            _httpFactory = httpFactory;
            _creds = creds;
        }

        public async Task<(Dictionary<string,CryptoResponseData> Data, Dictionary<string,CryptoResponseData> Nearest)> GetCryptoData(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return (null, null);
            }

            name = name.ToUpperInvariant();
            var crypto = await CryptoData(name).ConfigureAwait(false);
            return (crypto, null);
        }

        private readonly SemaphoreSlim getCryptoLock = new SemaphoreSlim(1, 1);
        public async Task<Dictionary<string,CryptoResponseData>> CryptoData(string name)
        {
            await getCryptoLock.WaitAsync();
            try
            {
                    try
                    {
                        using (var _http = _httpFactory.CreateClient())
                        {   
                            //check by slug name
                            Uri urival = new Uri($"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?" +
                                $"CMC_PRO_API_KEY={_creds.CoinmarketcapApiKey}" +
                                $"&slug={name.ToLowerInvariant()}" +
                                $"&convert=USD");
                            var strData = await _http.GetStringAsync(urival);
                            //check for match
                            return JsonConvert.DeserializeObject<CryptoResponse>(strData).Data;

                            }
                    }
                    

                    catch (Exception ex)
                    {
                        if (ex is HttpRequestException){
                            try{
                                // check using symbol
                                using (var _http = _httpFactory.CreateClient()){
                                Uri urival2 = new Uri($"https://pro-api.coinmarketcap.com/v1/cryptocurrency/quotes/latest?" +
                                $"CMC_PRO_API_KEY={_creds.CoinmarketcapApiKey}" +
                                $"&symbol={name}" +
                                $"&convert=USD");
                                var strData2 = await _http.GetStringAsync(urival2);
                                JsonConvert.DeserializeObject<CryptoResponse>(strData2); // just to see if its' valid
                                return JsonConvert.DeserializeObject<CryptoResponse>(strData2).Data;
                            }
                            }catch(Exception exc){
                                Log.Error(exc, "Error getting crypto data: {Message}", exc.Message);
                                return default;
                            }

                        }else{
                            Log.Error(ex, "Error getting crypto data: {Message}", ex.Message);

                            return default;

                        }
                    }

            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error retreiving crypto data: {Message}", ex.Message);
                return default;
            }
            finally
            {
                getCryptoLock.Release();
            }
        }
    }
}
