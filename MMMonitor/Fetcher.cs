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
        

        public static async Task<Player> getPlayer(string ID)
        {
            HttpClient client = new HttpClient();
            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            String data;
            client.GetAsync("https://api.worldofwarships.com/wows/account/info/?application_id=" + APP_ID + "&account_id=" + ID).ContinueWith((Task<HttpResponseMessage> msg)=>
            {
                data = JsonConvert.SerializeObject(msg.Result.Content);
                
            });
            //String data = msg.Content.ReadAsStringAsync().Result;
            string output = JsonConvert.SerializeObject(data);
            Console.WriteLine(output);
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
