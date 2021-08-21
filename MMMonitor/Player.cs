using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }

    public enum ShipType { Destroyer, Cruiser, Battleship, AirCarrier };

    public class Ship
    {
        public string name { get; set; }
        public int tier { get; set; }
        public ShipType type { get; set; }
        
    }
}
