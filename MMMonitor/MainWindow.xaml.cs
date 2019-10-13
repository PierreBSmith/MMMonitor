using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.ComponentModel;
using System.Runtime.CompilerServices;

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
            int PlayerComparer(Player x, Player y)
            {
                int typeDiff = y.ship.type - x.ship.type; //y-x for reverse sorting order
                if (typeDiff != 0)
                    return typeDiff;
                return y.ship.tier - x.ship.tier;
            }

            List<Player> players = parser.parsePlayers(path);
            MyTeam = players.Where((Player p) => p.relation <= 1).ToList();
            MyTeam.Sort(PlayerComparer);
            EnemyTeam = players.Where((Player p) => p.relation == 2).ToList();
            EnemyTeam.Sort(PlayerComparer);
            NotifyPropertyChanged(nameof(MyTeam));
            NotifyPropertyChanged(nameof(EnemyTeam));

            void SetupTeamWrText(List<Player> team, TextBlock text)
            {
                double wr = StatAnalysis.Potatometer(team);
                Tuple<double, double, double> hsvColor = new Tuple<double, double, double>(ColorStuff.WinrateToHue(wr), 1.0, 0.9);
                Dispatcher.Invoke(() =>
                {
                    text.Text = string.Format("Weighted WR: {0:P}", wr);
                    text.Background = new SolidColorBrush(ColorStuff.ColorFromHSV(hsvColor));
                    text.Foreground = new SolidColorBrush(ColorStuff.GetContrastingTextColor(hsvColor));
                });
            }

            SetupTeamWrText(MyTeam, MyTeamWrText);
            SetupTeamWrText(EnemyTeam, EnemyTeamWrText);
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
