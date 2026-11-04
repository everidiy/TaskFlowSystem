using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Shell;

namespace TaskFlowSystem
{
    public partial class MainWindow : Window
    {
        private const string SearchPlaceholder = "Поиск задач, проектов и участников...";
        private Button _activeNavButton;
        private Button _activeViewButton;

        public MainWindow()
            : this(null)
        {
        }

        public MainWindow(LocalProfile profile)
        {
            InitializeComponent();
            CurrentProfile = profile;
        }

        /// <summary>
        /// Профиль, с которым открыто приложение.
        /// </summary>
        public LocalProfile CurrentProfile { get; private set; }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            _activeNavButton = NavOverviewButton;
            _activeViewButton = ViewBoardButton;
            SetSearchPlaceholder();
            ApplyProfile(CurrentProfile);
            UpdateMaximizeButtonState();
        }

        private void ApplyProfile(LocalProfile profile)
        {
            if (profile == null)
            {
                return;
            }

            ProfileNameText.Text = profile.Name;
            ProfileDescriptionText.Text = string.IsNullOrWhiteSpace(profile.Description)
                ? "Локальный профиль"
                : profile.Description.Trim();

            var avatarBrush = ProfileAvatar.TryCreateBrush(profile.AvatarFile, 96);
            ProfileAvatarBorder.Background = avatarBrush != null
                ? (Brush)avatarBrush
                : ProfileColors.CreateBrush(profile.Color);
            ProfileAvatarText.Text = profile.Initial;
            ProfileAvatarText.Visibility = avatarBrush == null ? Visibility.Visible : Visibility.Collapsed;

            System.Windows.Automation.AutomationProperties.SetName(ProfileButton, "Профиль " + profile.Name);
            Title = "TaskFlow — " + profile.Name;
        }

        private void OnCloseClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnMinimizeClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnMaximizeClick(object sender, RoutedEventArgs e)
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void OnWindowStateChanged(object sender, EventArgs e)
        {
            UpdateMaximizeButtonState();
        }

        /// <summary>
        /// Кнопка развернуть/восстановить должна отражать текущее состояние окна.
        /// </summary>
        private void UpdateMaximizeButtonState()
        {
            if (BtnMaximize == null)
            {
                // StateChanged может сработать во время разбора XAML, до создания кнопки.
                return;
            }

            var isMaximized = WindowState == WindowState.Maximized;
            BtnMaximize.Content = isMaximized ? "❐" : "□";
            BtnMaximize.ToolTip = isMaximized ? "Восстановить" : "Развернуть";
            System.Windows.Automation.AutomationProperties.SetName(
                BtnMaximize,
                isMaximized ? "Восстановить окно" : "Развернуть окно");
        }

        private void OnLogoClick(object sender, RoutedEventArgs e)
        {
            OnOverviewClick(NavOverviewButton, e);
        }

