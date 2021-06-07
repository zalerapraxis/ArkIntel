using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using WinSCP;

namespace ArkIntel
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public static string SavefileName = "Extinction.ark";
        public static string ServerHostName = "";
        public static string ServerUserName = "";
        public static string ServerPassword = "";


        public static event EventHandler LstDinosSelectionChanged;
        public static event EventHandler DgDinoDataSelectionChanged;
        public FileInfo[] Files;
        public static DirectoryInfo Dir = new DirectoryInfo($"{Directory.GetCurrentDirectory()}\\output");
        public static List<DinoStats> GlobalDinoData;
        public static bool MapOverlayOpen;

        public MainWindow()
        {
            InitializeComponent();
            InitializeData();
        }

        public void InitializeData()
        {
            RunArkTools();
            PopulateListbox();
            CountTotalDinos();
            lbStatus.Content = "Worldfile last saved: " + File.GetLastWriteTime(SavefileName).ToString("MM/dd HH:mm:ss");
        }

        private void lstDinos_SelectionChanged(object sender, EventArgs e)
        {
            if (lstDinos.SelectedIndex == -1)
            {
                return;
            }
            string selectedValue = Files[lstDinos.SelectedIndex].Name;

            GlobalDinoData = JsonConvert.DeserializeObject<List<DinoStats>>(File.ReadAllText($@"{Dir}\{selectedValue}"));
            dgDinoData.ItemsSource = GlobalDinoData;
            
            lbSelectedDinoCount.Content = $"Selected count: {GlobalDinoData.Count}";
            btnShowMap.IsEnabled = true;

            LstDinosSelectionChanged?.Invoke(sender, e);
        }

        private void dgDinoData_SelectionChanged(object sender, EventArgs e)
        {
            DgDinoDataSelectionChanged?.Invoke(sender, e);
        }

        private async void btnRefreshData_Click(object sender, RoutedEventArgs e)
        {
            lbStatus.Content = "Connecting to FTP server...";
            await Task.Run(() => DownloadNewData());
            lbStatus.Content = "Save data downloaded. Running Ark-Tools...";
            InitializeData();
        }

        private void btnShowMap_Click(object sender, RoutedEventArgs e)
        {
            if (MapOverlayOpen)
            {
                return;
            }
            MapOverlay mapOverlay = new MapOverlay
            {
                Left = Left + Width,
                Top = Top
            };
            mapOverlay.Show();
            MapOverlayOpen = true;
        }

        private void RunArkTools()
        {
            if (!Directory.Exists(Dir.FullName))
            {
                Directory.CreateDirectory("output");
            }

            foreach (FileInfo file in Dir.GetFiles()) // Clear any old data
            {
                file.Delete();
            }

            System.Diagnostics.Process arktools = new System.Diagnostics.Process();
            System.Diagnostics.ProcessStartInfo startInfo =
                new System.Diagnostics.ProcessStartInfo
                {
                    WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden,
                    FileName = "java",
                    Arguments = $"-jar ark-tools.jar creatures {SavefileName} output"
                };
            arktools.StartInfo = startInfo;
            arktools.Start();
            arktools.WaitForExit();

            if (File.Exists($@"{Dir}\classes.json"))
            {
                File.Delete($@"{Dir}\classes.json");
            }
        }

        private int DownloadNewData()
        {
            if (File.Exists(SavefileName))
            {
                File.Delete(SavefileName);
            }
            try
            {
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = ServerHostName,
                    UserName = ServerUserName,
                    Password = ServerPassword
                };

                using (Session session = new Session())
                {
                    session.FileTransferProgress += SessionFileTransferProgress;

                    session.Open(sessionOptions);

                    try
                    {
                        session.GetFiles($"/ShooterGame/Saved/SavedArks/{SavefileName}", SavefileName).Check();
                    }
                    catch (Exception)
                    {
                        lbStatus.Content = "Error";
                    }
                }
                return 0;
            }
            catch (Exception)
            {
                lbStatus.Content = "Error";
                return 1;
            }
        }

        private void SessionFileTransferProgress(object sender, FileTransferProgressEventArgs e)
        {
            // Print transfer progress
            Dispatcher.Invoke(() =>
            {
                lbStatus.Content = $"Downloading: {e.FileName} - ({e.FileProgress:P0})";
            });
        }

        private void PopulateListbox()
        {
            lstDinos.Items.Clear();

            Files = Dir.GetFiles("*.json");

            foreach (var file in Files)
            {
                string fileCategorized;

                if (file.Name == "classes.json")
                    continue;

                // Remove extra filename clutter
                var fileCleaned = file.Name.Substring(0, file.Name.IndexOf("_C", StringComparison.Ordinal));

                // Categorizing any "special" dinos
                if (file.Name.Contains("SE_"))
                {
                    // scorched earth
                    fileCategorized = fileCleaned.Remove(0, 3) + " (SE)";
                }
                else if (file.Name.Contains("Corrupt"))
                {
                    // corrupted - from extinction
                    fileCategorized = fileCleaned + " (Corrupt)";
                }
                else if (file.Name.Contains("Rare"))
                {
                    // rare spawns - from 'Rare Sightings' mod
                    fileCategorized = fileCleaned + " (Rare)";
                }
                else
                {
                    fileCategorized = fileCleaned;
                }

                // Elemental dino handling
                if (file.Name.Contains("Lightning"))
                {
                    // lightning
                    fileCategorized = fileCategorized + " (L)";
                }
                else if (file.Name.Contains("Fire"))
                {
                    // fire
                    fileCategorized = fileCategorized + " (F)";
                }
                else if (file.Name.Contains("Poison"))
                {
                    // poison
                    fileCategorized = fileCategorized + " (P)";
                }
                else if (file.Name.Contains("Ice"))
                {
                    // ice
                    fileCategorized = fileCategorized + " (I)";
                }

                // Add to listbox
                lstDinos.Items.Add(fileCategorized);
            }
        }

        public void CountTotalDinos()
        {
            Files = Dir.GetFiles("*.json");
            var totalDinos = 0;
            foreach (FileInfo file in Files)
            {
                var dinoData = JsonConvert.DeserializeObject<List<DinoStats>>(File.ReadAllText($@"{Dir}\{file}"));
                totalDinos = totalDinos + dinoData.Count;
            }
            lbTotalDinoCount.Content = $"Total count: {totalDinos}";
        }

        public static bool IsWindowOpen<T>(string name = "") where T : Window
        {
            return string.IsNullOrEmpty(name)
               ? Application.Current.Windows.OfType<T>().Any()
               : Application.Current.Windows.OfType<T>().Any(w => w.Name.Equals(name));
        }
    }

    public class LevelToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int? input = value as int?;
            if (input > 90)
            {
                return Brushes.LightGreen;
            }
            else if (input > 60)
            {
                return Brushes.Yellow;
            }
            else if (input > 30)
            {
                return Brushes.Orange;
            }
            else return Brushes.Red;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    public class WildLevels
    {
        public int health { get; set; }
        public int stamina { get; set; }
        public int oxygen { get; set; }
        public int food { get; set; }
        public int weight { get; set; }
        public int melee { get; set; }
        public int speed { get; set; }
    }

    public class Location
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public double z { get; set; }
    }
    public class DinoStats
    {
        public int baseLevel { get; set; }
        public bool? female { get; set; }
        public Location location { get; set; }
        public WildLevels wildLevels { get; set; }
        public bool tamed { get; set; }
        public string name { get; set; }
    }
}
