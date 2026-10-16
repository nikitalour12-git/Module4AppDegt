using Module4AppDegt.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Module4AppDegt.View
{
    public partial class AuthWindow : Window
    {
        private const int CaptchaPiecesCount = 4;

        private static readonly string[] CaptchaFiles =
        {
            "CaptchaPart1.png",
            "CaptchaPart2.png",
            "CaptchaPart3.png",
            "CaptchaPart4.png"
        };

        private string login;
        private string password;
        private static int counterOfTries = 0;

        private int[] captchaOrder;

        private Border firstSelectedCell;
        private bool isCaptchaSolved = false;

        public AuthWindow()
        {
            InitializeComponent();
            InitializeCaptcha();
        }

        private void InitializeCaptcha()
        {
            try
            {
                captchaOrder = Enumerable.Range(0, CaptchaPiecesCount).ToArray();
                Shuffle(captchaOrder);

                while (IsCaptchaCorrect())
                {
                    Shuffle(captchaOrder);
                }

                CaptchaGrid.Children.Clear();
                for (int position = 0; position < CaptchaPiecesCount; position++)
                {
                    CaptchaGrid.Children.Add(
                        CreateCaptchaCell(CaptchaFiles[captchaOrder[position]], captchaOrder[position]));
                }

                firstSelectedCell = null;
                isCaptchaSolved = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Не удалось загрузить изображения капчи: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Border CreateCaptchaCell(string fileName, int pieceIndex)
        {
            BitmapImage bitmap = new BitmapImage(
                new Uri($"pack://application:,,,/Images/{fileName}", UriKind.Absolute));

            Image img = new Image
            {
                Source = bitmap,
                Stretch = Stretch.UniformToFill,
                Tag = pieceIndex
            };

            Border border = new Border
            {
                BorderBrush = Brushes.Gray,
                BorderThickness = new Thickness(1),
                Child = img,
                Tag = pieceIndex
            };
            border.MouseLeftButtonDown += CaptchaCell_Click;
            return border;
        }

        private void CaptchaCell_Click(object sender, MouseButtonEventArgs e)
        {
            if (isCaptchaSolved) return;

            Border clickedCell = sender as Border;
            if (clickedCell == null) return;

            if (firstSelectedCell == null)
            {
                firstSelectedCell = clickedCell;
                clickedCell.BorderBrush = Brushes.Red;
                clickedCell.BorderThickness = new Thickness(3);
            }
            else if (firstSelectedCell == clickedCell)
            {
                clickedCell.BorderBrush = Brushes.Gray;
                clickedCell.BorderThickness = new Thickness(1);
                firstSelectedCell = null;
            }
            else
            {
                SwapCells(firstSelectedCell, clickedCell);

                firstSelectedCell.BorderBrush = Brushes.Gray;
                firstSelectedCell.BorderThickness = new Thickness(1);
                firstSelectedCell = null;

                if (IsCaptchaCorrect())
                {
                    isCaptchaSolved = true;
                    CaptchaGrid.IsEnabled = false;
                    MessageBox.Show("Капча пройдена успешно", "Успех",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void SwapCells(Border a, Border b)
        {
            var aChild = a.Child;
            var aTag = a.Tag;
            var bChild = b.Child;
            var bTag = b.Tag;

            a.Child = null;
            b.Child = null;

            a.Child = bChild;
            a.Tag = bTag;
            b.Child = aChild;
            b.Tag = aTag;

            int posA = CaptchaGrid.Children.IndexOf(a);
            int posB = CaptchaGrid.Children.IndexOf(b);
            (captchaOrder[posA], captchaOrder[posB]) = (captchaOrder[posB], captchaOrder[posA]);
        }

        private bool IsCaptchaCorrect()
        {
            if (captchaOrder == null) return false;
            for (int i = 0; i < captchaOrder.Length; i++)
                if (captchaOrder[i] != i) return false;
            return true;
        }

        private static void Shuffle(int[] array)
        {
            Random rnd = new Random();
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = rnd.Next(i + 1);
                (array[i], array[j]) = (array[j], array[i]);
            }
        }

        private const int MaxTries = 3;


        private void Auth_Button_Click(object sender, RoutedEventArgs e)
        {
            login = LoginTextBox.Text;
            password = PasswordTextBox.Password;

            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Пожалуйста, заполните поля «Логин» и «Пароль».",
                    "Предупреждение", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!isCaptchaSolved)
            {
                RegisterFailedAttempt(login);
                return;
            }

            CheckData(login, password);
        }

        private void CheckData(string login, string password)
        {
            User user = Database.GetUser(login, password);

            if (user != null)
            {
                counterOfTries = 0;

                MessageBox.Show("Вы успешно авторизовались.",
                    "Успех", MessageBoxButton.OK, MessageBoxImage.Information);

            }
            else
            {
                RegisterFailedAttempt(login);
            }
        }

        private void RegisterFailedAttempt(string login)
        {
            counterOfTries++;

            MessageBox.Show("Вы ввели неверный логин или пароль. Пожалуйста проверьте ещё раз введенные данные.",
                "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);

            if (counterOfTries >= MaxTries)
            {
                Database.BlockUser(login);

                MessageBox.Show("Вы заблокированы. Обратитесь к администратору.",
                    "Доступ запрещён", MessageBoxButton.OK, MessageBoxImage.Warning);

                AuthBtn.IsEnabled = false;
                CaptchaGrid.IsEnabled = false;
            }
            else
            {
                isCaptchaSolved = false;
                InitializeCaptcha();
            }
        }
    }
}