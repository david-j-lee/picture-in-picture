using Microsoft.Extensions.DependencyInjection;
using PictureInPicture.Interfaces;
using PictureInPicture.ViewModels;

namespace PictureInPicture.Views
{
  /// <summary>
  /// View class for CropperWindow.xaml
  /// </summary>
  public partial class CropperWindow
  {
    /// <summary>
    /// Constructor
    /// </summary>
    public CropperWindow()
    {
      InitializeComponent();
      DataContext = App.Current.Services.GetService<CropperViewModel>();

      Loaded += (s, e) =>
      {
        if (DataContext is ICloseable closeable)
        {
          closeable.RequestClose += (_, __) => Close();
        }
      };
    }
  }
}
