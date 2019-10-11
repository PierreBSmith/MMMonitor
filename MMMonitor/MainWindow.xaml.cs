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
using System.Windows.Shapes;

namespace MMMonitor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string configDir = "config",
            configFile = "config.txt";

        public MainWindow()
        {
            InitializeComponent();
            Directory.CreateDirectory("config");
            if (File.Exists(Path.Combine(configDir, configFile)));
        }

        private void ChangeInstallDirButton_Click(object sender, RoutedEventArgs e)
        {
            using(FolderBrowserDialog folderBrowser = new FolderBrowserDialog())
            {
                if(folderBrowser.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string path = folderBrowser.SelectedPath;
                    File.WriteAllText("config\\config.txt", path);
                    InstallDirTextBlock.Text = path;
                }
            }
        }
    }
}
