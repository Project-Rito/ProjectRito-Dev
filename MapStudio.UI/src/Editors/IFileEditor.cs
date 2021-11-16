using System;
using System.Collections.Generic;
using System.Text;

namespace MapStudio.UI
{
    /// <summary>
    /// Attaches a custom editor to a file format.
    /// </summary>
    public interface ICustomFileEditor
    {
        IEditor Editor { get; set; }
    }
}
