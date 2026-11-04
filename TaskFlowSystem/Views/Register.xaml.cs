using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Microsoft.Win32;

namespace TaskFlowSystem
{
    /// <summary>
    /// Локальная регистрация: создание нового профиля без пароля и проверок.
    /// Окно возвращает созданный профиль через свойство CreatedProfile.
    /// </summary>
    public partial class Register : Window
    {
        private const int MaxNameLength = 40;
        private const int MinNameLength = 2;
        private const int MaxDescriptionLength = 80;

        private static readonly Color PlaceholderNameColor = Color.FromRgb(0x4E, 0x61, 0x57);
        private static readonly Color ActiveNameColor = Color.FromRgb(0xEA, 0xF6, 0xEF);

        private string _pendingAvatarPath;

        public Register()
        {
            InitializeComponent();
            AccentGreen.IsChecked = true;
            PlayEntrance();
        }

        /// <summary>
        /// Созданный профиль или null, если окно закрыли без сохранения.
        /// </summary>
        public LocalProfile CreatedProfile { get; private set; }

        private void PlayEntrance()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            NameBox.Focus();
            OnNameChanged(NameBox, null);
            OnDescriptionChanged(DescriptionBox, null);
            ApplyAccent(GetCheckedSwatch());
        }

        private RadioButton GetCheckedSwatch()
        {
            if (AccentBlue.IsChecked == true) return AccentBlue;
            if (AccentViolet.IsChecked == true) return AccentViolet;
            if (AccentAmber.IsChecked == true) return AccentAmber;
            if (AccentRose.IsChecked == true) return AccentRose;
            return AccentGreen;
        }

        private void ApplyAccent(RadioButton swatch)
        {
            var brush = swatch != null ? swatch.Background : null;
            if (brush == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(_pendingAvatarPath))
            {
                AvatarPreview.Background = brush;
            }

            PreviewDescription.Foreground = CreateReadableAccent(brush);
        }

        private void OnNameChanged(object sender, TextChangedEventArgs e)
        {
            var name = (NameBox.Text ?? string.Empty).Trim();
            var isEmpty = name.Length == 0;

            PreviewName.Text = isEmpty ? "Имя не указано" : Truncate(name, MaxNameLength);
            PreviewName.Foreground = new SolidColorBrush(isEmpty ? PlaceholderNameColor : ActiveNameColor);
            AvatarInitial.Text = isEmpty ? "?" : FirstLetter(name);

            if (ErrorText.Visibility == Visibility.Visible && !isEmpty)
            {
                ErrorText.Visibility = Visibility.Collapsed;
            }
        }

        private void OnDescriptionChanged(object sender, TextChangedEventArgs e)
        {
            var description = (DescriptionBox.Text ?? string.Empty).Trim();
            var isEmpty = description.Length == 0;

            PreviewDescription.Text = isEmpty ? "Без описания" : Truncate(description, MaxDescriptionLength);
            PreviewDescription.Foreground = isEmpty
                ? new SolidColorBrush(PlaceholderNameColor)
                : CreateReadableAccent(GetCheckedSwatch().Background);
        }

        private void OnAccentChecked(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            ApplyAccent(sender as RadioButton);
        }

