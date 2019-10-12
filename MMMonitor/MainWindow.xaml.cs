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

        public event PropertyChangedEventHandler PropertyChanged;

        public List<Player> MyTeam { get; set; }
        public List<Player> EnemyTeam { get; set; }

        public MainWindow()
        {
            
            InitializeComponent();
            DataContext = this;
            Directory.CreateDirectory("config");
            if (File.Exists(Path.Combine(configDir, configFile)))
            {
                InstallDirTextBlock.Text = File.ReadAllText(Path.Combine(configDir, configFile));
            }
            if (Directory.Exists(Path.Combine(InstallDirTextBlock.Text, "replays")))
                watcher = new FileSystemWatcher(Path.Combine(InstallDirTextBlock.Text, "replays"), "tempArenaInfo.json");
            else
            {
                watcher = new FileSystemWatcher();
                watcher.Filter = "tempArenaInfo.json";
            }
            watcher.Created += TempArenaInfoCreated;
            watcher.EnableRaisingEvents = true;
        }

        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void TempArenaInfoCreated(object sender, FileSystemEventArgs e)
        {
            List<Player> players = JsonParser.parsePlayers(e.FullPath);
            MyTeam = players.Where((Player p) => p.relation <= 1).ToList();
            EnemyTeam = players.Where((Player p) => p.relation == 2).ToList();

            NotifyPropertyChanged(nameof(MyTeam));
            NotifyPropertyChanged(nameof(EnemyTeam));
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
                }
            }
        }
    }
}
