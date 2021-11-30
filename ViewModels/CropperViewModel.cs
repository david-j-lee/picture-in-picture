using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using PictureInPicture.DataModel;
using PictureInPicture.Interfaces;
using PictureInPicture.Native;
using PictureInPicture.Shared;
using PictureInPicture.Views;

namespace PictureInPicture.ViewModels
{
  public class CropperViewModel : ObservableRecipient, ICloseable
  {
    private readonly ILogger<CropperViewModel> _logger;

    #region public

    public event EventHandler<EventArgs> RequestClose;

    public ICommand ClosingCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand StartCommand { get; }


    /// <summary>
    /// Gets selected region
    /// </summary>
    public NativeStructs.Rect SelectedRegion =>
        new NativeStructs.Rect(Left, Top, Width + Left, Top + Height);

    public const int DefaultPosition = 0;
    public bool RegionHasBeenModified =>
        Top != DefaultPosition
        || Left != DefaultPosition
        || Height != Constants.MinCropperHeight
        || Width != Constants.MinCropperWidth;

    /// <summary>
    /// Gets or sets window title
    /// </summary>
    public string Title
    {
      get => _title;
      set
      {
        _title = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets top property of the window
    /// </summary>
    public int WindowTop
    {
      get => _windowTop;
      set
      {
        _windowTop = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets left property of the window
    /// </summary>
    public int WindowLeft
    {
      get => _windowLeft;
      set
      {
        _windowLeft = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets width property of the window
    /// </summary>
    public int WindowWidth
    {
      get => _windowWidth;
      set
      {
        _windowWidth = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets height property of the window
    /// </summary>
    public int WindowHeight
    {
      get => _windowHeight;
      set
      {
        _windowHeight = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets canvas margin (border of the window to crop)
    /// </summary>
    public Thickness CanvasMargin
    {
      get => _canvasMargin;
      set
      {
        _canvasMargin = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets max height property of the region to crop
    /// </summary>
    public int MaxHeight
    {
      get => _maxHeight;
      set
      {
        _maxHeight = value;
        UpdateBottom();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets max width property of the region to crop
    /// </summary>
    public int MaxWidth
    {
      get => _maxWidth;
      set
      {
        _maxWidth = value;
        UpdateRight();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets min height property of the region to crop
    /// </summary>
    public int MinHeight
    {
      get => _minHeight;
      set
      {
        _minHeight = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets min width property of the region to crop
    /// </summary>
    public int MinWidth
    {
      get => _minWidth;
      set
      {
        _minWidth = value;
        OnPropertyChanged();
      }
    }

    /// <summary>
    /// Gets or sets height property of the region to crop
    /// </summary>
    public int Height
    {
      get => _height;
      set
      {
        _height = value;
        UpdateBottom();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets width property of the region to crop
    /// </summary>
    public int Width
    {
      get => _width;
      set
      {
        _width = value;
        UpdateRight();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets top property of the region to crop
    /// </summary>
    public int Top
    {
      get => _top;
      set
      {
        _top = value;
        UpdateBottom();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets left property of the region to crop
    /// </summary>
    public int Left
    {
      get => _left;
      set
      {
        _left = value;
        UpdateRight();
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets bottom property of the region to crop
    /// Updated by <see cref="UpdateBottom"/>
    /// </summary>
    public int Bottom
    {
      get => _bottom;
      set
      {
        _bottom = value;
        OnPropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets right property of the region to crop
    /// Updated by <see cref="UpdateRight"/>
    /// </summary>
    public int Right
    {
      get => _right;
      set
      {
        _right = value;
        OnPropertyChanged();
      }
    }

    #endregion

    #region private

    private string _title;

    private int _windowTop;
    private int _windowLeft;
    private int _windowWidth;
    private int _windowHeight;
    private Thickness _canvasMargin;

    private int _maxHeight;
    private int _maxWidth;
    private int _minHeight;
    private int _minWidth;

    private int _height;
    private int _width;
    private int _top;
    private int _left;
    private int _bottom;
    private int _right;

    private NativeStructs.Rect _sizeRestriction;
    private WindowInfo _windowInfo;
    private SelectedWindow _selectedWindow;

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public CropperViewModel(ILogger<CropperViewModel> logger)
    {
      _logger = logger;
      _logger?.LogInformation("   ====== CropperWindow ======   ");

      ClosingCommand = new RelayCommand(ClosingCommandExecute);
      CloseCommand = new RelayCommand(CloseCommandExecute);
      StartCommand = new RelayCommand(StartCommandExecute);

      Messenger.Register<WindowInfo>(this, (_, m) => Init(m));
      Messenger.Register<SelectedWindow>(
        this, (_, m) => HandleSelectedWindowChange(m));
      Messenger.Register<Action<NativeStructs.Rect>>(
        this, (_, m) => StartPip(m));
    }

    /// <summary>
    /// Called from <see cref="MainViewModel"/> to init window to crop
    /// </summary>
    /// <param name="windowInfo">data of the window to crop</param>
    private void Init(WindowInfo windowInfo)
    {
      Messenger.Unregister<WindowInfo>(this);
      _windowInfo = windowInfo;
      if (windowInfo == null)
      {
        _logger?.LogError("Can't Init cropper");
        ClosingCommandExecute();
        RequestClose?.Invoke(this, EventArgs.Empty);
        return;
      }

      _logger?.LogInformation("Init cropper : " + _windowInfo.Title);

      Title = _windowInfo.Title + " - Cropper - PictureInPicture";

      _windowInfo.SetAsForegroundWindow();
      _sizeRestriction = _windowInfo.Rect - _windowInfo.Border;

      WindowTop = _windowInfo.Position.Y;
      WindowLeft = _windowInfo.Position.X;
      WindowWidth = _windowInfo.Rect.Width;
      WindowHeight = _windowInfo.Rect.Height;
      CanvasMargin = new Thickness(
          _windowInfo.Border.Left,
          _windowInfo.Border.Top,
          _windowInfo.Border.Right,
          _windowInfo.Border.Bottom
      );

      MaxHeight = _sizeRestriction.Height;
      MaxWidth = _sizeRestriction.Width;
      MinHeight = Constants.MinCropperHeight;
      MinWidth = Constants.MinCropperWidth;

      Top = 0;
      Left = 0;
      Height = MinHeight;
      Width = MinWidth;

      ThisWindow().Focus();
      SetAsForegroundWindow();

      // TODO: commented out code
      // _mlSource = new CancellationTokenSource();
      // _mlToken = _mlSource.Token;
      // Task.Run(() =>
      // {
      //     MachineLearningService.Instance.PredictAsync(
      //         _windowInfo.Program,
      //         _windowInfo.Title,
      //         _windowInfo.RectNoBorder.Y,
      //         _windowInfo.RectNoBorder.X,
      //         _windowInfo.RectNoBorder.Height,
      //         _windowInfo.RectNoBorder.Width).ContinueWith(SetRegion, _mlToken);
      // }, _mlToken);
    }

    // TODO: commented out code
    /// <summary>
    /// Set region to crop
    /// </summary>
    /// <param name="obj">Prediction result</param>
    // private void SetRegion(Task<RegionPrediction> obj)
    // {
    //     if (RegionHasBeenModified)
    //         return;

    //     Top = (int)(obj.Result.Top / _windowInfo.DpiY);
    //     Left = (int)(obj.Result.Left / _windowInfo.DpiX);
    //     Height = (int)(obj.Result.Height / _windowInfo.DpiY);
    //     Width = (int)(obj.Result.Width / _windowInfo.DpiX);

    //     if (Width + Left > WindowWidth)
    //         Width = WindowWidth - Left;

    //     if (Height + Top > WindowHeight)
    //         Height = WindowHeight - Top;

    //     if (Height < Constants.MinCropperSize)
    //         Height = Constants.MinCropperSize;

    //     if (Width < Constants.MinCropperSize)
    //         Width = Constants.MinCropperSize;
    // }

    private void HandleSelectedWindowChange(SelectedWindow selectedWindow)
    {
      _selectedWindow = selectedWindow;

      if (selectedWindow != null)
      {
        Messenger.Unregister<SelectedWindow>(this);

        Top = _selectedWindow.SelectedRegion.Top;
        Left = _selectedWindow.SelectedRegion.Left;
        Height = _selectedWindow.SelectedRegion.Height;
        Width = _selectedWindow.SelectedRegion.Width;
      }
    }

    /// <summary>
    /// Gets this window
    /// </summary>
    /// <returns>This window</returns>
    private Window ThisWindow()
    {
      var windowsList = Application.Current.Windows.Cast<Window>();
      return windowsList.FirstOrDefault(
          window => window.DataContext == this
      );
    }

    /// <summary>
    /// Set this window as foreground window
    /// </summary>
    public void SetAsForegroundWindow()
    {
      var thisWindow = ThisWindow();
      if (thisWindow == null)
      {
        return;
      }
      thisWindow.Show();
      thisWindow.Activate();
      thisWindow.Topmost = true;
      thisWindow.Topmost = false;
      thisWindow.Focus();
      var handle = new WindowInteropHelper(thisWindow).Handle;
      NativeMethods.SetForegroundWindow(handle);
    }

    /// <summary>
    /// Called by <see cref="Height"/> or <see cref="Top"/> to update <see cref="Bottom"/>
    /// </summary>
    private void UpdateBottom()
    {
      Bottom = MaxHeight - (Top + Height);
    }

    /// <summary>
    /// Called by <see cref="Width"/> or <see cref="Left"/> to update <see cref="Right"/>
    /// </summary>
    private void UpdateRight()
    {
      Right = MaxWidth - (Left + Width);
    }

    /// <summary>
    /// Called from <see cref="MainViewModel"/> to get cropped region and start Pip mode
    /// </summary>
    /// <param name="cb">Callback with cropped region</param>
    private void StartPip(Action<NativeStructs.Rect> cb)
    {
      Messenger.Unregister<Action<NativeStructs.Rect>>(this);
      cb(SelectedRegion);
    }

    #region commands

    /// <summary>
    /// Executed when the window is closing. Unregister messengers
    /// </summary>
    private void ClosingCommandExecute()
    {
      _logger?.LogInformation("   |||||| Close CropperWindow ||||||   ");
      Messenger.Unregister<WindowInfo>(this);
      Messenger.Unregister<Action<NativeStructs.Rect>>(this);
    }

    /// <summary>
    /// Executed on click on check button. Start up Pip
    /// </summary>
    private void StartCommandExecute()
    {
      var pip = new PipModeWindow();
      pip.Show();
      Messenger.Send(pip);

      var selectedWindow = new SelectedWindow(_windowInfo, SelectedRegion);
      selectedWindow.PictureInPictureEnabled = true;
      if (_selectedWindow != null)
      {
        selectedWindow.PipPosition = _selectedWindow.PipPosition;
      }
      Messenger.Send(selectedWindow);

      RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Executed on click on close button. Close this window
    /// </summary>
    private void CloseCommandExecute()
    {
      if (_selectedWindow == null)
      {
        Messenger.Send<SelectedWindow>(null);
      }
      Messenger.Unregister<SelectedWindow>(this);
      RequestClose?.Invoke(this, EventArgs.Empty);

      // TODO: This is a poor way to focus back to the MainViewModel.
      // It assumes there is only one window left after closing this one, which
      // should always be true, however, if we were to introduce multiple
      // picture in picture windows this has the potential to break.
      var windowsList = Application.Current.Windows.Cast<Window>();
      if (windowsList.Any())
      {
        windowsList.First().Focus();
      }
    }
    #endregion

  }
}
