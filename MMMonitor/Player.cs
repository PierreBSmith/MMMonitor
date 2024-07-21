using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace MMMonitor
{
    public class Player
    {
        public double winrate { get; set; }
        public double carryScore { get; set; }
        public string userName { get; set; }
        public string ID { get; set; }
        public int relation { get; set; }
        public int numGames { get; set; }
        public Ship ship { get; set; } = new Ship();
        public int shipGames { get; set; }
        public double shipWr { get; set; }
        public bool hidden { get; set; }
    }

    public enum ShipType { Submarine, Destroyer, Cruiser, Battleship, AirCarrier };

    public class Ship
    {
        [JsonProperty(Required = Required.Always)]
        public string name { get; set; }
        
        [JsonProperty(Required = Required.Always)] 
        public int tier { get; set; }

        [JsonProperty(Required = Required.Always)]
        public ShipType type { get; set; }

        [JsonProperty(Required = Required.Always)]
        public string ship_id { get; set; }
    }
}
