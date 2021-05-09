using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace ArkIntel
{
    /// <summary>
    /// Interaction logic for MapOverlay.xaml
    /// </summary>
    public partial class MapOverlay
    {
        private double coordScale = 4.4;

        public MapOverlay()
        {
            InitializeComponent();
            txtScale.Text = coordScale.ToString();
            MainWindow.LstDinosSelectionChanged += MainWindow_lstDinosSelectionChanged;
            MainWindow.DgDinoDataSelectionChanged += MainWindow_dgDinoDataSelectionChanged;
            DrawDinoLocations();
        }

        private void DrawDinoLocations()
        {
            canvasDinoPositions.Children.Clear();

            var dinoData = MainWindow.GlobalDinoData;

            var index = dinoData.Count;
            var i = 0;
            while (i < index)
            {
                DrawShape(10, ScaleCoordsToMap(dinoData[i].location.lon), ScaleCoordsToMap(dinoData[i].location.lat), Colors.Black, ConvertLevelToColor(dinoData[i].baseLevel), ConvertLevelToZIndex(dinoData[i].baseLevel));
                i++;
            }
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            KeyDown += MapOverlay_KeyDown;
        }
    
        private void MapOverlay_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            MainWindow.MapOverlayOpen = false;
        }

        private void DrawShape(int circleSize, double x, double y, Color strokeColor, Color fillColor, int zindex)
        {
            var dotSize = circleSize;

            var currentDot = new Ellipse
            {
                Stroke = new SolidColorBrush(strokeColor),
                StrokeThickness = 2,
                Height = dotSize,
                Width = dotSize,
                Fill = new SolidColorBrush(fillColor),
                Margin = new Thickness(x, y, 0, 0)
            };
            // Sets the position.
            canvasDinoPositions.Children.Add(currentDot);
            Panel.SetZIndex(currentDot, zindex);

        }

        public double ScaleCoordsToMap(double coord)
        {
            return coord * coordScale;
        }

        public Color ConvertLevelToColor(int baseLevel)
        {
            int? input = baseLevel;
            if (input > 90)
            {
                return Colors.LightGreen;
            }
            else if (input > 60)
            {
                return Colors.Yellow;
            }
            else if (input > 30)
            {
                return Colors.Orange;
            }
            else return Colors.Red;
        }

        public int ConvertLevelToZIndex(int baseLevel)
        {
            int? input = baseLevel;
            if (input > 90)
            {
                return 4;
            }
            else if (input > 60)
            {
                return 3;
            }
            else if (input > 30)
            {
                return 2;
            }
            else return 1;
        }

        private void MainWindow_lstDinosSelectionChanged(object sender, EventArgs e)
        {
            DrawDinoLocations();
        }

        private void MainWindow_dgDinoDataSelectionChanged(object sender, EventArgs e)
        {
            canvasDinoPositionsHighlighted.Children.Clear();

            var dinoData = MainWindow.GlobalDinoData;
            var i = ((MainWindow)Application.Current.MainWindow).dgDinoData.SelectedIndex;

            if (i == -1) return;

            // Separate DrawShape logic so we can animate the the specific shape.
            const int dotSize = 10;
            var currentDot = new Ellipse
            {
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 2,
                Height = dotSize,
                Width = dotSize,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Fill = new SolidColorBrush(Colors.White),
                Margin = new Thickness(ScaleCoordsToMap(dinoData[i].location.lon), ScaleCoordsToMap(dinoData[i].location.lat), 0, 0)
            };
            // Sets the position.
            canvasDinoPositionsHighlighted.Children.Add(currentDot);

            var trans = new ScaleTransform();
            currentDot.RenderTransform = trans;
            var animation = new DoubleAnimation
            {
                From = 1,
                To = 2,
                Duration = TimeSpan.FromSeconds(1),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever
            };
            trans.BeginAnimation(ScaleTransform.ScaleXProperty, animation);
            trans.BeginAnimation(ScaleTransform.ScaleYProperty, animation);
        }

        private void btnChangeScale_Click(object sender, RoutedEventArgs e)
        {
            coordScale = double.Parse(txtScale.Text);
            DrawDinoLocations();
        }
    }
}
