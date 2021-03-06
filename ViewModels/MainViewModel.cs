using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Mvvm.ComponentModel;
using Microsoft.Toolkit.Mvvm.Input;
using Microsoft.Toolkit.Mvvm.Messaging;
using PictureInPicture.DataModel;
using PictureInPicture.Interfaces;
using PictureInPicture.Native;
using PictureInPicture.Services;
using PictureInPicture.Views;

namespace PictureInPicture.ViewModels
{
  public class MainViewModel : ObservableRecipient, ICloseable
  {
    private readonly ILogger<MainViewModel> _logger;
    private readonly ProcessesService _processesService;

    #region public

    public event EventHandler<EventArgs> RequestClose;

    public ICommand TogglePipCommand { get; }
    public ICommand LockPipCommand { get; }
    public ICommand SetupPipCommand { get; }
    public ICommand QuitCommand { get; }
    public ICommand ClosingCommand { get; }

    public bool HasSelectedWindow
    {
      get => SelectedWindow != null;
    }

    /// <summary>
    /// Gets or sets selected window info and call <see cref="ShowCropper"/>
    /// </summary>
    public WindowInfo SelectedWindowInfo
    {
      get => _selectedWindowInfo;
      set
      {
        if (_selectedWindowInfo == value)
        {
          return;
        }
        _selectedWindowInfo = value;
        if (value != null)
        {
          ShowCropper();
        }
        OnPropertyChanged();
        OnPropertyChanged(nameof(InfoText));
      }
    }
    public SelectedWindow SelectedWindow
    {
      get => _selectedWindow;
      set
      {
        if (_selectedWindow == value)
        {
          return;
        }
        _selectedWindow = value;
        OnPropertyChanged();
        OnPropertyChanged(nameof(HasSelectedWindow));
        OnPropertyChanged(nameof(InfoText));
      }
    }
    /// <summary>
    /// Gets or sets windows list
    /// </summary>
    public ObservableCollection<WindowInfo> WindowsList
    {
      get => _windowsList;
      set
      {
        _windowsList = value;
        OnPropertyChanged();
      }
    }

    public bool EnableTargetNextFocusedWindow
    {
      get => _enableTargetNextFocusedWindow;
      set
      {
        _enableTargetNextFocusedWindow = value;
        OnPropertyChanged();
      }
    }

    public bool LockPipControls
    {
      get => _lockPipControls;
      set
      {
        _lockPipControls = value;
        if (SelectedWindow != null)
        {
          SelectedWindow.DisableControls = _lockPipControls;
          Messenger.Send(SelectedWindow);
        }
        OnPropertyChanged();
      }
    }

    public string InfoText
    {
      get
      {
        if (SelectedWindowInfo == null)
        {
          return "Click the \"Select a window\" button to get started";
        }
        if (SelectedWindowInfo != null && SelectedWindow == null)
        {
          return "Setting up " + SelectedWindowInfo.Title;
        }
        if (SelectedWindow != null)
        {
          return SelectedWindowInfo.Title + " is ready";
        }
        return "";
      }
    }

    #endregion

    #region private

    private ObservableCollection<WindowInfo> _windowsList;
    private CropperWindow _cropperWindow;
    private WindowInfo _selectedWindowInfo;
    private SelectedWindow _selectedWindow;
    private PipModeWindow _pipModeWindow;
    private bool _enableTargetNextFocusedWindow = false;
    private bool _lockPipControls = false;

    #endregion

    /// <inheritdoc />
    /// <summary>
    /// Constructor
    /// </summary>
    public MainViewModel(ILogger<MainViewModel> logger,
      ProcessesService processesService
    )
    {
      _logger = logger;
      _processesService = processesService;

      _logger?.LogInformation("   ====== MainWindow ======   ");

      TogglePipCommand = new RelayCommand(TogglePipCommandExecute);
      LockPipCommand = new RelayCommand(LockPipCommandExecute);
      SetupPipCommand = new RelayCommand(SetupPipCommandExecute);
      QuitCommand = new RelayCommand(QuitCommandExecute);
      ClosingCommand = new RelayCommand(ClosingCommandExecute);

      WindowsList = new ObservableCollection<WindowInfo>();

      Messenger.Register<PipModeWindow>(
        this, (_, m) => HandlePipModeWindowChange(m));
      Messenger.Register<SelectedWindow>(
        this, (_, m) => HandleSelectedWindowChange(m));

      _processesService.OpenWindowsChanged += OpenWindowsChanged;
      _processesService.ForegroundWindowChanged +=
          ForegroundWindowChanged;
      UpdateWindowsList();
    }

    /// <summary>
    /// Update <see cref="WindowsList"/> with <see cref="ProcessesService.OpenWindows"/> 
    /// </summary>
    private void UpdateWindowsList()
    {
      _logger?.LogInformation("Windows list updated");
      var openWindows = _processesService.OpenWindows;

      var toAdd = openWindows.Where(x => WindowsList.All(y => x != y));
      var toRemove = WindowsList
          .Where(x => openWindows.All(y => x != y))
          .ToList();

      foreach (var e in toAdd)
      {
        WindowsList.Add(e);
      }
      for (var index = 0; index < toRemove.Count; index++)
      {
        WindowsList.Remove(toRemove[index]);
      }
      // TODO: Convert WindowsList to a CollectionViewSource, see https://stackoverflow.com/a/19113072/6535663
      WindowsList = new ObservableCollection<WindowInfo>(WindowsList.OrderBy(i => i.Title));
    }

