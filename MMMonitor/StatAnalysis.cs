using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MMMonitor
{
    class AdvInfo
    {
        public double allyTeamCarryIndex, enemyTeamCarryIndex, advantage;
    }

    static class StatAnalysis
    {

        public static double Weight(int games, bool player)
        {
            return Math.Min(1, Math.Sqrt(games / (player ? 6000.0 : 300.0)));
        }

        public static double Mean(List<Tuple<double, double>> WRsAndWeights)
        {
            if (WRsAndWeights.Count == 0)
                return double.NaN;
            return WRsAndWeights.Select(w => w.Item1).Average();
        }

        public static double WeightedMean(List<Tuple<double, double>> WRsAndWeights)
        {
            if (WRsAndWeights.Count == 0)
                return double.NaN;
            return WRsAndWeights.Select(w => w.Item1 * w.Item2).Sum() / WRsAndWeights.Select(w => w.Item2).Sum();
        }

        public static double Median(List<Tuple<double, double>> WRsAndWeights)
        {
            if (WRsAndWeights.Count == 0)
                return double.NaN;
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

        public static Tuple<double, double> CombinedWR(Player p)
        {
            if (p.numGames == 0)
                return new Tuple<double, double>(0.45, 0.0);
            double pWeight = Weight(p.numGames, true),
                    sWeight = Weight(p.shipGames, false);
            double weightSum = pWeight + sWeight;
            pWeight /= weightSum;
            sWeight /= weightSum;
            return new Tuple<double, double>(p.winrate * pWeight + p.shipWr * sWeight, weightSum / 2.0);
        }

        public static List<Tuple<double, double>> CombinedWRs(List<Player> team)
        {
            return team.Where(p => p.numGames > 0).Select((p) => CombinedWR(p)).ToList();
        }

        private static double sigmoid(double x)
        {
            return 2.0 / (1.0 + Math.Exp(-x)) - 1.0;
        }

        public static double CarryScore(Player p)
        {
            /*
            double f(double x)
            {
                return sigmoid(20 * (x - .5));
            }

            var cwr = CombinedWR(p);
            return f(cwr.Item1) * cwr.Item2;
            */

            var cwr = CombinedWR(p);
            double slope = 12.0, wr = cwr.Item1, weight = cwr.Item2;
            return weight * (slope * (wr - .5) + .5) + .5;
        }

        public static AdvInfo Advantage(List<Player> allyTeam, List<Player> enemyTeam)
        {
            double g(double x, double y)
            {
                x = Math.Max(0.001, x);
                y = Math.Max(0.001, y);
                return sigmoid(4 * Math.Log(x / y));
            }

            void setTeamCarryScores(List<Player> team)
            {
                team.ForEach((p) => p.carryScore = CarryScore(p));
            }

            AdvInfo r = new AdvInfo();

            setTeamCarryScores(allyTeam);
            setTeamCarryScores(enemyTeam);
            r.allyTeamCarryIndex = allyTeam.Select((p) => p.carryScore).Sum();
            r.enemyTeamCarryIndex = enemyTeam.Select((p) => p.carryScore).Sum();
            r.advantage = g(r.allyTeamCarryIndex, r.enemyTeamCarryIndex);
            return r;
        }
    }
}
