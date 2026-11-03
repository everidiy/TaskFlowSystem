using System;
using System.Linq;
using System.Windows;
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
        private const int PreviewNameLength = 26;

        public Login()
        {
            InitializeComponent();
            PlayEntrance();
        }

        /// <summary>
        /// Профиль, с которым нужно продолжить работу, или null, если окно закрыли.
        /// </summary>
        public LocalProfile SelectedProfile { get; private set; }

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

            var profiles = ProfileStore.Load()
                .OrderByDescending(p => p.CreatedUtc)
                .ToList();

            foreach (var profile in profiles)
            {
                var row = BuildProfileCard(profile);
                row.Margin = new Thickness(0, 0, 0, 10);
                ProfileList.Children.Add(row);
            }

            var isEmpty = profiles.Count == 0;
            CreateProfileButton.Content = BuildCreateButtonContent(isEmpty ? "Создать первый профиль" : "Новый профиль");
            HeaderSubtitle.Text = isEmpty
                ? "Профилей пока нет — создайте первый за пару секунд"
                : "Выберите профиль, чтобы продолжить";
            FooterHint.Text = isEmpty
                ? "Профили хранятся локально на этом компьютере. Пароли не нужны."
                : "Профили хранятся локально на этом компьютере. Пароли не нужны.";
        }

        private FrameworkElement BuildProfileCard(LocalProfile profile)
        {
            var roleText = string.IsNullOrWhiteSpace(profile.Role) ? "Гость" : profile.Role;

            var nameAndRole = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(14, 0, 0, 0)
            };

            nameAndRole.Children.Add(new TextBlock
            {
                Text = profile.Name,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(0xEA, 0xF6, 0xEF)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            nameAndRole.Children.Add(new TextBlock
            {
                Text = roleText,
                Margin = new Thickness(0, 3, 0, 0),
                FontSize = 12,
                Foreground = new SolidColorBrush(Color.FromRgb(0x7E, 0x91, 0x86)),
                TextTrimming = TextTrimming.CharacterEllipsis
            });

            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var avatar = new Border
            {
                Width = 42,
                Height = 42,
                CornerRadius = new CornerRadius(21),
                Background = ProfileColors.CreateBrush(profile.Color),
                Child = new TextBlock
                {
                    Text = profile.Initial,
                    FontSize = 16,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x06, 0x25, 0x1A)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                }
            };
            content.Children.Add(avatar);

            Grid.SetColumn(nameAndRole, 1);
            content.Children.Add(nameAndRole);

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
            System.Windows.Automation.AutomationProperties.SetName(card, profile.Name);
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
            System.Windows.Automation.AutomationProperties.SetName(deleteButton, "Удалить профиль " + profile.Name);
            deleteButton.Template = BuildFlatButtonTemplate();
            deleteButton.Click += OnDeleteProfile;

            var row = new Grid();
            row.Margin = new Thickness(0, 0, 0, 10);
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.Children.Add(card);
            Grid.SetColumn(deleteButton, 1);
            row.Children.Add(deleteButton);

            return row;
        }

        /// <summary>
        /// Плоская кнопка без рамки — для удаления профиля.
        /// </summary>
        private static ControlTemplate BuildFlatButtonTemplate()
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
            hover.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0x3A, 0x1F, 0x26)), "FlatBorder"));
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
            ProfileStore.SetActiveId(profile.Id);
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

            ProfileStore.Remove(profile.Id);
            Refresh();
        }

        private void OnCreateProfileClick(object sender, RoutedEventArgs e)
        {
            var register = new Register { Owner = this };
            var result = register.ShowDialog();

            if (result == true && register.CreatedProfile != null)
            {
                ProfileStore.Add(register.CreatedProfile);
                ProfileStore.SetActiveId(register.CreatedProfile.Id);
                SelectedProfile = register.CreatedProfile;
                DialogResult = true;
                return;
            }

            Refresh();
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
