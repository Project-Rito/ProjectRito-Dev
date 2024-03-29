﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GLFrameworkEngine
{
    public class InputSettings
    {
        public static InputSettings INPUT = new InputSettings();

        public TransformInput Transform = new TransformInput();
        public Scene3D Scene = new Scene3D();
        public Camera3D Camera = new Camera3D();

        public class TransformInput
        {
            public string AxisX = "X";
            public string AxisY = "Y";
            public string AxisZ = "Z";

            public string TranslateGizmo = "Ctrl+1";
            public string ScaleGizmo = "Ctrl+2";
            public string RotateGizmo = "Ctrl+3";

            public string Translate = "G";
            public string Scale = "T";
            public string Rotate = "R";
        }

        public class Scene3D
        {
            public string ShowAddContextMenu = "Ctrl+Q";
            public string SelectAll = "Ctrl+!Shift+A";
            public string DeselectAll = "Ctrl+Shift+A";
            public string Undo = "Ctrl+Z";
            public string Redo = "Ctrl+R";
            public string EditMode = "Tab";
            public string Copy = "Ctrl+C";
            public string Paste = "Ctrl+V";
            public string Delete = "Delete";
            public string Extrude = "E";
            public string Create = "Q";
            public string Fill = "F";
            public string Merge = "M";
            public string Insert = "I";
            public string SelectionBox = "B";
            public string SelectionCircle = "C";
        }

        public class Camera3D
        {
            public string AxisX = "X";
            public string AxisY = "Y";

            public string MoveForward = "W";
            public string MoveBack = "S";
            public string MoveLeft = "A";
            public string MoveRight = "D";

            public string MoveUp = "Space";
            public string MoveDown = "Ctrl+Space";

            public string CameraFront = "keypad1";
            public string CameraRight = "keypad3";
            public string CameraOrtho = "keypad5";
            public string CameraTop = "keypad7";
            public string CameraLeft = "keypad9";
        }
    }
}
