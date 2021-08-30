using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;

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

        const string SHIP_FIELDS = "name%2Ctype%2Ctier%2Cship_id";

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

        private static string makeQueryList(IEnumerable<string> list)
        {
            string res = "";
            foreach (string item in list)
                res += item + ",";
            return WebUtility.UrlEncode(res.Remove(res.Length - 1));
        }

        public static List<Player> getPlayers(List<Tuple<string, string, int>> playerInfoList, Dictionary<string, Ship> shipDict)
        {
            playerInfoList = playerInfoList.Where(x => !x.Item1.StartsWith(":")).ToList(); //remove bots
            Dictionary<string, Player> playerNameDict = new Dictionary<string, Player>();
            foreach (Tuple<string, string, int> tuple in playerInfoList)
            {
                playerNameDict.Add(tuple.Item1.ToLower(), new Player
                {
                    userName = tuple.Item1,
                    ship = shipDict.ContainsKey(tuple.Item2) ? shipDict[tuple.Item2] : new Ship { name = "Unknown Ship" },
                    relation = tuple.Item3,
                });
            }

            //Get Player IDs
            string responseString = HttpGet(string.Format("https://api.worldofwarships.com/wows/account/list/?application_id={0}&type=exact&search={1}",
                APP_ID, makeQueryList(playerInfoList.Select(x => x.Item1))));
            if (responseString == null)
                throw new InvalidResponse("Player IDs request returned null");
            dynamic idData = JsonConvert.DeserializeObject(responseString);
            Dictionary<string, Player> playerIdDict = new Dictionary<string, Player>();
            foreach (dynamic item in idData.data)
            {
                string name = (string)item.nickname;
                if(playerNameDict.ContainsKey(name.ToLower()))
                {
                    Player player = playerNameDict[name.ToLower()];
                    player.ID = item.account_id;
                    playerIdDict.Add((string)item.account_id, player);
                }
                else
                {
                    MessageBox.Show("Missing entry for player " + name, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }

            //Get Account Stats
            string idsQueryList = makeQueryList(playerIdDict.Keys);
            responseString = HttpGet(string.Format("https://api.worldofwarships.com/wows/account/info/?application_id={0}&account_id={1}&fields=statistics.pvp.battles%2Cstatistics.pvp.wins",
                APP_ID, idsQueryList));
            if (responseString == null)
                throw new InvalidResponse("Account stats request returned null");
            dynamic statsData = JsonConvert.DeserializeObject(responseString);
            Dictionary<string, dynamic> statsDict = statsData.data.ToObject<Dictionary<string, dynamic>>();
            List<string> hiddenList = statsData.meta.hidden.ToObject<List<string>>();
            foreach (KeyValuePair<string, Player> pair in playerIdDict)
            {
                if (hiddenList?.Contains(pair.Key) == true)
                {
                    pair.Value.hidden = true;
                }
                else
                {
                    pair.Value.hidden = false;
                    if (statsDict.ContainsKey(pair.Key))
                    {
                        pair.Value.winrate = (double)statsDict[pair.Key].statistics.pvp.wins / (double)statsDict[pair.Key].statistics.pvp.battles;
                        pair.Value.numGames = (int)statsDict[pair.Key].statistics.pvp.battles;
                    }
                    else
                    {
                        MessageBox.Show("Missing data for player " + pair.Value.userName, "", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }

            //Get Clan Tags
            responseString = HttpGet(string.Format("https://api.worldofwarships.com/wows/clans/accountinfo/?application_id={0}&account_id={1}&extra=clan&fields=clan",
                APP_ID, idsQueryList));
            if (responseString == null)
                MessageBox.Show("Clan data request returned null");
            dynamic clanData = JsonConvert.DeserializeObject(responseString);
            Dictionary<string, dynamic> clanDict = clanData.data.ToObject<Dictionary<string, dynamic>>();
            foreach(KeyValuePair<string, Player> pair in playerIdDict)
            {
                if(clanDict.ContainsKey(pair.Key))
                {
                    if (clanDict[pair.Key]?.clan != null)
                        pair.Value.userName = "[" + clanDict[pair.Key].clan.tag + "] " + pair.Value.userName;
                }
                else
                {
                    MessageBox.Show("Missing clan data for player" + pair.Value.userName);
                }
            }

            foreach(Player player in playerIdDict.Values)
            {
                if(player.ship.ship_id != null)
                    getPlayerShip(player, player.ship.ship_id);
            }

            return playerIdDict.Values.ToList();
        }

        /*
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
                        clanTag = "[" + clanData.data.ID.clan.tag + "] ";
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
        */
        
        public static void getPlayerShip(Player player, string shipID)
        {
            //Query for ship stats
            if (shipID == null)
                return;
            string responseString = HttpGet("https://api.worldofwarships.com/wows/ships/stats/?application_id=" + APP_ID + "&account_id=" + player.ID + "&ship_id=" + shipID + "&fields=pvp.battles%2Cpvp.wins");
            responseString = responseString.Replace("\"" + player.ID + "\"", "\"ID\"");
            dynamic shipsStuff = JsonConvert.DeserializeObject(responseString);

            player.shipGames = (shipsStuff.meta.hidden == null && shipsStuff.data.ID != null) ? (int)shipsStuff.data.ID[0].pvp.battles : 0;
            player.shipWr = (shipsStuff.meta.hidden == null && shipsStuff.data.ID != null && (int)shipsStuff.data.ID[0].pvp.battles > 0) ? ((double)shipsStuff.data.ID[0].pvp.wins / (int)shipsStuff.data.ID[0].pvp.battles) : 0;
        }

        /*
        public static Ship getShip(string shipId)
        {
            try
            {
                string resp = HttpGet("https://api.worldofwarships.com/wows/encyclopedia/ships/?ship_id=" + shipId + "&application_id=" + APP_ID + "&fields=" + SHIP_FIELDS);
                dynamic output = JsonConvert.DeserializeObject(resp);
                return output.data.ToObject<Dictionary<string, Ship>>()[shipId];
            }
            catch
            {
                return null;
            }
        }
        */

        public static Dictionary<string, Ship> getShips(List<string> shipIds)
        {
            try
            {
                string responseString = HttpGet(string.Format("https://api.worldofwarships.com/wows/encyclopedia/ships/?application_id={0}&ship_id={1}&fields={2}",
                APP_ID, makeQueryList(shipIds), SHIP_FIELDS));
                dynamic output = JsonConvert.DeserializeObject(responseString);
                return output.data.ToObject<Dictionary<string, Ship>>();
            }
            catch
            {
                return new Dictionary<string, Ship>();
            }
        }

        public static Dictionary<string, Ship> getShipDict()
        {
            Dictionary<string, Ship> result = new Dictionary<string, Ship>();
            int page = 1;
            while(true)
            {
                string resp = HttpGet("https://api.worldofwarships.com/wows/encyclopedia/ships/?application_id=" + APP_ID + "&fields=" + SHIP_FIELDS + "&page_no=" + page.ToString());
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
