using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using Newtonsoft.Json;
namespace MMMonitor
{
    static class Fetcher
    {
        const string APP_ID = "9215170717109877eac3240ae6393ed8";
        

        public static Player getPlayer(string name)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            string ID = "";
            using (var client = new HttpClient())
            {
                var firstResponse = client.GetAsync("https://api.worldoftanks.com/wgn/account/list/?application_id=" + APP_ID + "&search=" + name).Result;
                if (firstResponse.IsSuccessStatusCode)
                {
                    var responseContent = firstResponse.Content;
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    dynamic data = JsonConvert.DeserializeObject(responseString);
                    ID = (string)data.data[0].account_id;
                }

                var response = client.GetAsync("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID).Result;
                
                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;

                    // by calling .Result you are synchronously reading the result
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
                    dynamic output = JsonConvert.DeserializeObject(responseString);
                    return new Player(((double)output.data.ID.statistics.pvp.wins)/((double)output.data.ID.statistics.pvp.battles),(int)output.data.ID.statistics.pvp.battles, (string)output.data.ID.nickname, ID, 0);
                }
            }            
            return null;
        }
        
    }

    public class Player
    {
        public double winrate { get; set; }
        public string userName { get; set; }
        public string ID { get; set; }
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
        public Player(double wr,int games, string name, string ident, int relation)
        {
            winrate = wr*100;
            userName = name;
            ID = ident;
            this.relation = relation;
            numGames = games;
        }
    }
}
