using System;
using System.Numerics;

namespace MapStudio.UI
{
    public class ColorCycle
    {
        public float Brightness = 0.5f;
        public float Alpha = 1f;

        private float CurrentHue = 0f;
        private float Factor = 0.5f;

        public Vector4 NextColor()
        {
            Vector4 res = ColorFromSpectrum(CurrentHue);
            CurrentHue += Factor;
            if ((Factor * 2) % CurrentHue == 0 && Factor < 0.5f)
                CurrentHue += Factor;
            if (CurrentHue + Factor > 1f)
            {
                Factor /= 2;
                CurrentHue = 0f + Factor;
            }

            return res;
        }

        // Modified from stackoverflow - https://stackoverflow.com/questions/18057203/c-sharp-looping-through-colors
        private Vector4 ColorFromSpectrum(float w)
        {
            float r = 0.0f;
            float g = 0.0f;
            float b = 0.0f;

            w *= 100;
            w %= 100f;

            if (w < 17)
            {
                r = -(w - 17.0f) / 17.0f;
                b = 1.0f;
            }
            else if (w < 33)
            {
                g = (w - 17.0f) / (33.0f - 17.0f);
                b = 1.0f;
            }
            else if (w < 50)
            {
                g = 1.0f;
                b = -(w - 50.0f) / (50.0f - 33.0f);
            }
            else if (w < 67)
            {
                r = (w - 50.0f) / (67.0f - 50.0f);
                g = 1.0f;
            }
            else if (w < 83)
            {
                r = 1.0f;
                g = -(w - 83.0f) / (83.0f - 67.0f);
            }
            else
            {
                r = 1.0f;
                b = (w - 83.0f) / (100.0f - 83.0f);
            }

            return new Vector4(r * Brightness, g * Brightness, b * Brightness, Alpha);
        }
    }
}
