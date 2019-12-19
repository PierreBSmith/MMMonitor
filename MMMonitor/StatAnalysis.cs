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

        public static double Mean(List<Tuple<double, double>> WRsAndWeights)
        {
            return WRsAndWeights.Select(w => w.Item1).Average();
        }

        public static double WeightedMean(List<Tuple<double, double>> WRsAndWeights)
        {
            return WRsAndWeights.Select(w => w.Item1 * w.Item2).Sum() / WRsAndWeights.Select(w => w.Item2).Sum();
        }

        public static double Median(List<Tuple<double, double>> WRsAndWeights)
        {
            if (WRsAndWeights.Count == 0)
                return 0;
            List<double> sortedWrs = WRsAndWeights.Select(w => w.Item1).OrderBy(w => w).ToList();
            int count = sortedWrs.Count;
            double median = sortedWrs[count / 2];
            if (count % 2 == 0)
                median = (median + sortedWrs[count / 2 - 1]) / 2;
            return median;
        }

        public static List<Tuple<double, double>> PlayerWRs(List<Player> team)
        {
            return team.Where(p => p.numGames > 0).Select((p) => new Tuple<double, double>(p.winrate, Weight(p.numGames, true))).ToList();
        }

        public static List<Tuple<double, double>> ShipWRs(List<Player> team)
        {
            return team.Where(p => p.shipGames > 0).Select((p) => new Tuple<double, double>(p.shipWr, Weight(p.shipGames, false))).ToList();
        }

        public static List<Tuple<double, double>> CombinedWRs(List<Player> team)
        {
            return team.Where(p => p.numGames > 0).Select((p) =>
            {
                double pWeight = Weight(p.numGames, true),
                    sWeight = Weight(p.shipGames, false);
                double weightSum = pWeight + sWeight;
                pWeight /= weightSum;
                sWeight /= weightSum;
                return new Tuple<double, double>(p.winrate * pWeight + p.shipWr * sWeight, weightSum);
            }).ToList();
        }
    }
}
