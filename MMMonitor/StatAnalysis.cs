using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMMonitor
{
    static class StatAnalysis
    {
        private static double Weight(int games, bool player)
        {
            return Math.Min(1, Math.Sqrt(games / (player ? 10000.0 : 500.0)));
        }

        public static double Potatometer(List<Player> team)
        {
            int n = 0;
            return team.Where(p => p.numGames > 0).Select((p) =>
            {
                double pWeight = Weight(p.numGames, true),
                    sWeight = Weight(p.shipGames, false);
                double weightSum = pWeight + sWeight;
                pWeight /= weightSum;
                sWeight /= weightSum;
                return (p.winrate * pWeight + p.shipWr * sWeight);
            }).Average();
        }
    }
}
