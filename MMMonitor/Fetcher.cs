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

        public static Player getPlayer(string name, string shipID)
        {
            //Use name to get player ID
            string responseString = HttpGet("https://api.worldofwarships.com/wows/account/list/?application_id=" + APP_ID + "&search=" + name);
            if (responseString == null)
                return null;
            dynamic data = JsonConvert.DeserializeObject(responseString);
            if ((int)data.meta.count == 0)
                return null;
            string ID = (string)data.data[0].account_id;

            //Query for account stats
            responseString = HttpGet("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID);
            if (responseString == null)
                return null;
            responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
            dynamic output = JsonConvert.DeserializeObject(responseString);

            //Query for ship stats
            responseString = HttpGet("https://api.worldofwarships.com/wows/ships/stats/?application_id=" + APP_ID + "&account_id=" + ID + "&ship_id=" + shipID);
            responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
            dynamic shipsStuff = JsonConvert.DeserializeObject(responseString);

            return new Player
            {
                winrate = (output.meta.hidden == null && output.data.ID != null) ? ((double)output.data.ID.statistics.pvp.wins / (double)output.data.ID.statistics.pvp.battles) : 0,
                numGames = (output.meta.hidden == null && output.data.ID != null) ? (int)output.data.ID.statistics.pvp.battles : 0,
                userName = (string)output.data.ID.nickname,
                ID = ID,
                shipGames = (output.meta.hidden == null && shipsStuff.data.ID != null) ? (int)shipsStuff.data.ID[0].pvp.battles : 0,
                shipWr = (output.meta.hidden == null && shipsStuff.data.ID != null && (int)shipsStuff.data.ID[0].pvp.battles > 0) ? ((double)shipsStuff.data.ID[0].pvp.wins/(int)shipsStuff.data.ID[0].pvp.battles) : 0
            };
        }
        
        public static Dictionary<string, Ship> getShipDict(string configDir, bool forceReload = false)
        {
            string filePath = Path.Combine(configDir, "ships.json");
            if (!forceReload && File.Exists(filePath))
                return JsonConvert.DeserializeObject<Dictionary<string, Ship>>(File.ReadAllText(filePath));
            
            Dictionary<string, Ship> result = new Dictionary<string, Ship>();
            int page = 1;
            while(true)
            {
                string resp = HttpGet("https://api.worldofwarships.com/wows/encyclopedia/ships/?application_id=" + APP_ID + "&fields=name%2C+type%2C+tier&page_no=" + page.ToString());
                dynamic output = JsonConvert.DeserializeObject(resp);
                Dictionary<string, Ship> dict = output.data.ToObject<Dictionary<string, Ship>>();
                result = result.Concat(dict).ToDictionary(x => x.Key, x => x.Value);
                if (page++ >= (int)output.meta.page_total)
                    break;
            }
            File.WriteAllText(filePath, JsonConvert.SerializeObject(result));
            return result;
        }
    }
}
