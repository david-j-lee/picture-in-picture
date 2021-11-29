using System.Windows.Input;
using PictureInPicture.Interfaces;

namespace PictureInPicture.Views
{
  /// <inheritdoc cref="PipModeWindow" />
  /// <summary>
  /// Logique d'interaction pour PipModeWindow.xaml
  /// </summary>
  public partial class PipModeWindow
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public PipModeWindow()
    {
      InitializeComponent();

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
