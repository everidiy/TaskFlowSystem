using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TaskFlowSystem.UserControls
{
    /// <summary>
    /// Логика взаимодействия для OverviewView.xaml
    /// </summary>
    public partial class OverviewView : UserControl
    {
        public OverviewView()
        {
            InitializeComponent();
        }

        private Button _activeViewButton;

        private void CreateTask(string columnName)
        {
            MessageBox.Show($"Создать задачу в колонке «{columnName}».", "Новая задача",
                MessageBoxButton.OK, MessageBoxImage.Information);
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

        private void SetActiveView(Button button)
        {
            ResetViewButtons();

            button.Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x3A, 0x2D));
            button.Foreground = new SolidColorBrush(Color.FromRgb(0x6E, 0xE7, 0xB7));

            _activeViewButton = button;
        }

        private void Reset(Button button)
        {
            button.ClearValue(Button.BackgroundProperty);
            button.ClearValue(Button.ForegroundProperty);
        }

        private void ResetViewButtons()
        {
            Reset(ViewBoardButton);
            Reset(ViewCalendarButton);
            Reset(ViewListButton);
            Reset(ViewTimelineButton);
        }
    }
}
