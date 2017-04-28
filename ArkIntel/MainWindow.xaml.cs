using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        public static event EventHandler LstDinosSelectionChanged;
        public static event EventHandler DgDinoDataSelectionChanged;
        public FileInfo[] Files;
        public static DirectoryInfo Dir = new DirectoryInfo(@"output");
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
            lbStatus.Content = "Worldfile last saved: " + File.GetLastWriteTime("TheVolcano.ark").ToString("MM/dd HH:mm:ss");
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
            if (!Directory.Exists($@"{Dir}"))
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
                    Arguments = "-jar ark-tools.jar wild TheVolcano.ark output"
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
            if (File.Exists("TheVolcano.ark"))
            {
                File.Delete("TheVolcano.ark");
            }
            try
            {
                SessionOptions sessionOptions = new SessionOptions
                {
                    Protocol = Protocol.Ftp,
                    HostName = "",
                    UserName = "",
                    Password = ""
                };

                using (Session session = new Session())
                {
                    session.FileTransferProgress += SessionFileTransferProgress;

                    session.Open(sessionOptions);

                    try
                    {
                        session.GetFiles("/ShooterGame/Saved/SavedArk39950/TheVolcano.ark", "TheVolcano.ark").Check();
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

                // Remove extra filename clutter
                var fileCleaned = file.Name.Substring(0, file.Name.IndexOf("_Character", StringComparison.Ordinal));

                // Categorizing any "special" dinos
                if (file.Name.Contains("SE_"))
                {
                    fileCategorized = fileCleaned.Remove(0, 3) + " (SE)";
                }
                else if (file.Name.Contains("Child"))
                {
                    fileCategorized = fileCleaned + " (Child)";
                }
                else if (!file.Name.Contains("Core"))
                {
                    fileCategorized = fileCleaned + " (Base)";
                }
                else
                {
                    fileCategorized = fileCleaned;
                }

                // Elemental dino handling
                if (file.Name.Contains("Lightning"))
                {
                    fileCategorized = fileCategorized + " (L)";
                }
                else if (file.Name.Contains("Fire"))
                {
                    fileCategorized = fileCategorized + " (F)";
                }
                else if (file.Name.Contains("Poison"))
                {
                    fileCategorized = fileCategorized + " (P)";
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
    public class DinoStats
    {
        public double lat { get; set; }
        public double lon { get; set; }
        public int baseLevel { get; set; }
        public bool? female { get; set; }
        public WildLevels wildLevels { get; set; }
    }
}
