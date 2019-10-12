using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using Newtonsoft.Json;
namespace MMMonitor
{
    static class Fetcher
    {
        const string APP_ID = "9215170717109877eac3240ae6393ed8";

        private static string HttpGet(string url)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            using (var client = new HttpClient())
            {
                var firstResponse = client.GetAsync(url).Result;
                if (firstResponse.IsSuccessStatusCode)
                    return firstResponse.Content.ReadAsStringAsync().Result;
                return null;
            }
        }

        public static Player getPlayer(string name)
        {
            string ID = "";

            string responseString = HttpGet("https://api.worldofwarships.com/wows/account/list/?application_id=" + APP_ID + "&search=" + name);
            if (responseString == null)
                return null;
            dynamic data = JsonConvert.DeserializeObject(responseString);
            ID = (string)data.data[0].account_id;

            responseString = HttpGet("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID);
            responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
            if (responseString == null)
                return null;
            dynamic output = JsonConvert.DeserializeObject(responseString);
            return new Player
            {
                winrate = output.meta.hidden == null ? ((double)output.data.ID.statistics.pvp.wins / (double)output.data.ID.statistics.pvp.battles) : 0,
                numGames = output.meta.hidden == null ? (int)output.data.ID.statistics.pvp.battles : 0,
                userName = (string)output.data.ID.nickname,
                ID = ID
            };
        }
        
        public static Dictionary<string, string> getShipDict(string configDir, bool forceReload = false)
        {
            string filePath = Path.Combine(configDir, "ships.json");
            if (!forceReload && File.Exists(filePath))
                return JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(filePath));
            
            Dictionary<string, string> result = new Dictionary<string, string>();
            int page = 1;
            while(true)
            {
                string resp = HttpGet("https://api.worldofwarships.com/wows/encyclopedia/ships/?application_id=" + APP_ID + "&fields=name&page_no=" + page.ToString());
                dynamic output = JsonConvert.DeserializeObject(resp);
                Dictionary<string, Dictionary<string, string>> dict = output.data.ToObject<Dictionary<string, Dictionary<string, string>>>();
                Dictionary<string, string> dict2 = dict.ToDictionary(x => x.Key, x => x.Value["name"]);
                result = result.Concat(dict2).ToDictionary(x => x.Key, x => x.Value);
                if (page++ >= (int)output.meta.page_total)
                    break;
            }
            File.WriteAllText(filePath, JsonConvert.SerializeObject(result));
            return result;
        }
    }

    public class Player
    {
        public double winrate { get; set; }
        public string userName { get; set; }
        public string ID { get; set; }
        public string ship { get; set; }
        public int relation { get; set; }
        public int numGames { get; set; }
        public Player()
        {
            winrate = 0;
            userName = "";
            ID = "";
            numGames = 0;
            relation = 0;
        }
    }
}