        private void OnPickAvatarClick(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Фотография профиля",
                Filter = "Изображения (*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff)|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|Все файлы (*.*)|*.*",
                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                var brush = ProfileAvatar.CreatePreviewBrush(dialog.FileName);
                _pendingAvatarPath = dialog.FileName;
                ApplyAvatarPreview(brush);
                ErrorText.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                ShowError("Не удалось использовать фотографию: " + ex.Message, false);
            }
        }

        private void OnRemoveAvatarClick(object sender, RoutedEventArgs e)
        {
            ResetAvatarPreview();
        }

        private void ApplyAvatarPreview(ImageBrush brush)
        {
            AvatarPreview.Background = brush;
            AvatarInitial.Visibility = Visibility.Collapsed;
            RemoveAvatarButton.Visibility = Visibility.Visible;
            AvatarButton.ToolTip = "Изменить фотографию";
            System.Windows.Automation.AutomationProperties.SetName(
                AvatarButton,
                "Изменить фотографию профиля");
        }

        private void ResetAvatarPreview()
        {
            _pendingAvatarPath = null;
            AvatarPreview.Background = GetCheckedSwatch().Background;
            AvatarInitial.Visibility = Visibility.Visible;
            RemoveAvatarButton.Visibility = Visibility.Collapsed;
            AvatarButton.ToolTip = "Добавить фотографию";
            System.Windows.Automation.AutomationProperties.SetName(
                AvatarButton,
                "Добавить фотографию профиля");
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                CreateProfile();
            }
            else if (e.Key == Key.Escape)
            {
                Cancel();
            }
        }

        private void OnCreateClick(object sender, RoutedEventArgs e)
        {
            CreateProfile();
        }

        private void OnCancelClick(object sender, RoutedEventArgs e)
        {
            Cancel();
        }

        private void CreateProfile()
        {
            var name = (NameBox.Text ?? string.Empty).Trim();
            if (name.Length < MinNameLength)
            {
                ShowError("Введите имя — не меньше двух символов.", true);
                NameBox.Focus();
                return;
            }

            var description = (DescriptionBox.Text ?? string.Empty).Trim();

            var profile = new LocalProfile
            {
                Id = Guid.NewGuid().ToString("N"),
                Name = Truncate(name, MaxNameLength),
                Description = description.Length == 0 ? null : Truncate(description, MaxDescriptionLength),
                Color = GetSelectedAccentKey(),
                CreatedUtc = DateTime.UtcNow
            };

            if (!string.IsNullOrEmpty(_pendingAvatarPath))
            {
                try
                {
                    profile.AvatarFile = ProfileAvatar.Save(profile.Id, _pendingAvatarPath);
                }
                catch (Exception ex)
                {
                    ShowError("Не удалось сохранить фотографию профиля: " + ex.Message, false);
                    return;
                }
            }

            CreatedProfile = profile;
            DialogResult = true;
        }

        private void Cancel()
        {
            DialogResult = false;
        }

        private void ShowError(string message, bool shakeName)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;

            if (!shakeName)
            {
                return;
            }

            var shake = new DoubleAnimationUsingKeyFrames();
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(60))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(8, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(140))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(-5, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(220))));
            shake.KeyFrames.Add(new EasingDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(300))));

            var transform = NameBox.RenderTransform as TranslateTransform;
            if (transform == null)
            {
                transform = new TranslateTransform();
                NameBox.RenderTransform = transform;
            }

            transform.BeginAnimation(TranslateTransform.XProperty, shake);
        }

        private string GetSelectedAccentKey()
        {
            if (AccentBlue.IsChecked == true) return "Blue";
            if (AccentViolet.IsChecked == true) return "Violet";
            if (AccentAmber.IsChecked == true) return "Amber";
            if (AccentRose.IsChecked == true) return "Rose";
            return "Green";
        }

        private static string FirstLetter(string name)
        {
            var first = name[0];
            return char.IsLetter(first) ? first.ToString().ToUpperInvariant() : "#";
        }

        private static string Truncate(string value, int maxLength)
        {
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        /// <summary>
        /// Немного осветлённый оттенок акцента для текста на тёмном фоне.
        /// </summary>
        private static Brush CreateReadableAccent(Brush brush)
        {
            var solid = brush as SolidColorBrush;
            if (solid != null)
            {
                return new SolidColorBrush(Lighten(solid.Color, 0.35));
            }

            var gradient = brush as GradientBrush;
            if (gradient != null && gradient.GradientStops.Count > 0)
            {
                return new SolidColorBrush(Lighten(gradient.GradientStops[0].Color, 0.35));
            }

            return new SolidColorBrush(Color.FromRgb(0x8A, 0xA3, 0x94));
        }

        private static Color Lighten(Color color, double amount)
        {
            return Color.FromRgb(
                (byte)(color.R + (255 - color.R) * amount),
                (byte)(color.G + (255 - color.G) * amount),
                (byte)(color.B + (255 - color.B) * amount));
        }
    }
}
