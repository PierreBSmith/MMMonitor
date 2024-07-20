using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MMMonitor
{
    class JsonParser
    {
        Dictionary<string, Ship> shipDict;
        string configDir;
        string filePath { get => Path.Combine(configDir, "ships.json"); }

        public JsonParser(string configDir)
        {
            this.configDir = configDir;
            loadShipDict();
        }
        
        public List<Player> parsePlayers(string path)
        {
            List<Player> players = new List<Player>();

            string gameData = "";
            if (File.Exists(path))
            {
                for(int i = 0; i < 20; ++i)
                {
                    try
                    {
                        gameData = File.ReadAllText(path);
                        break;
                    }
                    catch(IOException e)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
            dynamic data = JsonConvert.DeserializeObject(gameData);
            
            List<Tuple<string, string, int>> playerInfo = new List<Tuple<string, string, int>>();
            HashSet<string> unknownShipIds = new HashSet<string>();
            foreach(dynamic vehicle in data.vehicles)
            {
                playerInfo.Add(new Tuple<string, string, int>((string)vehicle.name, (string)vehicle.shipId, (int)vehicle.relation));
                string shipId = (string)vehicle.shipId;
                if (!shipDict.ContainsKey(shipId))
                    unknownShipIds.Add(shipId);
            }
            if(unknownShipIds.Count > 0)
                updateShipDict(unknownShipIds.ToList());
            players = Fetcher.getPlayers(playerInfo, shipDict);

            /*
            for (int i = 0; i < data.vehicles.Count; i++)
            {
                if (((string)data.vehicles[i].name).StartsWith(":"))
                    continue;
                Player newPlayer;
                try
                {
                    newPlayer = Fetcher.getPlayer((string)data.vehicles[i].name);
                }
                catch(Exception)
                {
                    newPlayer = new Player { userName = (string)data.vehicles[i].name };
                }
                try
                {
                    Fetcher.getPlayerShip(ref newPlayer, (string)data.vehicles[i].shipId);
                }
                catch(Exception)
                {}

                newPlayer.relation = (int)data.vehicles[i].relation;
                string shipId = (string)data.vehicles[i].shipId;
                if (!shipDict.ContainsKey(shipId))
                {
                    updateShipDict(shipId);
                }
                if (!shipDict.ContainsKey(shipId))
                    newPlayer.ship = new Ship { name = "Unknown ship" };
                else
                    newPlayer.ship = shipDict[shipId];
                players.Add(newPlayer);
            }*/
            return players;
        }

        private void loadShipDict(bool forceReload = false)
        {
            if (File.Exists(filePath) && !forceReload)
            {
                try
                {
                    shipDict = JsonConvert.DeserializeObject<Dictionary<string, Ship>>(File.ReadAllText(filePath), new JsonSerializerSettings { MissingMemberHandling = MissingMemberHandling.Error });
                    return;
                }
                catch(JsonSerializationException)
                { }
            }
            shipDict = new Dictionary<string, Ship>();
        }

        private void saveShipDict()
        {
            File.WriteAllText(filePath, JsonConvert.SerializeObject(shipDict));
        }

        private void updateShipDict(List<string> shipIds)
        {
            Dictionary<string, Ship> newShips = Fetcher.getShips(shipIds);
            bool save = false;
            foreach(KeyValuePair<string, Ship> pair in newShips)
            {
                if(pair.Value != null)
                {
                    shipDict.Add(pair.Key, pair.Value);
                    save = true;
                }
            }
            if (save)
                saveShipDict();
        }
    }
}
