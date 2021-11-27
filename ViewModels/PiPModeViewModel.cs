using System;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.CommandWpf;
using PictureInPicture.DataModel;
using PictureInPicture.Interfaces;
using PictureInPicture.Native;
using PictureInPicture.Shared;
using PictureInPicture.Shared.Helpers;
using PictureInPicture.Views;
using Application = System.Windows.Application;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using Point = System.Drawing.Point;

namespace PictureInPicture.ViewModels
{
  public class PiPModeViewModel : ViewModelBase, ICloseable, IDisposable
  {
    #region public

    /// <summary>
    /// Gets or sets window title
    /// </summary>
    public string Title
    {
      get => _title;
      set
      {
        _title = value;
        RaisePropertyChanged();
      }
    }

    public const int MinSize = 100;
    public const float DefaultSizePercentage = 0.25f;
    public const float DefaultPositionPercentage = 0.1f;

    public event EventHandler<EventArgs> RequestClose;

    public ICommand LoadedCommand { get; }
    public ICommand CloseCommand { get; }
    public ICommand ClosingCommand { get; }
    public ICommand ChangeSelectedWindowCommand { get; }
    public ICommand MouseEnterCommand { get; }
    public ICommand MouseLeaveCommand { get; }
    public ICommand DpiChangedCommand { get; }

