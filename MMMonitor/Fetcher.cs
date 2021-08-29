using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MMMonitor
{
    class InvalidResponse: Exception
    {
        public InvalidResponse(string message):base(message)
        {}
    }

    static class Fetcher
    {
        const string APP_ID = "e097fb76afcd3bc68716bff7d1e7832c"; //"9215170717109877eac3240ae6393ed8";

        private static string HttpGet(string url)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            using (var client = new HttpClient { Timeout = TimeSpan.FromSeconds(1)})
            {
                for(int i = 0; i < 5; ++i)
                {
                    try
                    {
                        var firstResponse = client.GetAsync(url).Result;
                        if (firstResponse.IsSuccessStatusCode)
                            return firstResponse.Content.ReadAsStringAsync().Result;
                        return null;
                    }
                    catch(Exception e) when (e.InnerException is HttpRequestException || e.InnerException is TaskCanceledException)
                    {
                    }
                }
                return null;
            }
        }

        public static Player getPlayer(string name)
        {
            //Use name to get player ID
            string responseString = HttpGet("https://api.worldofwarships.com/wows/account/list/?application_id=" + APP_ID + "&type=exact&search=" + name);
            if (responseString == null)
                throw new InvalidResponse("Could not get player ID, request returned null");
            dynamic data = JsonConvert.DeserializeObject(responseString);
            if ((int)data.meta.count == 0)
                throw new InvalidResponse("Could not find player");
            string ID = (string)data.data[0].account_id;

            //Query for account stats
            responseString = HttpGet("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID);
            if (responseString == null)
                throw new InvalidResponse("Could not get player stats, request returned null");
            responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
            dynamic playerData = JsonConvert.DeserializeObject(responseString);
            if (playerData.data.ID == null)
                throw new InvalidResponse("No player data");

            //Get clan tag
            string clanTag = "";
            try
            {
                responseString = HttpGet(string.Format("https://api.worldofwarships.com/wows/clans/accountinfo/?account_id={0}&application_id={1}&fields=clan&extra=clan", ID, APP_ID));
                responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
                if (responseString != null)
                {
                    dynamic clanData = JsonConvert.DeserializeObject(responseString);
                    if (clanData.data.ID.clan != null)
                        clanTag = "[" + clanData.data.ID.clan.tag + "]";
                }
            }
            catch
            { }

            Player player = new Player
            {
                userName = clanTag + (string)playerData.data.ID.nickname,
                ID = ID
            };

            if (playerData.meta.hidden.ToObject<List<string>>()?.Contains(ID) == true)
            {
                player.hidden = true;
            }
            else
            {
                player.hidden = false;
                player.winrate = (double)playerData.data.ID.statistics.pvp.wins / (double)playerData.data.ID.statistics.pvp.battles;
                player.numGames = (int)playerData.data.ID.statistics.pvp.battles;
            }

            return player;
        }
        
        public static void getPlayerShip(ref Player player, string shipID)
        {
            //Query for ship stats
            string responseString = HttpGet("https://api.worldofwarships.com/wows/ships/stats/?application_id=" + APP_ID + "&account_id=" + player.ID + "&ship_id=" + shipID);
            responseString = responseString.Replace("\"" + player.ID + "\"", "\"ID\"");
            dynamic shipsStuff = JsonConvert.DeserializeObject(responseString);

            player.shipGames = (shipsStuff.meta.hidden == null && shipsStuff.data.ID != null) ? (int)shipsStuff.data.ID[0].pvp.battles : 0;
            player.shipWr = (shipsStuff.meta.hidden == null && shipsStuff.data.ID != null && (int)shipsStuff.data.ID[0].pvp.battles > 0) ? ((double)shipsStuff.data.ID[0].pvp.wins / (int)shipsStuff.data.ID[0].pvp.battles) : 0;
            
        }

        public static Ship getShip(string shipId)
        {
            try
            {
                string resp = HttpGet("https://api.worldofwarships.com/wows/encyclopedia/ships/?ship_id=" + shipId + "&application_id=" + APP_ID + "&fields=name%2C+type%2C+tier");
                dynamic output = JsonConvert.DeserializeObject(resp);
                return output.data.ToObject<Dictionary<string, Ship>>()[shipId];
            }
            catch
            {
                return null;
            }
        }

        public static Dictionary<string, Ship> getShipDict()
        {
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
            return result;
        }
    }
}
