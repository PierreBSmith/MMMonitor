using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
namespace MMMonitor
{
    static class JsonParser
    {
        
        const string APP_ID = "9215170717109877eac3240ae6393ed8";
        
        public static List<Player> parsePlayers(string path)
        {
            List<Player> players = new List<Player>(); 
            
            string gameData = "";
            if (File.Exists(path)) {
                gameData = File.ReadAllText(path);
            }    
            dynamic data = JsonConvert.DeserializeObject(gameData);
            for (int i = 0; i < data.vehicles.Count; i++)
            {
                players.Add(Fetcher.getPlayer((string)data.vehicles[i].name));
                players.ElementAt(i).relation = (int)data.vehicles[i].relation;
            }
            return players;
        }
    }
}
