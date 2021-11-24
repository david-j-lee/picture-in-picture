﻿using System;
using System.IO;
using PictureInPicture.Shared.Helpers;

namespace PictureInPicture.Shared
{
  public static class Constants
  {

    public static readonly string FolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PiP-Tool");

    public static readonly string DataPath = Path.Combine(FolderPath, "Data.csv");

    public static readonly string ModelPath = Path.Combine(FolderPath, "Model.zip");

    public static readonly string LogPath = Path.Combine(FolderPath, "logs.txt");

    public static int MinCropperSize => 100;

  }
}