    /// <summary>
    /// Close old cropper if exist, and open new with <see cref="SelectedWindowInfo"/>
    /// </summary>
    private void ShowCropper()
    {
      _cropperWindow?.Close();
      _cropperWindow = new CropperWindow();
      Messenger.Send(SelectedWindowInfo);
      _cropperWindow.Show();
    }

    private void HandlePipModeWindowChange(PipModeWindow pipModeWindow)
    {
      _pipModeWindow = pipModeWindow;
    }

    private void HandleSelectedWindowChange(SelectedWindow selectedWindow)
    {
      SelectedWindowInfo = selectedWindow?.WindowInfo;
      SelectedWindow = selectedWindow;
    }

    /// <summary>
    /// Callback in <see cref="StartPipCommandExecute"/>. Show <see cref="PipModeWindow"/> and send selected window
    /// </summary>
    /// <param name="selectedRegion"></param>
    private void StartPip(NativeStructs.Rect selectedRegion)
    {
      _pipModeWindow = new PipModeWindow();
      SelectedWindow.SelectedRegion = selectedRegion;
      Messenger.Send(SelectedWindow);
      _pipModeWindow.Show();
    }

    /// <summary>
    /// Called when foreground window changed. Change <see cref="SelectedWindowInfo"/>
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event arguments</param>
    private void ForegroundWindowChanged(object sender, EventArgs e)
    {
      UpdateWindowsList();
      var foregroundWindow = _processesService.ForegroundWindow;
      if (foregroundWindow != null)
      {
        if (EnableTargetNextFocusedWindow)
        {
          SelectedWindowInfo = foregroundWindow;
        }
        _logger?.LogInformation(
            "Foreground window updated : " + SelectedWindowInfo?.Title
        );
      }
      else
        _logger?.LogWarning(
            "Foreground window updated but window is null"
        );
    }

    /// <summary>
    /// Called when open windows changed
    /// </summary>
    /// <param name="sender">The source of the event</param>
    /// <param name="e">Event arguments</param>
    private void OpenWindowsChanged(object sender, EventArgs e) =>
        UpdateWindowsList();

    private void CloseAllWindows()
    {
      var windowsList = Application.Current.Windows.Cast<Window>();
      foreach (var window in windowsList)
      {
        if (window.DataContext != this)
        {
          window.Close();
        }
      }
    }

    #region commands

    /// <summary>
    /// Executed on click on change selected window button. Send message with <see cref="StartPip"/> callback
    /// </summary>
    private void TogglePipCommandExecute()
    {
      if (SelectedWindowInfo == null)
      {
        Messenger.Send<Action<NativeStructs.Rect>>(StartPip);
      }
      else if (SelectedWindow != null
        && SelectedWindow.PictureInPictureEnabled
        && _pipModeWindow != null)
      {
        _selectedWindow.PipSize = new Vector2(
          (float)_pipModeWindow.Width, (float)_pipModeWindow.Height);
        _selectedWindow.PipPosition = new Vector2(
          (float)_pipModeWindow.Left, (float)_pipModeWindow.Top);
        _pipModeWindow.Close();
        SelectedWindow.PictureInPictureEnabled = false;

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
      else if (SelectedWindow != null)
      {
        StartPip(SelectedWindow.SelectedRegion);
        SelectedWindow.PictureInPictureEnabled = true;
      }
    }

    private void LockPipCommandExecute()
    {
      if (SelectedWindow != null)
      {
        SelectedWindow.DisableControls = true;
        Messenger.Send(SelectedWindow);
      }
    }

    private void SetupPipCommandExecute()
    {
      if (_pipModeWindow != null)
      {
        _pipModeWindow.Close();
      }

      if (_cropperWindow != null)
      {
        _cropperWindow?.Close();
        _cropperWindow = new CropperWindow();
        Messenger.Send(SelectedWindowInfo);

        _selectedWindow.PipSize = new Vector2(
          (float)_pipModeWindow.Width, (float)_pipModeWindow.Height);
        _selectedWindow.PipPosition = new Vector2(
          (float)_pipModeWindow.Left, (float)_pipModeWindow.Top);
        Messenger.Send(SelectedWindow);

        _cropperWindow.Show();
      }
    }

    /// <summary>
    /// Executed on click on quit button
    /// </summary>
    private void QuitCommandExecute()
    {
      CloseAllWindows();
      RequestClose?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Executed when the window is closing. Dispose <see cref="ProcessesService"/>. Close <see cref="CropperWindow"/>
    /// </summary>
    private void ClosingCommandExecute()
    {
      _logger.LogInformation("   |||||| Close MainWindow ||||||   ");

      Messenger.Unregister<PipModeWindow>(this);
      Messenger.Unregister<SelectedWindow>(this);
      _processesService.OpenWindowsChanged -= OpenWindowsChanged;
      _processesService.ForegroundWindowChanged -= ForegroundWindowChanged;
      _processesService.Dispose();

      CloseAllWindows();

      _cropperWindow?.Close();
    }
    #endregion

  }
}
