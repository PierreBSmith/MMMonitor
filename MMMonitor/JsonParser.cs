using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MMMonitor
{
    class JsonParser
    {
        Dictionary<string, string> shipDict;
        string configDir;

        public JsonParser(string configDir)
        {
            this.configDir = configDir;
            shipDict = Fetcher.getShipDict(configDir);
        }
        
        public List<Player> parsePlayers(string path)
        {
            List<Player> players = new List<Player>();

            string gameData = "";
            if (File.Exists(path))
            {
                gameData = File.ReadAllText(path);
            }
            dynamic data = JsonConvert.DeserializeObject(gameData);
            for (int i = 0; i < data.vehicles.Count; i++)
            {
                Player newPlayer = Fetcher.getPlayer((string)data.vehicles[i].name);
                newPlayer.relation = (int)data.vehicles[i].relation;
                string shipId = (string)data.vehicles[i].shipId;
                if (!shipDict.ContainsKey(shipId))
                    shipDict = Fetcher.getShipDict(configDir, true);
                newPlayer.ship = shipDict[shipId];
                players.Add(newPlayer);
            }
            return players;
        }
    }
}
