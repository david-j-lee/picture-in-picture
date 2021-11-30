using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using PictureInPicture.Interfaces;
using PictureInPicture.ViewModels;

namespace PictureInPicture.Views
{
  /// <summary>
  /// View class for MainWindow.xaml
  /// </summary>
  public partial class MainWindow
  {
    private bool _dragging;
    private Point _anchorPoint;

    /// <summary>
    /// Constructor
    /// </summary>
    public MainWindow()
    {
      this.InitializeComponent();
      DataContext = App.Current.Services.GetService<MainViewModel>();

      Loaded += (s, e) =>
      {
        if (DataContext is ICloseable closeable)
        {
          closeable.RequestClose += (_, __) => Close();
        }
      };
    }

    /// <summary>
    /// Move window if dragging
    /// </summary>
    /// <param name="e">Event arguments</param>
    protected override void OnMouseMove(MouseEventArgs e)
    {
      if (!_dragging)
      {
        return;
      }
      Left = Left + e.GetPosition(this).X - _anchorPoint.X;
      Top = Top + e.GetPosition(this).Y - _anchorPoint.Y;
    }

    /// <summary>
    /// Start dragging
    /// </summary>
    /// <param name="e">Event arguments</param>
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
      _anchorPoint = e.GetPosition(this);
      _dragging = true;
      CaptureMouse();
      e.Handled = true;
    }

    /// <summary>
    /// Stop dragging
    /// </summary>
    /// <param name="e">Event arguments</param>
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
      if (!_dragging)
        return;
      ReleaseMouseCapture();
      _dragging = false;
      e.Handled = true;
    }
  }
}
