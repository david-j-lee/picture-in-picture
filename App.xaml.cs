using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using PictureInPicture.Services;
using PictureInPicture.ViewModels;
using PictureInPicture.Views;

namespace PictureInPicture
{
  /// <summary>
  /// Interaction logic for App.xaml
  /// </summary>
  public partial class App : Application
  {
    public IServiceProvider Services { get; }

    public App()
    {
      Services = ConfigureServices();

      InitializeComponent();
    }

    /// <summary>
    /// Gets the current <see cref="App"/> instance in use
    /// </summary>
    public new static App Current => (App)Application.Current;

    private void OnStartup(object sender, StartupEventArgs e)
    {
      var mainWindow = Services.GetService<MainWindow>();
      mainWindow.Show();
    }

    private static IServiceProvider ConfigureServices()
    {
      var services = new ServiceCollection();

      services.AddLogging(configure => configure.AddConsole());

      // Services
      services.AddSingleton<ProcessesService>();

      // View Models
      services.AddTransient<CropperViewModel>();
      services.AddTransient<MainViewModel>();
      services.AddTransient<PipModeViewModel>();

      // Views
      services.AddSingleton<CropperWindow>();
      services.AddSingleton<MainWindow>();
      services.AddSingleton<PipModeWindow>();

      return services.BuildServiceProvider();
    }
  }
}
