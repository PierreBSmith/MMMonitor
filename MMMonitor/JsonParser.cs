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
                    Ship newShip = Fetcher.getShip(shipId);
                    if (newShip != null)
                        shipDict.Add(shipId, newShip);
                }
                if (!shipDict.ContainsKey(shipId))
                    newPlayer.ship = new Ship { name = "Unknown ship" };
                else
                    newPlayer.ship = shipDict[shipId];
                players.Add(newPlayer);
            }
            return players;
        }
    }
}