        private void OnOverviewClick(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavOverviewButton);
            PageTitleText.Text = "Обзор";
            PageSubtitleText.Text = "Добавьте первую задачу, чтобы начать работу";
        }

        private void OnMyTasksClick(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavTasksButton);
            PageTitleText.Text = "Мои задачи";
            PageSubtitleText.Text = "Здесь будут отображаться назначенные вам задачи";
        }

        private void OnInboxClick(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavInboxButton);
            PageTitleText.Text = "Входящие";
            PageSubtitleText.Text = "Новых уведомлений нет";
        }

        private void OnCalendarClick(object sender, RoutedEventArgs e)
        {
            SetActiveNav(NavCalendarButton);
            PageTitleText.Text = "Календарь";
            PageSubtitleText.Text = "Сроки появятся после создания задач";
        }

        private void OnProjectsClick(object sender, RoutedEventArgs e)
        {
            PageTitleText.Text = "Проекты";
            PageSubtitleText.Text = "Создайте первый проект для команды";
        }

        private void OnAddProjectClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            MessageBox.Show("Открыть диалог создания проекта.", "Новый проект",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnTeamClick(object sender, RoutedEventArgs e)
        {
            PageTitleText.Text = "Участники";
            PageSubtitleText.Text = "Команда пока не добавлена";
        }

        private void OnReportsClick(object sender, RoutedEventArgs e)
        {
            PageTitleText.Text = "Отчёты";
            PageSubtitleText.Text = "Статистика появится после появления задач";
        }

        private void OnSettingsClick(object sender, RoutedEventArgs e)
        {
            PageTitleText.Text = "Настройки";
            PageSubtitleText.Text = "Параметры рабочего пространства";
        }

        private void OnUpgradeClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Переход на тариф Pro.", "TaskFlow Pro",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnCreateTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTask("К выполнению");
        }

        private void OnAddTodoTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTask("К выполнению");
        }

        private void OnAddInProgressTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTask("В работе");
        }

        private void OnAddReviewTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTask("На проверке");
        }

        private void OnAddDoneTaskClick(object sender, RoutedEventArgs e)
        {
            CreateTask("Готово");
        }

        private void OnBoardViewClick(object sender, RoutedEventArgs e)
        {
            SetActiveView(ViewBoardButton);
        }

        private void OnListViewClick(object sender, RoutedEventArgs e)
        {
            SetActiveView(ViewListButton);
        }

        private void OnCalendarViewClick(object sender, RoutedEventArgs e)
        {
            SetActiveView(ViewCalendarButton);
        }

        private void OnTimelineViewClick(object sender, RoutedEventArgs e)
        {
            SetActiveView(ViewTimelineButton);
        }

        private void OnFilterClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открыть панель фильтров.", "Фильтр",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnSortClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Открыть параметры сортировки.", "Сортировка",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnNotificationsClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Уведомлений пока нет.", "Уведомления",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnHelpClick(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Справка TaskFlowSystem.", "Помощь",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OnProfileClick(object sender, RoutedEventArgs e)
        {
            var login = new Login { Owner = this };
            var result = login.ShowDialog();

            if (login.EditedProfile != null &&
                CurrentProfile != null &&
                string.Equals(login.EditedProfile.Id, CurrentProfile.Id, StringComparison.OrdinalIgnoreCase))
            {
                CurrentProfile = login.EditedProfile;
                ApplyProfile(CurrentProfile);
                PageSubtitleText.Text = string.Format("Профиль «{0}» активен", CurrentProfile.Name);
            }

            if (result != true || login.SelectedProfile == null)
            {
                return;
            }

            CurrentProfile = login.SelectedProfile;
            ApplyProfile(CurrentProfile);
            PageTitleText.Text = "Обзор";
            PageSubtitleText.Text = string.Format("Профиль «{0}» активен", CurrentProfile.Name);
        }

        private void OnSearchGotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == SearchPlaceholder)
            {
                SearchBox.Text = string.Empty;
                SearchBox.Foreground = new SolidColorBrush(Color.FromRgb(0x25, 0x29, 0x3B));
            }
        }

        private void OnSearchLostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SetSearchPlaceholder();
            }
        }

        private void OnSearchKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RunSearch();
            }
        }

        private void OnSearchShortcutClick(object sender, RoutedEventArgs e)
        {
            SearchBox.Focus();
        }

        private void CreateTask(string columnName)
        {
            MessageBox.Show($"Создать задачу в колонке «{columnName}».", "Новая задача",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void RunSearch()
        {
            var query = SearchBox.Text;
            if (string.IsNullOrWhiteSpace(query) || query == SearchPlaceholder)
            {
                return;
            }

            MessageBox.Show($"Поиск: {query}", "Поиск",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void SetSearchPlaceholder()
        {
            SearchBox.Text = SearchPlaceholder;
            SearchBox.Foreground = new SolidColorBrush(Color.FromRgb(0x8A, 0x8F, 0xA3));
        }

        private void SetActiveNav(Button button)
        {
            ResetNav(NavOverviewButton);
            ResetNav(NavTasksButton);
            ResetNav(NavInboxButton);
            ResetNav(NavCalendarButton);

            button.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xEE, 0xFF));
            button.Foreground = new SolidColorBrush(Color.FromRgb(0x62, 0x5B, 0xF6));
            _activeNavButton = button;
        }

        private void ResetNav(Button button)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = new SolidColorBrush(Color.FromRgb(0x6D, 0x72, 0x87));
        }

        private void SetActiveView(Button button)
        {
            ResetView(ViewBoardButton);
            ResetView(ViewListButton);
            ResetView(ViewCalendarButton);
            ResetView(ViewTimelineButton);

            button.Background = new SolidColorBrush(Color.FromRgb(0xEF, 0xEE, 0xFF));
            button.Foreground = new SolidColorBrush(Color.FromRgb(0x62, 0x5B, 0xF6));
            _activeViewButton = button;
        }

        private void ResetView(Button button)
        {
            button.Background = Brushes.Transparent;
            button.Foreground = new SolidColorBrush(Color.FromRgb(0x6D, 0x72, 0x87));
        }
    }
}