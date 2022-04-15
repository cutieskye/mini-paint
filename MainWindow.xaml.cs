// https://stackoverflow.com/questions/4022746/wpf-add-a-dropshadow-effect-to-an-element-from-code-behind
// https://docs.microsoft.com/en-us/dotnet/api/system.reflection.propertyinfo.getvalue?view=net-5.0
// https://stackoverflow.com/questions/946544/good-text-foreground-color-for-a-given-background-color
// https://stackoverflow.com/questions/62315767/how-to-draw-a-rectangle-on-a-canvas-with-mouse-but-see-the-rectangle-while-movi
// https://programmingpages.wordpress.com/2012/01/22/dragging-shapes-with-the-mouse-in-wpf/
// https://jasonkemp.ca/blog/how-to-save-xaml-as-an-image/

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Mini_Paint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ObservableCollection<Color> colors = new();
        List<Shape> myShapes = new();
        Shape currentShape;
        Point? startPoint;
        Point? endPoint;
        double x_shape, x_canvas, y_shape, y_canvas;

        public MainWindow()
        {
            InitializeComponent();
            InitializeComboBox();
            for (int i = 0; i < 4; ++i)
            {
                Shape shape = new Random().Next(2) == 0 ? new Rectangle() : new Ellipse();
                DrawShape(shape);
            }
        }

        private void InitializeComboBox()
        {
            foreach (var prop in typeof(Colors).GetProperties(BindingFlags.Static |
                                                              BindingFlags.Public))
            {
                if (prop.Name == "Transparent")
                    continue;

                var color = (Color)prop.GetValue(null, null);
                colors.Add(color);
                double luma = 0.2126 * color.ScR + 0.7152 * color.ScG + 0.0722 * color.ScB;

                var textBlock = new TextBlock
                {
                    Background = new SolidColorBrush(color),
                    Foreground = luma < 0.5 ? new SolidColorBrush(Colors.White) : new SolidColorBrush(Colors.Black),
                    Text = prop.Name
                };

                myComboBox.Items.Add(textBlock);
            }
        }

        private void DrawShape(Shape shape)
        {
            var random = new Random();
            shape.Width = random.Next(50, 300);
            shape.Height = random.Next(50, 300);
            Canvas.SetLeft(shape, random.Next((int)myWindow.MinWidth * 2));
            Canvas.SetTop(shape, random.Next((int)myWindow.MinHeight * 2));

            shape.Fill = new SolidColorBrush(colors[new Random().Next(colors.Count)]);

            shape.Cursor = Cursors.Hand;
            shape.MouseRightButtonDown += Shape_MouseRightButtonDown;
            shape.MouseLeftButtonDown += Shape_MouseLeftButtonDown;
            shape.MouseMove += Shape_MouseMove;
            shape.MouseLeftButtonUp += Shape_MouseLeftButtonUp;

            myCanvas.Children.Add(shape);
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            currentShape = new Rectangle();
            DeselectShapes();
            myCanvas.Cursor = Cursors.Cross;
            currentShape.Fill = new SolidColorBrush(colors[new Random().Next(colors.Count)]);
        }

        private void EllipseButton_Click(object sender, RoutedEventArgs e)
        {
            currentShape = new Ellipse();
            DeselectShapes();
            myCanvas.Cursor = Cursors.Cross;
            currentShape.Fill = new SolidColorBrush(colors[new Random().Next(colors.Count)]);
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var shape in myShapes)
                myCanvas.Children.Remove(shape);
            myComboBox.SelectedItem = null;
            ClearValue(DataContextProperty);
            myShapes.Clear();
        }

        private void RandomColorsButton_Click(object sender, RoutedEventArgs e)
        {
            int index = new Random().Next(colors.Count);
            foreach (var shape in myShapes)
                shape.Fill = new SolidColorBrush(colors[index]);
        }

        private void ExportToPngButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new SaveFileDialog
            {
                DefaultExt = ".png",
                Filter = "PNG File|*.png"
            };
            if (dialog.ShowDialog() == true)
            {
                var rect = new Rect(myCanvas.RenderSize);
                var bitmap = new RenderTargetBitmap((int)rect.Right,
                    (int)rect.Bottom, 96, 96, PixelFormats.Default);
                bitmap.Render(myCanvas);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmap));
                using var stream = new MemoryStream();
                encoder.Save(stream);
                File.WriteAllBytes(dialog.FileName, stream.ToArray());
            }
        }

        private void DeselectShapes()
        {
            foreach (Shape shape in myCanvas.Children)
            {
                shape.ClearValue(EffectProperty);
                shape.ClearValue(CursorProperty);
                myComboBox.IsEnabled = false;
                myComboBox.SelectedItem = null;
            }
            myShapes.Clear();
        }

        private void SetMaxZIndex(Shape shape)
        {
            int maxZIndex = 0;
            foreach (UIElement element in myCanvas.Children)
            {
                int zIndex = Canvas.GetZIndex(element);
                if (zIndex > maxZIndex)
                    maxZIndex = zIndex;
            }
            Canvas.SetZIndex(shape, ++maxZIndex);
        }

        private void HighlightShape(Shape shape)
        {
            shape.Effect = new DropShadowEffect
            {
                BlurRadius = 50,
                Color = Colors.White,
                Direction = 270
            };
            SetMaxZIndex(shape);
            myComboBox.IsEnabled = true;
            myShapes.Add(shape);
            DataContext = myShapes.LastOrDefault();
        }

        private void Shape_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var shape = sender as Shape;
            if (myShapes.Contains(shape))
            {
                shape.ClearValue(EffectProperty);
                myComboBox.IsEnabled = false;
                myComboBox.SelectedItem = null;
                myShapes.Remove(shape);
            }
            else
                HighlightShape(shape);
        }

        private void Shape_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var shape = sender as Shape;
            DeselectShapes();
            HighlightShape(shape);
            Mouse.Capture(shape);
            shape.Cursor = Cursors.ScrollAll;
            x_shape = Canvas.GetLeft(shape);
            x_canvas = e.GetPosition(myCanvas).X;
            y_shape = Canvas.GetTop(shape);
            y_canvas = e.GetPosition(myCanvas).Y;
        }

        private void Shape_MouseMove(object sender, MouseEventArgs e)
        {
            var shape = sender as Shape;
            if (e.LeftButton != MouseButtonState.Pressed)
                return;
            double x = e.GetPosition(myCanvas).X;
            double y = e.GetPosition(myCanvas).Y;
            x_shape += x - x_canvas;
            Canvas.SetLeft(shape, x_shape);
            x_canvas = x;
            y_shape += y - y_canvas;
            Canvas.SetTop(shape, y_shape);
            y_canvas = y;
        }

        private void Shape_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Mouse.Capture(null);
            foreach (Shape shape in myCanvas.Children)
                shape.Cursor = Cursors.Hand;
        }

        private void MyCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentShape == null)
                return;
            startPoint = e.GetPosition(myCanvas);
            Canvas.SetLeft(currentShape, startPoint.Value.X);
            Canvas.SetTop(currentShape, startPoint.Value.Y);
            myCanvas.Children.Add(currentShape);
            SetMaxZIndex(currentShape);
        }

        private void MyCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Released || currentShape == null)
                return;
            endPoint = e.GetPosition(myCanvas);
            Canvas.SetLeft(currentShape, Math.Min(startPoint.Value.X, endPoint.Value.X));
            Canvas.SetTop(currentShape, Math.Min(startPoint.Value.Y, endPoint.Value.Y));
            currentShape.Width = Math.Abs(endPoint.Value.X - startPoint.Value.X);
            currentShape.Height = Math.Abs(endPoint.Value.Y - startPoint.Value.Y);
        }

        private void MyCanvas_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (currentShape == null)
                return;
            currentShape.MouseLeftButtonDown += Shape_MouseLeftButtonDown;
            currentShape.MouseMove += Shape_MouseMove;
            currentShape.MouseLeftButtonUp += Shape_MouseLeftButtonUp;
            currentShape = null;
            startPoint = null;
            endPoint = null;
            myCanvas.ClearValue(CursorProperty);
            foreach (Shape shape in myCanvas.Children)
                shape.Cursor = Cursors.Hand;
        }
    }
}
