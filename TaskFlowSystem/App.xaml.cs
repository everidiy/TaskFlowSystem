using System.Windows;

namespace TaskFlowSystem
{
    /// <summary>
    /// Запускает приложение с выбора локального профиля.
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Пока открыт диалог выбора профиля, главное окно ещё не создано.
            ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var login = new Login();
            MainWindow = login;

            var result = login.ShowDialog();
            if (result == true && login.SelectedProfile != null)
            {
                var main = new MainWindow(login.SelectedProfile);
                MainWindow = main;
                ShutdownMode = ShutdownMode.OnMainWindowClose;
                main.Show();
            }
            else
            {
                Shutdown();
            }
        }
    }
}
