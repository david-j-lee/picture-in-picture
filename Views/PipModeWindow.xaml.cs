using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PictureInPicture.Interfaces;
using PictureInPicture.ViewModels;

namespace PictureInPicture.Views
{
  /// <inheritdoc cref="PipModeWindow" />
  /// <summary>
  /// View class for PipModeWindow.xaml
  /// </summary>
  public partial class PipModeWindow
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public PipModeWindow()
    {
      InitializeComponent();

      var viewModel = App.Current.Services.GetService<PipModeViewModel>();
      DataContext = viewModel;

      Loaded += new RoutedEventHandler((s, e) => viewModel.LoadedCommandExecute());

      Loaded += (s, e) =>
      {
        if (DataContext is ICloseable closeable)
        {
          closeable.RequestClose += (_, __) => Close();
        }
      };
    }

    /// <summary>
    /// Drag this window
    /// </summary>
    /// <param name="e">Event arguments</param>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
      base.OnMouseLeftButtonDown(e);

      DragMove();
    }
  }
}
