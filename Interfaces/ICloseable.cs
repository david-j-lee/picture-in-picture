using System;

namespace PictureInPicture.Interfaces
{
  public interface ICloseable
  {
    event EventHandler<EventArgs> RequestClose;
  }
}
