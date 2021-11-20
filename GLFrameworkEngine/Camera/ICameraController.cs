using OpenTK;

namespace GLFrameworkEngine
{
    public interface ICameraController
    {
        void MouseClick();
        void MouseMove(Vector2 previousLocation);
        void MouseWheel();
        void KeyPress();
    }
}
