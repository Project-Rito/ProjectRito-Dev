using System;
using System.Collections.Generic;
using System.Text;

namespace GLFrameworkEngine
{
    /// <summary>
    /// Represents an input used from an IDrawable instance in the scene.
    /// </summary>
    public interface IDrawableInput
    {
        void OnMouseDown();
        void OnMouseUp();
        void OnMouseMove();
        void OnKeyDown();
    }
}
