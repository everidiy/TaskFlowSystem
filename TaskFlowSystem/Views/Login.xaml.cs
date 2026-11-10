using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace TaskFlowSystem
{
    /// <summary>
    /// Выбор локального профиля или создание нового.
    /// Выбранный профиль возвращается через свойство SelectedProfile.
    /// </summary>
    public partial class Login : Window
    {
        private bool _storageAvailable = true;

        public Login()
        {
            InitializeComponent();
            PlayEntrance();
        }

        /// <summary>
        /// Профиль, с которым нужно продолжить работу, или null, если окно закрыли.
        /// </summary>
        public LocalProfile SelectedProfile { get; private set; }

        /// <summary>
        /// Последний изменённый профиль — чтобы открытое главное окно
        /// могло обновить шапку, не перечитывая всё хранилище.
        /// </summary>
        public LocalProfile EditedProfile { get; private set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            Refresh();
        }

        private void PlayEntrance()
        {
            BeginAnimation(OpacityProperty, new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(260))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            });
        }

        private void Refresh()
        {
            ProfileList.Children.Clear();
            _storageAvailable = true;
            CreateProfileButton.IsEnabled = true;

            List<LocalProfile> profiles;
            try
            {
                profiles = ProfileStore.Load()
                    .OrderByDescending(p => p.CreatedUtc)
                    .ToList();
            }
            catch (Exception ex)
            {
                _storageAvailable = false;
                profiles = new List<LocalProfile>();
                CreateProfileButton.IsEnabled = false;
                ShowError(string.Format(
                    "Не удалось прочитать профили: {0} Файл не будет перезаписан. Проверьте папку {1}.",
                    ex.Message,
                    ProfileStore.StorageDirectory));
            }

            if (_storageAvailable)
            {
                HideError();
            }

            foreach (var profile in profiles)
            {
                var row = BuildProfileCard(profile);
                row.Margin = new Thickness(0, 0, 0, 10);
                ProfileList.Children.Add(row);
            }

            var isEmpty = profiles.Count == 0 && _storageAvailable;
            if (isEmpty)
            {
                ProfileList.Children.Add(BuildEmptyState());
            }

            CreateProfileButton.Content = BuildCreateButtonContent(
                isEmpty ? "Создать первый профиль" : "Новый профиль");

            HeaderSubtitle.Text = _storageAvailable
                ? (isEmpty
                    ? "Профилей пока нет — создайте первый за пару секунд"
                    : "Выберите профиль, чтобы продолжить")
                : "Профили сейчас недоступны";

            FooterHint.Text = _storageAvailable
                ? "Профили и фотографии хранятся только на этом компьютере. Пароли не нужны."
                : string.Empty;
        }

        private FrameworkElement BuildEmptyState()
        {
            var text = new StackPanel();

            text.Children.Add(new TextBlock
            {
                Text = "Здесь появятся профили",
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF6, 0xEF))
            });

            text.Children.Add(new TextBlock
            {
                Text = "Создайте первый профиль — имя, описание и фотография сохранятся локально.",
                Margin = new Thickness(0, 6, 0, 0),
                FontSize = 13,
                LineHeight = 19,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x91, 0x86))
            });

            return new Border
            {
                Background = new SolidColorBrush(Color.FromRgb(0x11, 0x1C, 0x16)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(0x22, 0x33, 0x2B)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(18, 16, 18, 16),
                Child = text
            };
        }

        private FrameworkElement BuildProfileCard(LocalProfile profile)
        {
            var description = string.IsNullOrWhiteSpace(profile.Description)
                ? "Локальный профиль"
                : profile.Description.Trim();

            var nameAndDescription = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };

            nameAndDescription.Children.Add(new TextBlock
            {
                Text = profile.Name,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF6, 0xEF)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            nameAndDescription.Children.Add(new TextBlock
            {
                Text = description,
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x91, 0x86)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var avatarBrush = ProfileAvatar.TryCreateBrush(profile.AvatarFile, 96);
            var avatar = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(21),
                Background = avatarBrush != null
                    ? (Brush)avatarBrush
                    : ProfileColors.CreateBrush(profile.Color)
            };

            if (avatarBrush == null)
            {
                avatar.Child = new TextBlock
                {
                    Text = profile.Initial,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0x25, 0x1A)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
            }

            content.Children.Add(avatar);

            Grid.SetColumn(nameAndDescription, 1);
            content.Children.Add(nameAndDescription);

            var arrow = new TextBlock
            {
                Text = "›",
                FontSize = 22,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0x61, 0x57))
            };
            Grid.SetColumn(arrow, 2);
            content.Children.Add(arrow);

            var card = new Button
            {
                Style = (Style)FindResource("TfProfileCard"),
                Content = content,
                Tag = profile,
                Margin = new Thickness(0, 0, 6, 0),
                ToolTip = "Войти в профиль"
            };
            AutomationProperties.SetName(
                card,
                string.IsNullOrWhiteSpace(profile.Description)
                    ? profile.Name
                    : profile.Name + ", " + profile.Description);
            card.Click += OnProfileSelected;

            var deleteButton = new Button
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x3E, 0x56, 0x4A)),
                FontSize = 13,
                Cursor = Cursors.Hand,
                Content = "✕",
                Tag = profile,
                ToolTip = "Удалить профиль",
                VerticalAlignment = VerticalAlignment.Center
            };
            AutomationProperties.SetName(deleteButton, "Удалить профиль " + profile.Name);
            deleteButton.Template = BuildFlatButtonTemplate(Color.FromRgb(0x3A, 0x1F, 0x26));
            deleteButton.Click += OnDeleteProfile;

            var editButton = new Button
            {
                Width = 32,
                Height = 32,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0x4E, 0x61, 0x57)),
                FontSize = 14,
                Cursor = Cursors.Hand,
                Content = "✎",
                Tag = profile,
                ToolTip = "Изменить профиль",
                VerticalAlignment = VerticalAlignment.Center
            };
            AutomationProperties.SetName(editButton, "Изменить профиль " + profile.Name);
            editButton.Template = BuildFlatButtonTemplate(Color.FromRgb(0x1B, 0x2E, 0x24));
            editButton.Click += OnEditProfile;

            var row = new Grid();
            row.Margin = new Thickness(0, 0, 0, 10);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(card);
            Grid.SetColumn(editButton, 1);
            row.Children.Add(editButton);
            Grid.SetColumn(deleteButton, 2);
            row.Children.Add(deleteButton);

            return row;
        }

        /// <summary>
        /// Плоская кнопка без рамки — для действий с профилем.
        /// </summary>
        private static ControlTemplate BuildFlatButtonTemplate(Color hoverBackground)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border), "FlatBorder");
            border.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(10));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var hover = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(hoverBackground), "FlatBorder"));
            template.Triggers.Add(hover);

            return template;
        }

        private static StackPanel BuildCreateButtonContent(string label)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal };
            panel.Children.Add(new TextBlock { Text = "+", Margin = new Thickness(0, -2, 10, 0), FontSize = 20 });
            panel.Children.Add(new TextBlock { Text = label, VerticalAlignment = VerticalAlignment.Center });
            return panel;
        }

        private void OnProfileSelected(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button != null ? button.Tag as LocalProfile : null;
            if (profile == null)
            {
                return;
            }

            SelectedProfile = profile;

            try
            {
                ProfileStore.SetActiveId(profile.Id);
            }
            catch
            {
                // Активный профиль — вспомогательное состояние; выбранный профиль
                // уже передан в MainWindow и приложение может продолжить работу.
            }

            DialogResult = true;
        }

        private void OnDeleteProfile(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button != null ? button.Tag as LocalProfile : null;
            if (profile == null)
            {
                return;
            }

            var answer = MessageBox.Show(
                this,
                string.Format("Удалить локальный профиль «{0}»? Это действие нельзя отменить.", profile.Name),
                "Удаление профиля",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer != MessageBoxResult.Yes)
            {
                return;
            }

            try
            {
                ProfileStore.Remove(profile.Id);
            }
            catch (Exception ex)
            {
                ShowError("Не удалось удалить профиль: " + ex.Message);
                return;
            }

            Refresh();
        }

        private void OnEditProfile(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var profile = button != null ? button.Tag as LocalProfile : null;
            if (profile == null)
            {
                return;
            }

            Register register;
            bool? result;

            try
            {
                register = new Register(profile) { Owner = this };
                result = register.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось открыть окно редактирования профиля: " + ex.Message);
                return;
            }

            if (result == true && register.CreatedProfile != null)
            {
                if (TrySaveProfile(register.CreatedProfile, profile))
                {
                    EditedProfile = register.CreatedProfile;
                    Refresh();
                }
            }
        }

        private void OnCreateProfileClick(object sender, RoutedEventArgs e)
        {
            Register register;
            bool? result;

            try
            {
                register = new Register { Owner = this };
                result = register.ShowDialog();
            }
            catch (Exception ex)
            {
                ShowError("Не удалось открыть окно создания профиля: " + ex.Message);
                return;
            }

            if (result == true && register.CreatedProfile != null)
            {
                var profile = register.CreatedProfile;
                if (!TrySaveProfile(profile, null))
                {
                    return;
                }

                try
                {
                    ProfileStore.SetActiveId(profile.Id);
                }
                catch
                {
                    // Профиль уже сохранён; не теряем результат из-за вспомогательного файла.
                }

                SelectedProfile = profile;
                DialogResult = true;
                return;
            }

            Refresh();
        }

        /// <summary>
        /// Сохраняет профиль и убирает только что созданный файл аватара,
        /// если запись профиля не удалась. Старый аватар при ошибке не трогаем.
        /// </summary>
        private bool TrySaveProfile(LocalProfile profile, LocalProfile original)
        {
            try
            {
                ProfileStore.Add(profile);
                return true;
            }
            catch (Exception ex)
            {
                var avatarChanged = original == null ||
                    !string.Equals(profile.AvatarFile, original.AvatarFile, StringComparison.OrdinalIgnoreCase);
                if (avatarChanged)
                {
                    ProfileAvatar.TryDelete(profile.AvatarFile);
                }

                ShowError("Не удалось сохранить профиль: " + ex.Message);
                return false;
            }
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void OnWindowKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
            }
        }

        private void ShowError(string message)
        {
            ErrorText.Text = message;
            ErrorText.Visibility = Visibility.Visible;
        }

        private void HideError()
        {
            ErrorText.Text = string.Empty;
            ErrorText.Visibility = Visibility.Collapsed;
        }
    }
}
