using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace GraphicsGame
{
    public partial class MainWindow : Window
    {
        private enum ShapeType { None, Circle, Rectangle }
        private ShapeType currentShape = ShapeType.None;
        private Point firstClickPosition;
        private bool isFirstClick = true;
        private ArrayList shapes = new ArrayList();
        private List<MovingShape> movingShapes = new List<MovingShape>();
        private DispatcherTimer timer;
        private TextBlock gun;
        private List<TextBlock> bullets = new List<TextBlock>();
        private double gunAngle = 0;

        private class MovingShape
        {
            public UIElement Shape { get; set; }
            public Vector Velocity { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(0.05);
            timer.Tick += Timer_Tick;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            MoveShapes(6);
            MoveBullets();
            CheckCollisions();
        }

        private void ShapeCanvas_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (currentShape == ShapeType.None) return;

            Point currentPosition = e.GetPosition(ShapeCanvas);

            if (isFirstClick)
            {
                firstClickPosition = currentPosition;
                isFirstClick = false;
            }
            else
            {
                string? selectedColor = ((ComboBoxItem)ColorPicker.SelectedItem)?.Content.ToString();
                Color color = selectedColor switch
                {
                    "Red" => Colors.Red,
                    "Green" => Colors.Green,
                    "Blue" => Colors.Blue,
                    "Yellow" => Colors.Yellow,
                    _ => Colors.Black
                };

                if (currentShape == ShapeType.Circle)
                {
                    DrawCircle(firstClickPosition, currentPosition, color);
                }
                else if (currentShape == ShapeType.Rectangle)
                {
                    DrawRectangle(firstClickPosition, currentPosition, color);
                }

                isFirstClick = true;
            }
        }

        private void DrawCircle(Point center, Point rim, Color color)
        {
            double radius = (center - rim).Length;
            Ellipse ellipse = new Ellipse
            {
                Width = 2 * radius,
                Height = 2 * radius,
                Stroke = new SolidColorBrush(color)
            };
            ellipse.SetValue(Canvas.LeftProperty, center.X - radius);
            ellipse.SetValue(Canvas.TopProperty, center.Y - radius);
            ShapeCanvas.Children.Add(ellipse);
            shapes.Add(ellipse);
            movingShapes.Add(new MovingShape { Shape = ellipse, Velocity = GetRandomVelocity() });
        }

        private void DrawRectangle(Point topLeft, Point bottomRight, Color color)
        {
            Rectangle rectangle = new Rectangle
            {
                Width = Math.Abs(bottomRight.X - topLeft.X),
                Height = Math.Abs(bottomRight.Y - topLeft.Y),
                Stroke = new SolidColorBrush(color)
            };
            rectangle.SetValue(Canvas.LeftProperty, Math.Min(topLeft.X, bottomRight.X));
            rectangle.SetValue(Canvas.TopProperty, Math.Min(topLeft.Y, bottomRight.Y));
            ShapeCanvas.Children.Add(rectangle);
            shapes.Add(rectangle);
            movingShapes.Add(new MovingShape { Shape = rectangle, Velocity = GetRandomVelocity() });
        }

        private void CircleButton_Click(object sender, RoutedEventArgs e)
        {
            currentShape = ShapeType.Circle;
        }

        private void RectangleButton_Click(object sender, RoutedEventArgs e)
        {
            currentShape = ShapeType.Rectangle;
        }

        private void StepButton_Click(object sender, RoutedEventArgs e)
        {
            MoveShapes(2);
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Start();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            timer.Stop();
        }

        private void FireButton_Click(object sender, RoutedEventArgs e)
        {
            InitializeGun();
        }

        private void MoveShapes(double offset)
        {
            // Move each shape
            foreach (var movingShape in movingShapes)
            {
                double left = (double)movingShape.Shape.GetValue(Canvas.LeftProperty);
                double top = (double)movingShape.Shape.GetValue(Canvas.TopProperty);
                double shapeWidth = (movingShape.Shape is Shape shape) ? shape.Width : 0;
                double shapeHeight = (movingShape.Shape is Shape shapeH) ? shapeH.Height : 0;

                // Calculate new position
                double newLeft = left + movingShape.Velocity.X * offset;
                double newTop = top + movingShape.Velocity.Y * offset;

                // Check collision with canvas boundaries
                if (newLeft + shapeWidth >= ShapeCanvas.ActualWidth || newLeft <= 0)
                {
                    movingShape.Velocity = new Vector(-movingShape.Velocity.X, movingShape.Velocity.Y);
                    newLeft = Math.Clamp(newLeft, 0, ShapeCanvas.ActualWidth - shapeWidth);
                }

                if (newTop + shapeHeight >= ShapeCanvas.ActualHeight || newTop <= 0)
                {
                    movingShape.Velocity = new Vector(movingShape.Velocity.X, -movingShape.Velocity.Y);
                    newTop = Math.Clamp(newTop, 0, ShapeCanvas.ActualHeight - shapeHeight);
                }

                // Check collision with other shapes
                foreach (var otherShape in movingShapes)
                {
                    if (otherShape != movingShape)
                    {
                        // Get current positions and sizes
                        double otherLeft = (double)otherShape.Shape.GetValue(Canvas.LeftProperty);
                        double otherTop = (double)otherShape.Shape.GetValue(Canvas.TopProperty);
                        double otherWidth = (otherShape.Shape is Shape otherShapeH) ? otherShapeH.Width : 0;
                        double otherHeight = (otherShape.Shape is Shape otherShapeV) ? otherShapeV.Height : 0;

                        // Calculate bounding boxes
                        Rect currentRect = new Rect(newLeft, newTop, shapeWidth, shapeHeight);
                        Rect otherRect = new Rect(otherLeft, otherTop, otherWidth, otherHeight);

                        // Check if bounding boxes intersect
                        if (currentRect.IntersectsWith(otherRect))
                        {
                            // Calculate overlap depths
                            double overlapLeft = currentRect.Right - otherRect.Left;
                            double overlapRight = otherRect.Right - currentRect.Left;
                            double overlapTop = currentRect.Bottom - otherRect.Top;
                            double overlapBottom = otherRect.Bottom - currentRect.Top;

                            // Resolve overlap
                            if (overlapLeft < overlapRight && overlapLeft < overlapTop && overlapLeft < overlapBottom)
                            {
                                newLeft -= overlapLeft;
                                movingShape.Velocity = new Vector(-movingShape.Velocity.X, movingShape.Velocity.Y);
                            }
                            else if (overlapRight < overlapLeft && overlapRight < overlapTop && overlapRight < overlapBottom)
                            {
                                newLeft += overlapRight;
                                movingShape.Velocity = new Vector(-movingShape.Velocity.X, movingShape.Velocity.Y);
                            }
                            else if (overlapTop < overlapLeft && overlapTop < overlapRight && overlapTop < overlapBottom)
                            {
                                newTop -= overlapTop;
                                movingShape.Velocity = new Vector(movingShape.Velocity.X, -movingShape.Velocity.Y);
                            }
                            else if (overlapBottom < overlapLeft && overlapBottom < overlapRight && overlapBottom < overlapTop)
                            {
                                newTop += overlapBottom;
                                movingShape.Velocity = new Vector(movingShape.Velocity.X, -movingShape.Velocity.Y);
                            }
                        }
                    }
                }

                // Update shape position
                movingShape.Shape.SetValue(Canvas.LeftProperty, newLeft);
                movingShape.Shape.SetValue(Canvas.TopProperty, newTop);
            }
        }



        private Vector GetRandomVelocity()
        {
            Random rand = new Random();
            return new Vector(rand.NextDouble() * 2 - 1, rand.NextDouble() * 2 - 1);
        }

        private void InitializeGun()
        {
            if (gun != null)
            {
                ShapeCanvas.Children.Remove(gun);
            }
            gun = new TextBlock
            {
                Text = "A",
                Foreground = Brushes.White,
                FontSize = 24
            };
            ShapeCanvas.Children.Add(gun);
            Canvas.SetLeft(gun, ShapeCanvas.ActualWidth / 2);
            Canvas.SetTop(gun, ShapeCanvas.ActualHeight - 30);
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            double left = Canvas.GetLeft(gun);
            if (e.Key == Key.NumPad4 && left > 0)
            {
                Canvas.SetLeft(gun, left - 5);
            }
            else if (e.Key == Key.NumPad6 && left < ShapeCanvas.ActualWidth - gun.ActualWidth)
            {
                Canvas.SetLeft(gun, left + 5);
            }
            else if (e.Key == Key.NumPad7) // Adjust gun angle to the left
            {
                gunAngle -= 0.1; // Decrease the angle
                RotateGun();
            }
            else if (e.Key == Key.NumPad9) // Adjust gun angle to the right
            {
                gunAngle += 0.1; // Increase the angle
                RotateGun();
            }
            else if (e.Key == Key.S)
            {
                FireBullet();
            }
        }

        private void RotateGun()
        {
            gun.RenderTransformOrigin = new Point(0.5, 1); // Rotate around the bottom center of the gun
            gun.RenderTransform = new RotateTransform(gunAngle * 180 / Math.PI); // Convert radians to degrees
        }

        private void FireBullet()
        {
            TextBlock bullet = new TextBlock
            {
                Text = "|",
                Foreground = Brushes.White,
                FontSize = 24
            };
            double gunLeft = Canvas.GetLeft(gun);
            double gunTop = Canvas.GetTop(gun);

            double bulletXVelocity = Math.Cos(gunAngle); // Calculate X velocity based on gun angle
            double bulletYVelocity = Math.Sin(gunAngle); // Calculate Y velocity based on gun angle

            Canvas.SetLeft(bullet, gunLeft + gun.ActualWidth / 2);
            Canvas.SetTop(bullet, gunTop - 20);
            ShapeCanvas.Children.Add(bullet);
            bullets.Add(bullet);

            // Move bullet based on velocity
            DispatcherTimer bulletTimer = new DispatcherTimer();
            bulletTimer.Interval = TimeSpan.FromMilliseconds(50); // Adjust bullet speed here
            bulletTimer.Tick += (sender, e) =>
            {
                double bulletLeft = Canvas.GetLeft(bullet);
                double bulletTop = Canvas.GetTop(bullet);
                Canvas.SetLeft(bullet, bulletLeft + bulletXVelocity * 5); // Adjust bullet speed here
                Canvas.SetTop(bullet, bulletTop - bulletYVelocity * 5); // Adjust bullet speed here
            };
            bulletTimer.Start();
        }



        private void MoveBullets()
        {
            List<TextBlock> bulletsToRemove = new List<TextBlock>();
            foreach (var bullet in bullets)
            {
                double top = Canvas.GetTop(bullet);
                Canvas.SetTop(bullet, top - 10);
                if (top < 0)
                {
                    bulletsToRemove.Add(bullet);
                }
            }

            foreach (var bullet in bulletsToRemove)
            {
                ShapeCanvas.Children.Remove(bullet);
                bullets.Remove(bullet);
            }
        }

        private void CheckCollisions()
        {
            List<UIElement> shapesToRemove = new List<UIElement>();
            List<TextBlock> bulletsToRemove = new List<TextBlock>();

            foreach (var bullet in bullets)
            {
                Rect bulletRect = new Rect(Canvas.GetLeft(bullet), Canvas.GetTop(bullet), bullet.ActualWidth, bullet.ActualHeight);
                foreach (var shape in shapes)
                {
                    if (shape is UIElement shapeElement)
                    {
                        Rect shapeRect = new Rect(Canvas.GetLeft(shapeElement), Canvas.GetTop(shapeElement), shapeElement.RenderSize.Width, shapeElement.RenderSize.Height);
                        if (bulletRect.IntersectsWith(shapeRect))
                        {
                            shapesToRemove.Add(shapeElement);
                            bulletsToRemove.Add(bullet);
                            break;
                        }
                    }
                }
            }

            foreach (var shape in shapesToRemove)
            {
                ShapeCanvas.Children.Remove(shape);
                shapes.Remove(shape);
            }

            foreach (var bullet in bulletsToRemove)
            {
                ShapeCanvas.Children.Remove(bullet);
                bullets.Remove(bullet);
            }
        }

    }
}