    /// <summary>
    /// Gets or sets min height property of the window
    /// </summary>
    public int MinHeight
    {
      get => _minHeight;
      set
      {
        _minHeight = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets min width property of the window
    /// </summary>
    public int MinWidth
    {
      get => _minWidth;
      set
      {
        _minWidth = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets top property of the window
    /// </summary>
    public int Top
    {
      get => _top;
      set
      {
        _top = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets left property of the window
    /// </summary>
    public int Left
    {
      get => _left;
      set
      {
        _left = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets height property of the window
    /// </summary>
    public int Height
    {
      get => _height;
      set
      {
        _height = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets width property of the window
    /// </summary>
    public int Width
    {
      get => _width;
      set
      {
        _width = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets or sets ratio of the pip region
    /// </summary>
    public float Ratio { get; private set; }
    /// <summary>
    /// Gets or sets visibility of the topbar
    /// </summary>
    public Visibility TopBarVisibility
    {
      get => _topBarVisibility;
      set
      {
        _topBarVisibility = value;
        UpdateDwmThumbnail();
        RaisePropertyChanged();
      }
    }
    /// <summary>
    /// Gets if topbar is visible
    /// </summary>
    public bool TopBarIsVisible => TopBarVisibility == Visibility.Visible;

    #endregion

    #region private

    private string _title;

    private float _dpiX = 1;
    private float _dpiY = 1;
    private Visibility _topBarVisibility;
    private byte _opacity = 255;
    private bool _renderSizeEventDisabled;
    private int _minHeight;
    private int _minWidth;
    private int _top;
    private int _left;
    private int _height;
    private int _width;
    private IntPtr _targetHandle,
        _thumbHandle;
    private SelectedWindow _selectedWindow;

    private enum Position
    {
      TopLeft,
      TopRight,
      BottomLeft,
      BottomRight
    }

    private readonly CancellationTokenSource _mlSource;

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public PiPModeViewModel()
    {
      Logger.Instance.Info("   ====== PiPModeWindow ======   ");

      LoadedCommand = new RelayCommand(LoadedCommandExecute);
      CloseCommand = new RelayCommand(CloseCommandExecute);
      ClosingCommand = new RelayCommand(ClosingCommandExecute);
      ChangeSelectedWindowCommand = new RelayCommand(
          ChangeSelectedWindowCommandExecute
      );
      MouseEnterCommand = new RelayCommand<MouseEventArgs>(
          MouseEnterCommandExecute
      );
      MouseLeaveCommand = new RelayCommand<MouseEventArgs>(
          MouseLeaveCommandExecute
      );
      DpiChangedCommand = new RelayCommand(DpiChangedCommandExecute);

      MessengerInstance.Register<SelectedWindow>(
          this,
          InitSelectedWindow
      );

      MinHeight = MinSize;
      MinWidth = MinSize;
    }

    /// <summary>
    /// Set selected region' data. Set position and size of this window
    /// </summary>
    /// <param name="selectedWindow">Selected window to use in pip mode</param>
    private void InitSelectedWindow(SelectedWindow selectedWindow)
    {
      if (selectedWindow == null || selectedWindow.WindowInfo == null)
      {
        Logger.Instance.Error("Can't init PiP mode");
        return;
      }

      Logger.Instance.Info(
          "Init PiP mode : " + selectedWindow.WindowInfo.Title
      );

      MessengerInstance.Unregister<SelectedWindow>(this);

      Title =
          selectedWindow.WindowInfo.Title
          + " - PiP Mode - PictureInPicture";

      _selectedWindow = selectedWindow;
      _renderSizeEventDisabled = true;
      TopBarVisibility = Visibility.Hidden;
      Ratio = _selectedWindow.Ratio;

      DpiChangedCommandExecute();
      Height = (int)(_selectedWindow.SelectedRegion.Height / _dpiY);
      Width = (int)(_selectedWindow.SelectedRegion.Width / _dpiX);
      Top = 200;
      Left = 200;

      // set Min size
      if (Height < Width)
      {
        MinWidth = MinSize * (int)Ratio;
      }
      else if (Width < Height)
      {
        MinHeight = MinSize * (int)_selectedWindow.RatioHeightByWidth;
      }

      _renderSizeEventDisabled = false;

      SetSize(DefaultSizePercentage);
      SetPosition(Position.BottomLeft);

      InitDwmThumbnail();
    }

    /// <summary>
    /// Register dwm thumbnail properties
    /// </summary>
    private void InitDwmThumbnail()
    {
      if (
          _selectedWindow == null
          || _selectedWindow.WindowInfo.Handle == IntPtr.Zero
          || _targetHandle == IntPtr.Zero
      )
      {
        return;
      }

      if (_thumbHandle != IntPtr.Zero)
      {
        _ = NativeMethods.DwmUnregisterThumbnail(_thumbHandle);
      }

      if (
          NativeMethods.DwmRegisterThumbnail(
              _targetHandle,
              _selectedWindow.WindowInfo.Handle,
              out _thumbHandle
          ) == 0
      )
      {
        UpdateDwmThumbnail();
      }
    }

    /// <summary>
    /// Update dwm thumbnail properties
    /// </summary>
    private void UpdateDwmThumbnail()
    {
      if (_thumbHandle == IntPtr.Zero)
      {
        return;
      }

      var dest = new NativeStructs.Rect(
          0,
          0,
          (int)(_width * _dpiX),
          (int)(_height * _dpiY)
      );

      var props = new NativeStructs.DwmThumbnailProperties
      {
        fVisible = true,
        dwFlags = (int)(
              DWM_TNP.DWM_TNP_VISIBLE
              | DWM_TNP.DWM_TNP_RECTDESTINATION
              | DWM_TNP.DWM_TNP_OPACITY
              | DWM_TNP.DWM_TNP_RECTSOURCE
          ),
        opacity = _opacity,
        rcDestination = dest,
        rcSource = _selectedWindow.SelectedRegion
      };

      _ = NativeMethods.DwmUpdateThumbnailProperties(
          _thumbHandle,
          ref props
      );
    }

    /// <summary>
    /// Set size of this window
    /// </summary>
    private void SetSize(float sizePercentage)
    {
      _renderSizeEventDisabled = true;

      if (Height > SystemParameters.PrimaryScreenHeight * sizePercentage)
      {
        Height = (int)(
            SystemParameters.PrimaryScreenHeight * sizePercentage
        );
        Width = Convert.ToInt32(Height * Ratio);
      }
      if (Width > SystemParameters.PrimaryScreenWidth * sizePercentage)
      {
        Width = (int)(
            SystemParameters.PrimaryScreenWidth * sizePercentage
        );
        Height = Convert.ToInt32(
            Width * _selectedWindow.RatioHeightByWidth
        );
      }
      _renderSizeEventDisabled = false;
    }

    /// <summary>
    /// Set position of this window
    /// </summary>
    private void SetPosition(Position position)
    {
      _renderSizeEventDisabled = true;
      var resolutionWidth = (int)(
          SystemParameters.PrimaryScreenWidth / _dpiX
      );
      var resolutionHeight = (int)(
          SystemParameters.PrimaryScreenHeight / _dpiY
      );
      var top = 0;
      var left = 0;
      switch (position)
      {
        case Position.TopLeft:
          top = (int)(resolutionHeight * DefaultPositionPercentage);
          left = (int)(resolutionWidth * DefaultPositionPercentage);
          break;
        case Position.TopRight:
          top = (int)(resolutionHeight * DefaultPositionPercentage);
          left =
              resolutionWidth
              - Width
              - (int)(resolutionWidth * DefaultPositionPercentage);
          break;
        case Position.BottomLeft:
          top =
              resolutionHeight
              - Height
              - (int)(resolutionHeight * DefaultPositionPercentage);
          left = (int)(resolutionWidth * DefaultPositionPercentage);
          break;
        case Position.BottomRight:
          top =
              resolutionHeight
              - Height
              - (int)(resolutionHeight * DefaultPositionPercentage);
          left =
              resolutionWidth
              - Width
              - (int)(resolutionWidth * DefaultPositionPercentage);
          break;
      }
      Top = top;
      Left = left;
      _renderSizeEventDisabled = false;
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
    /// Keep aspect ratio on window resize
    /// https://stackoverflow.com/questions/2471867/resize-a-wpf-window-but-maintain-proportions
    /// </summary>
    /// <param name="hwnd">The window handle.</param>
    /// <param name="msg">The message ID.</param>
    /// <param name="wParam">The message's wParam value.</param>
    /// <param name="lParam">The message's lParam value.</param>
    /// <param name="handeled">A value that indicates whether the message was handled. Set the value to true if the message was handled; otherwise, false.</param>
    /// <returns>The appropriate return value depends on the particular message. See the message documentation details for the Win32 message being handled.</returns>
    private IntPtr DragHook(
        IntPtr hwnd,
        int msg,
        IntPtr wParam,
        IntPtr lParam,
        ref bool handeled
    )
    {
      if ((WM)msg != WM.WINDOWPOSCHANGING)
      {
        return IntPtr.Zero;
      }

      if (_renderSizeEventDisabled)
      {
        return IntPtr.Zero;
      }

      var position = (NativeStructs.WINDOWPOS)Marshal.PtrToStructure(
          lParam,
          typeof(NativeStructs.WINDOWPOS)
      );
      if (
          (position.flags & (int)SWP.NOMOVE) != 0
          || HwndSource.FromHwnd(hwnd)?.RootVisual == null
      )
      {
        return IntPtr.Zero;
      }

      position.cx = (int)(position.cy * Ratio);

      Marshal.StructureToPtr(position, lParam, true);
      handeled = true;

      return IntPtr.Zero;
    }

    /// <inheritdoc />
    /// <summary>
    /// Remove DragHook
    /// </summary>
    public void Dispose()
    {
      _mlSource?.Cancel();
      (
          (HwndSource)PresentationSource.FromVisual(ThisWindow())
      )?.RemoveHook(DragHook);
    }

    #region commands

    /// <summary>
    /// Executed when the window is loaded. Get handle of the window and call <see cref="InitDwmThumbnail"/> 
    /// </summary>
    private void LoadedCommandExecute()
    {
      ((HwndSource)PresentationSource.FromVisual(ThisWindow()))?.AddHook(
          DragHook
      );
      var windowsList = Application.Current.Windows.Cast<Window>();
      var thisWindow = windowsList.FirstOrDefault(
          x => x.DataContext == this
      );
      if (thisWindow != null)
      {
        _targetHandle = new WindowInteropHelper(thisWindow).Handle;
      }
      InitDwmThumbnail();
    }

    /// <summary>
    /// Executed on click on close button. Close this window
    /// </summary>
    private void CloseCommandExecute()
    {
      MessengerInstance.Unregister<SelectedWindow>(this);
      RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Executed when the window is closing.
    /// </summary>
    private void ClosingCommandExecute()
    {
      Logger.Instance.Info("   |||||| Close PiPModeWindow ||||||   ");
      Dispose();
    }

    /// <summary>
    /// Executed on click on change selected window button. Close this window and open <see cref="MainWindow"/>
    /// </summary>
    private void ChangeSelectedWindowCommandExecute()
    {
      // var mainWindow = new MainWindow();
      // mainWindow.Show();
      CloseCommandExecute();

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

    /// <summary>
    /// Executed on mouse enter. Open top bar
    /// </summary>
    /// <param name="e">Event arguments</param>
    private void MouseEnterCommandExecute(MouseEventArgs e)
    {
      if (TopBarIsVisible)
      {
        return;
      }
      _renderSizeEventDisabled = true;
      // TODO: only set opacity on video, this is also effecting the button
      _opacity = Constants.PiPOpacityOnHover;
      TopBarVisibility = Visibility.Visible;
      _renderSizeEventDisabled = false;
      e.Handled = true;
    }

    /// <summary>
    /// Executed on mouse leave. Close top bar
    /// </summary>
    /// <param name="e">Event arguments</param>
    private void MouseLeaveCommandExecute(MouseEventArgs e)
    {
      // Prevent OnMouseEnter, OnMouseLeave loop
      Thread.Sleep(50);
      NativeMethods.GetCursorPos(out var p);
      var r = new Rectangle(
          Convert.ToInt32(Left),
          Convert.ToInt32(Top),
          Convert.ToInt32(Width),
          Convert.ToInt32(Height)
      );
      var pa = new Point(Convert.ToInt32(p.X), Convert.ToInt32(p.Y));

      if (!TopBarIsVisible || r.Contains(pa))
      {
        return;
      }
      TopBarVisibility = Visibility.Hidden;
      _renderSizeEventDisabled = true;
      _opacity = 255;
      _renderSizeEventDisabled = false;
      e.Handled = true;
    }

    /// <summary>
    /// Executed on DPI change to handle multi-screen with different DPI
    /// </summary>
    private void DpiChangedCommandExecute()
    {
      DpiHelper.GetDpi(_targetHandle, out _dpiX, out _dpiY);
    }
    #endregion

  }
}
