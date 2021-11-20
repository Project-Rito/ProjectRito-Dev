using OpenTK;

namespace GLFrameworkEngine
{
    public interface ICameraController
    {
        void MouseClick(float frameTime);
        void MouseMove(Vector2 previousLocation, float frametime);
        void MouseWheel(float frameTime);
        void KeyPress(float frameTime);
    }
}
