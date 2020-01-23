using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Path = System.IO.Path;

namespace MMMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        const string configDir = "config",
            configFile = "config.txt";

        FileSystemWatcher watcher;
        JsonParser parser;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<Player> MyTeam { get; set; }
        public List<Player> EnemyTeam { get; set; }

        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            Directory.CreateDirectory(configDir);
            parser = new JsonParser(configDir);
            if (File.Exists(Path.Combine(configDir, configFile)))
            {
                InstallDirTextBlock.Text = File.ReadAllText(Path.Combine(configDir, configFile));
            }
            if (Directory.Exists(Path.Combine(InstallDirTextBlock.Text, "replays")))
            {
                watcher = new FileSystemWatcher(Path.Combine(InstallDirTextBlock.Text, "replays"), "tempArenaInfo.json");
                if (File.Exists(Path.Combine(InstallDirTextBlock.Text, "replays", "tempArenaInfo.json")))
                    LoadPlayers(Path.Combine(InstallDirTextBlock.Text, "replays", "tempArenaInfo.json"));
                watcher.EnableRaisingEvents = true;
            }
            else
            {
                watcher = new FileSystemWatcher();
                watcher.Filter = "tempArenaInfo.json";
            }
            watcher.Created += TempArenaInfoCreated;
            
        }

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void LoadPlayers(string path)
        {
            Task.Run(() => LoadPlayers2(path));
        }

        private void LoadPlayers2(string path)
        {
            int PlayerComparer(Player x, Player y)
            {
                int typeDiff = y.ship.type - x.ship.type; //y-x for reverse sorting order
                if (typeDiff != 0)
                    return typeDiff;
                return y.ship.tier - x.ship.tier;
            }

            Dispatcher.Invoke(() => LoadingPanel.Visibility = Visibility.Visible);
            List<Player> players = parser.parsePlayers(path);
            MyTeam = players.Where((Player p) => p.relation <= 1).ToList();
            MyTeam.Sort(PlayerComparer);
            EnemyTeam = players.Where((Player p) => p.relation == 2).ToList();
            EnemyTeam.Sort(PlayerComparer);
            double advantage = StatAnalysis.Advantage(MyTeam, EnemyTeam);
            double positiveAdvantage = Math.Max(0, advantage),
                negativeAdvantage = Math.Max(0, -advantage);
            Dispatcher.Invoke(() =>
            {
                LoadingPanel.Visibility = Visibility.Hidden;
                MyTeamAdvantageBar.Width = new GridLength(positiveAdvantage, GridUnitType.Star);
                MyTeamAdvantageBarNegative.Width = new GridLength(Math.Max(0, 1 - positiveAdvantage), GridUnitType.Star);
                EnemyTeamAdvantageBar.Width = new GridLength(negativeAdvantage, GridUnitType.Star);
                EnemyTeamAdvantageBarNegative.Width = new GridLength(Math.Max(0, 1 - negativeAdvantage), GridUnitType.Star);
            });
            NotifyPropertyChanged(nameof(MyTeam));
            NotifyPropertyChanged(nameof(EnemyTeam));

            void SetupTeamWrText(List<Player> team, Grid infoGrid)
            {
                Func<List<Tuple<double, double>>, double>[] analysisMethods = { StatAnalysis.Mean, StatAnalysis.WeightedMean, StatAnalysis.Median };
                Func<List<Player>, List<Tuple<double, double>>>[] dataSources = { StatAnalysis.CombinedWRs, StatAnalysis.PlayerWRs, StatAnalysis.ShipWRs };
                String[] analysisNames = { "Average", "Weighted Average", "Median" },
                    dataNames = { "Combined", "Player", "Ship" };

                Tuple<double, double, double> FillCell(int row, int col, int colSpan, Func<List<Tuple<double, double>>, double> analysisMethod, Func<List<Player>, List<Tuple<double, double>>> dataSource, string labelText = null)
                {
                    double wr = analysisMethod(dataSource(team));
                    Tuple<double, double, double> hsvColor = double.IsNaN(wr) ? new Tuple<double, double, double>(0, 0, 1) : new Tuple<double, double, double>(ColorStuff.WinrateToHue(wr), 1.0, 0.9);
                    Dispatcher.Invoke(() =>
                    {
                        TextBlock text = new TextBlock
                        {
                            Text = string.Format(labelText + "{0:P}", wr),
                            Background = new SolidColorBrush(ColorStuff.ColorFromHSV(hsvColor)),
                            Foreground = new SolidColorBrush(ColorStuff.GetContrastingTextColor(hsvColor))
                        };
                        Grid.SetRow(text, row);
                        Grid.SetColumn(text, col);
                        Grid.SetColumnSpan(text, colSpan);
                        infoGrid.Children.Add(text);
                    });
                    return hsvColor;
                }

                for (int i = 0; i < analysisMethods.Length; ++i)
                {
                    Tuple<double, double, double> bgColorHSV = null;
                    for (int j = 0; j < dataSources.Length; ++j)
                    {
                        if (j == 0)
                            bgColorHSV = FillCell(2 * i, 1, 2, analysisMethods[i], dataSources[j], dataNames[j] + ": ");
                        else
                            FillCell(2 * i + 1, j - 1, 1, analysisMethods[i], dataSources[j], dataNames[j] + ": ");
                    }
                    Dispatcher.Invoke(() =>
                    {
                        Border border = new Border { BorderThickness = new Thickness(1), BorderBrush = Brushes.Black };
                        Grid.SetRow(border, i * 2);
                        Grid.SetRowSpan(border, 2);
                        Grid.SetColumnSpan(border, 2);
                        Grid.SetZIndex(border, 1);
                        infoGrid.Children.Add(border);
                        TextBlock text = new TextBlock
                        {
                            Text = analysisNames[i] + " Win Rate", FontWeight = FontWeights.Bold,
                            Background = new SolidColorBrush(ColorStuff.ColorFromHSV(bgColorHSV)),
                            Foreground = new SolidColorBrush(ColorStuff.GetContrastingTextColor(bgColorHSV))
                        };
                        Grid.SetRow(text, i * 2);
                        infoGrid.Children.Add(text);
                    });
                }
            }

            SetupTeamWrText(MyTeam, MyTeamInfoGrid);
            SetupTeamWrText(EnemyTeam, EnemyTeamInfoGrid);
        }

        private void TempArenaInfoCreated(object sender, FileSystemEventArgs e)
        {
            LoadPlayers(e.FullPath);
        }

        private void ChangeInstallDirButton_Click(object sender, RoutedEventArgs e)
        {
            using(FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                folderBrowser.SelectedPath = InstallDirTextBlock.Text;
                if(folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = folderBrowser.SelectedPath;
                    File.WriteAllText("config\\config.txt", path);
                    InstallDirTextBlock.Text = path;
                    watcher.Path = Path.Combine(InstallDirTextBlock.Text, "replays");
                    watcher.EnableRaisingEvents = true;
                    if (File.Exists(Path.Combine(InstallDirTextBlock.Text, "replays", "tempArenaInfo.json")))
                        LoadPlayers(Path.Combine(InstallDirTextBlock.Text, "replays", "tempArenaInfo.json"));
                }
            }
        }
    }

    public class ColorContraster:IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Player player = (Player)value;
            bool usePlayerStats = bool.Parse((string)parameter);
            var bgColor = ColorStuff.StatsToColor(player, usePlayerStats);
            return ColorStuff.GetContrastingTextColor(bgColor).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class PlayerStatsToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Player player = (Player)value;
            bool usePlayerStats = bool.Parse((string)parameter);
            return ColorStuff.ColorFromHSV(ColorStuff.StatsToColor(player, usePlayerStats)).ToString();
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    static class ColorStuff
    {
        public static Color ColorFromHSV(Tuple<double, double, double> input)
        {
            double hue = input.Item1, saturation = input.Item2, value = input.Item3;
            int hi = (int)(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            byte v = (byte)(value);
            byte p = (byte)(value * (1 - saturation));
            byte q = (byte)(value * (1 - f * saturation));
            byte t = (byte)(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }

        public static double WinrateToHue(double winrate)
        {
            if (winrate < .40)
                return 0;
            else if (winrate <= .50)
                return scale(winrate, .40, .50, 0, 60);
            else if (winrate <= .65)
                return scale(winrate, .50, .65, 60, 240);
            else if (winrate <= .70)
                return scale(winrate, .65, .70, 240, 280);
            else
                return 280;
        }

        public static Tuple<double, double, double> StatsToColor(Player player, bool usePlayerStats)
        {

            double winrate = usePlayerStats ? player.winrate : player.shipWr;
            int games = usePlayerStats ? player.numGames : player.shipGames;
            double hue = WinrateToHue(winrate);
            double sat = Math.Min(1, Math.Sqrt(games / (usePlayerStats ? 10000.0 : 500.0)));
            return new Tuple<double, double, double>(hue, sat, .9);
        }

        public static Color GetContrastingTextColor(Tuple<double, double, double> bgColor)
        {
            if (bgColor.Item2 < .5 || bgColor.Item1 < 200)
                return Colors.Black;
            return Colors.White;
        }

        private static double scale(double val, double max1, double min1, double max2, double min2)
        {
            return (val - min1) / (max1 - min1) * (max2 - min2) + min2;
        }
    }
}
