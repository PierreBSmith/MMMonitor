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
        const string APP_ID = "e097fb76afcd3bc68716bff7d1e7832c";
        

        public static Player getPlayer(string ID)
        {
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            using (var client = new HttpClient())
            {
                var response = client.GetAsync("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID).Result;

                if (response.IsSuccessStatusCode)
                {
                    var responseContent = response.Content;

                    // by calling .Result you are synchronously reading the result
                    string responseString = responseContent.ReadAsStringAsync().Result;
                    responseString = responseString.Replace("\"" + ID + "\"", "\"ID\"");
                    dynamic output = JsonConvert.DeserializeObject(responseString);
                    Player result = new Player(((double)output.data.ID.statistics.pvp.wins)/((double)output.data.ID.statistics.pvp.battles), (string)output.data.ID.nickname, (string)ID);
                }
            }            

            Player player = new Player();
            return player;
        }
        
    }
    class Player
    {
        double winrate;
        string userName;
        string ID;
        public Player()
        {
            winrate = .2;
            userName = "potato";
            ID = "P Sherman, 42 Wallaby Way, Sydney";
        }
        public Player(double wr, string name, string ident)
        {
            winrate = wr;
            userName = name;
            ID = ident;
        }
    }
}
