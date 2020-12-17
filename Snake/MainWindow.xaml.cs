using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace Snake
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        bool isPlaying = false;
        // Параметры размера змейки
        int SnakeSquareSize = 40;
        const int SnakeStartLength = 3;
        const int SnakeStartSpeed = 400;
        const int SnakeSpeedThreshold = 100; // мин. скорость

        // Парметры цвета змейки
        private SolidColorBrush snakeBodyBrush = Brushes.Green;
        private SolidColorBrush snakeHeadBrush = Brushes.YellowGreen;
        private List<SnakePart> snakeParts = new List<SnakePart>();
        // Параметры движения змейки
        public enum SnakeDirection { Left, Right, Up, Down };
        private SnakeDirection snakeDirection = SnakeDirection.Right;
        private int snakeLength;
        private int currentScore = 0;
        private DispatcherTimer gameTickTimer = new DispatcherTimer();

        // Парметры еды
        private Random rnd = new Random();
        private UIElement snakeFood = null;
        private SolidColorBrush foodBrush = Brushes.Red;
        public MainWindow()
        {
            InitializeComponent();
            gameTickTimer.Tick += GameTickTimer_Tick;
        }
        private void OnContentRendered(object sender, EventArgs e)
        {
            DrawGameArea();
            //StartNewGame();
        }
        private void GameTickTimer_Tick(object sender, EventArgs e)
        {
            MoveSnake();
        }
        private void DrawGameArea()
        {
            // Нарисуем сетку
            bool doneDrawingBackground = false;
            int nextX = 0, nextY = 0;
            int rowCounter = 0;
            bool nextIsOdd = false;
            while (doneDrawingBackground == false)
            {
                Rectangle rect = new Rectangle
                {
                    Width = SnakeSquareSize,
                    Height = SnakeSquareSize,
                    Fill = nextIsOdd ? Brushes.White : Brushes.LightGray
                };
                GameArea.Children.Add(rect);
                Canvas.SetTop(rect, nextY);
                Canvas.SetLeft(rect, nextX);

                nextIsOdd = !nextIsOdd;
                nextX += SnakeSquareSize;
                if (nextX >= GameArea.ActualWidth)
                {
                    nextX = 0;
                    nextY += SnakeSquareSize;
                    rowCounter++;
                    nextIsOdd = (rowCounter % 2 != 0);
                }

                if (nextY >= GameArea.ActualHeight)
                    doneDrawingBackground = true;
            }
        }
        private void DrawSnake()
        {
            foreach (SnakePart snakePart in snakeParts)
            {
                if (snakePart.UiElement == null)
                {
                    snakePart.UiElement = new Rectangle()
                    {
                        Width = SnakeSquareSize,
                        Height = SnakeSquareSize,
                        Fill = (snakePart.IsHead ? snakeHeadBrush : snakeBodyBrush)
                    };
                    GameArea.Children.Add(snakePart.UiElement);
                    Canvas.SetTop(snakePart.UiElement, snakePart.Position.Y);
                    Canvas.SetLeft(snakePart.UiElement, snakePart.Position.X);
                }
            }
        }
        private void MoveSnake()
        {
            // Могла увеличиться длина змейки
            // Удаление последней части змейки  
            while (snakeParts.Count >= snakeLength)
            {
                GameArea.Children.Remove(snakeParts[0].UiElement);
                snakeParts.RemoveAt(0);
            }

            // Удалим старую голову
            (snakeParts[snakeParts.Count - 1].UiElement as Rectangle).Fill = snakeBodyBrush;
            snakeParts[snakeParts.Count - 1].IsHead = false;

            // Определяем сторону, в которую будет двигаться змейка  
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];
            double nextX = snakeHead.Position.X;
            double nextY = snakeHead.Position.Y;
            switch (snakeDirection)
            {
                case SnakeDirection.Left:
                    nextX -= SnakeSquareSize;
                    break;
                case SnakeDirection.Right:
                    nextX += SnakeSquareSize;
                    break;
                case SnakeDirection.Up:
                    nextY -= SnakeSquareSize;
                    break;
                case SnakeDirection.Down:
                    nextY += SnakeSquareSize;
                    break;
            }

            // Добавляем "новую голову" по направлению движения
            snakeParts.Add(new SnakePart()
            {
                Position = new Point(nextX, nextY),
                IsHead = true
            });
            DrawSnake();
            // Проверка столкновений
            DoCollisionCheck();          
        }

        private void StartNewGame()
        {
            // Удаление змейки из предыдущей игры
            foreach (SnakePart snakeBodyPart in snakeParts)
            {
                if (snakeBodyPart.UiElement != null)
                    GameArea.Children.Remove(snakeBodyPart.UiElement);
            }
            snakeParts.Clear();
            if (snakeFood != null)
                GameArea.Children.Remove(snakeFood);

            // Перезапуск
            currentScore = 0;
            snakeLength = SnakeStartLength;
            snakeDirection = SnakeDirection.Right;
            snakeParts.Add(new SnakePart() { Position = new Point(SnakeSquareSize * 5, SnakeSquareSize * 5) });
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(SnakeStartSpeed);
            DrawGameArea();
            DrawSnake();
            DrawSnakeFood();
            UpdateGameStatus();
            gameTickTimer.IsEnabled = true;
        }
        private Point GetNextFoodPosition()
        {
            int maxX = (int)(GameArea.ActualWidth / SnakeSquareSize);
            int maxY = (int)(GameArea.ActualHeight / SnakeSquareSize);
            int foodX = rnd.Next(0, maxX) * SnakeSquareSize;
            int foodY = rnd.Next(0, maxY) * SnakeSquareSize;

            foreach (SnakePart snakePart in snakeParts)
            {
                if ((snakePart.Position.X == foodX) && (snakePart.Position.Y == foodY))
                    return GetNextFoodPosition();
            }

            return new Point(foodX, foodY);
        }

        private void DrawSnakeFood()
        {
            Point foodPosition = GetNextFoodPosition();
            snakeFood = new Ellipse()
            {
                Width = SnakeSquareSize,
                Height = SnakeSquareSize,
                Fill = foodBrush
            };
            GameArea.Children.Add(snakeFood);
            Canvas.SetTop(snakeFood, foodPosition.Y);
            Canvas.SetLeft(snakeFood, foodPosition.X);
        }
        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            SnakeDirection originalSnakeDirection = snakeDirection;
            switch (e.Key)
            {
                case Key.Up:
                    if (snakeDirection != SnakeDirection.Down)
                        snakeDirection = SnakeDirection.Up;
                    break;
                case Key.Down:
                    if (snakeDirection != SnakeDirection.Up)
                        snakeDirection = SnakeDirection.Down;
                    break;
                case Key.Left:
                    if (snakeDirection != SnakeDirection.Right)
                        snakeDirection = SnakeDirection.Left;
                    break;
                case Key.Right:
                    if (snakeDirection != SnakeDirection.Left)
                        snakeDirection = SnakeDirection.Right;
                    break;
                //case Key.Space:
                    //StartNewGame();
                    //break;
            }
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }
        private void GameArea_MouseLeftButtonDown(object sender, RoutedEventArgs e)
        {
            Point mousePosition = Mouse.GetPosition(GameArea);
            Point snakeHeadPosition = snakeParts[snakeParts.Count - 1].Position;
            SnakeDirection originalSnakeDirection = snakeDirection;
            //// Вверх, вниз
            //if (mousePosition.X <= snakeHeadPosition.X + SnakeSquareSize*2 && mousePosition.X >= snakeHeadPosition.X - SnakeSquareSize*2)
            //{
            //    // Вверх
            //    if (mousePosition.Y <= snakeHeadPosition.Y)
            //    {
            //        snakeDirection = SnakeDirection.Up;
            //    }
            //    // Вниз
            //    else
            //    {
            //        snakeDirection = SnakeDirection.Down;
            //    }
            //}
            //// Влево, вправо
            //if (mousePosition.Y <= snakeHeadPosition.Y + SnakeSquareSize*2 && mousePosition.Y >= snakeHeadPosition.Y - SnakeSquareSize*2)
            //{
            //    // Влево
            //    if (mousePosition.X <= snakeHeadPosition.X)
            //    {
            //        snakeDirection = SnakeDirection.Left;
            //    }
            //    // Вниз
            //    else
            //    {
            //        snakeDirection = SnakeDirection.Right;
            //    }
            //}
            if (snakeDirection != originalSnakeDirection)
                MoveSnake();
        }
        private void EatSnakeFood()
        {
            snakeLength++;
            currentScore++;
            int timerInterval = Math.Max(SnakeSpeedThreshold, (int)gameTickTimer.Interval.TotalMilliseconds - (currentScore * 2));
            gameTickTimer.Interval = TimeSpan.FromMilliseconds(timerInterval);
            GameArea.Children.Remove(snakeFood);
            DrawSnakeFood();
            UpdateGameStatus();
        }
        private void UpdateGameStatus()
        {
            this.Title = "Snake - Score: " + currentScore + " - Game speed: " + gameTickTimer.Interval.TotalMilliseconds;
        }
        private void EndGame()
        {
            isPlaying = false;
            gameTickTimer.IsEnabled = false;
            MessageBox.Show("Oooops, you died!", "Snake");
        }
        private void DoCollisionCheck()
        {
            SnakePart snakeHead = snakeParts[snakeParts.Count - 1];

            if ((snakeHead.Position.X == Canvas.GetLeft(snakeFood)) && (snakeHead.Position.Y == Canvas.GetTop(snakeFood)))
            {
                EatSnakeFood();
                return;
            }

            if ((snakeHead.Position.Y < 0) || (snakeHead.Position.Y >= GameArea.ActualHeight) ||
            (snakeHead.Position.X < 0) || (snakeHead.Position.X >= GameArea.ActualWidth))
            {
                EndGame();
            }

            foreach (SnakePart snakeBodyPart in snakeParts.Take(snakeParts.Count - 1))
            {
                if ((snakeHead.Position.X == snakeBodyPart.Position.X) && (snakeHead.Position.Y == snakeBodyPart.Position.Y))
                    EndGame();
            }
        }
        private void Start_Click(object sender, EventArgs e)
        {

            int size = Int32.Parse(textBox1.Text);
            if (size >= 15 && size <= 70)
            {
                SnakeSquareSize = size;
                StartNewGame();
                isPlaying = true;
            }
            else 
            {
                MessageBox.Show("Square size must be between 15 and 70.");
            }
        }

        private void Resume_Pause_Click(object sender, EventArgs e)
        {
            if (isPlaying)
            {
                isPlaying = false;
                gameTickTimer.IsEnabled = false;
            }
            else 
            {
                isPlaying = true;
                gameTickTimer.IsEnabled = true;
            }
        }
        private void TextBox_TextChanged(object sender, EventArgs e)
        {
            if (System.Text.RegularExpressions.Regex.IsMatch(textBox1.Text, "[^0-9]"))
            {
                MessageBox.Show("Please enter only numbers.");
                textBox1.Text = textBox1.Text.Remove(textBox1.Text.Length - 1);
            }
        }
        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

    }
}
