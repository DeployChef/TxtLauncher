using System;

namespace TxtLauncher.Utils.Interfaces
{
    public interface ISelectable
    {
        bool IsSelected { get; set; }

        event EventHandler<bool> SelectionChanged;
    }
}
