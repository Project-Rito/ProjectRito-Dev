using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using Newtonsoft.Json;
using MapStudio.UI;
using GLFrameworkEngine;
using Toolbox.Core;

namespace MapStudio.UI
{
    public class GlobalSettings
    {
        public static GlobalSettings Current;

        /// <summary>
        /// The global program settings.
        /// </summary>
        public ProgramSettings Program { get; set; } = new ProgramSettings();

        /// <summary>
        /// The program input settings.
        /// </summary>
        public InputSettings InputSettings { get; set; } = new InputSettings();

        /// <summary>
        /// The global camera settings used in the 3d viewer.
        /// </summary>
        public CameraSettings Camera { get; set; } = new CameraSettings();

        /// <summary>
        /// The global 3d viewer settings.
        /// </summary>
        public ViewerSettings Viewer { get; set; } = new ViewerSettings();

        /// <summary>
        /// The global settings used by the asset window.
        /// </summary>
        public AssetSettings Asset { get; set; } = new AssetSettings();

        /// <summary>
        /// The global 3d viewer background settings.
        /// </summary>
        public BackgroundSettings Background { get; set; } = new BackgroundSettings();

        /// <summary>
        /// The global 3d viewer grid settings.
        /// </summary>
        public GridSettings Grid { get; set; } = new GridSettings();

        /// <summary>
        /// The global 3d viewer bone settings.
        /// </summary>
        public BoneSettings Bones { get; set; } = new BoneSettings();

        private GLContext _context;
        private Camera _camera;

        public GlobalSettings() { Current = this; }

        public GlobalSettings(GLContext context, Camera camera)
        {
            _context = context;
            _camera = camera;
            Current = this;
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static GlobalSettings Load()
        {
            if (!File.Exists($"{Runtime.ExecutableDir}/ConfigGlobal.json")) { new GlobalSettings().Save(); }

            var config = JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText($"{Runtime.ExecutableDir}/ConfigGlobal.json"), new
                JsonSerializerSettings()
            {
                //If settings get added, don't alter the defaults
                NullValueHandling = NullValueHandling.Ignore,
            });
            return config;
        }

        /// <summary>
        /// Reloads the current language in the program.
        /// </summary>
        public void ReloadLanguage() {
            TranslationSource.Instance.Update(Program.Language);
        }

        /// <summary>
        /// Reloads the current theme in the program.
        /// </summary>
        public void ReloadTheme()
        {
            var theme = ThemeHandler.Themes.FirstOrDefault(x => x.Name == Program.Theme);
            if (theme != null)
                ThemeHandler.UpdateTheme(theme);
        }

        /// <summary>
        /// Updates the gl context settings from the configuration file.
        /// </summary>
        public void ReloadContext(GLContext context, Camera camera)
        {
            _context = context;
            _camera = camera;
            ApplyConfiguration();
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void SaveDefaults()
        {
            Save();
        }

        /// <summary>
        /// Saves the current configuration to json on disc.
        /// </summary>
        public void Save()
        {
            File.WriteAllText($"{Runtime.ExecutableDir}/ConfigGlobal.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            ApplyConfiguration();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void ApplyConfiguration()
        {
            if (_context != null)
            {
                _camera.Mode = Camera.Mode;
                _camera.IsOrthographic = Camera.IsOrthographic;
                _camera.KeyMoveSpeed = Camera.KeyMoveSpeed;
                _camera.PanSpeed = Camera.PanSpeed;
                _camera.ZoomSpeed = Camera.ZoomSpeed;
                _camera.ZNear = Camera.ZNear;
                _camera.ZFar = Camera.ZFar;
                _camera.FovDegrees = Camera.FovDegrees;

                _context.EnableFog = Viewer.DisplayFog;
                _context.EnableBloom = Viewer.DisplayBloom;
            }

            DrawableBackground.Display = Background.Display;
            DrawableBackground.BackgroundTop = Background.TopColor;
            DrawableBackground.BackgroundBottom = Background.BottomColor;

            DrawableGridFloor.Display = Grid.Display;
            DrawableGridFloor.GridColor = Grid.Color;
            DrawableGridFloor.CellAmount = Grid.CellCount;
            DrawableGridFloor.CellSize = Grid.CellSize;

            Toolbox.Core.Runtime.DisplayBones = Bones.Display;
            Toolbox.Core.Runtime.BonePointSize = Bones.Size;
        }

        /// <summary>
        /// Updates the configuration with the current program values.
        /// </summary>
        public void LoadCurrentSettings()
        {
            Program.Language = TranslationSource.LanguageKey;

            Camera.Mode = _camera.Mode;
            Camera.IsOrthographic = _camera.IsOrthographic;
            Camera.KeyMoveSpeed = _camera.KeyMoveSpeed;
            Camera.ZoomSpeed = _camera.ZoomSpeed;
            Camera.PanSpeed = _camera.PanSpeed;
            Camera.ZNear = _camera.ZNear;
            Camera.ZFar = _camera.ZFar;
            Camera.FovDegrees = _camera.FovDegrees;

            Viewer.DisplayBloom = _context.EnableBloom;
            Viewer.DisplayFog = _context.EnableFog;

            Background.Display = DrawableBackground.Display;
            Background.TopColor = DrawableBackground.BackgroundTop;
            Background.BottomColor = DrawableBackground.BackgroundBottom;

            Grid.Display = DrawableGridFloor.Display;
            Grid.Color = DrawableGridFloor.GridColor;
            Grid.CellSize = Toolbox.Core.Runtime.GridSettings.CellSize;
            Grid.CellCount = Toolbox.Core.Runtime.GridSettings.CellAmount;

            Bones.Display = Toolbox.Core.Runtime.DisplayBones;
            Bones.Size = Toolbox.Core.Runtime.BonePointSize;
        }

        public class ProgramSettings
        {
            public string Theme { get; set; } = "DARK_BLUE_THEME";

            /// <summary>
            /// The language of the program.
            /// </summary>
            public string Language { get; set; } = "English";

            /// <summary>
            /// Gets the current project directory.
            /// </summary>
            private string _projectDirectory = DefaultProjectPath();
            public string ProjectDirectory {
                get
                {
                    Directory.CreateDirectory(_projectDirectory);
                    return _projectDirectory;
                }
                set
                {
                    Directory.CreateDirectory(value);
                    _projectDirectory = value;
                }
            }

            /// <summary>
            /// Gets the default project directory from the user's local application directory.
            /// </summary>
            /// <returns></returns>
            static string DefaultProjectPath()
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return $"{local}/MapStudio/Projects";
            }
        }

        public class ViewerSettings
        {
            /// <summary>
            /// Toggles displaying fog in the 3D view.
            /// </summary>
            public bool DisplayFog { get; set; } = true;

            /// <summary>
            /// Toggles displaying bloom in the 3D view.
            /// </summary>
            public bool DisplayBloom { get; set; } = false;
        }

        public class AssetSettings
        {
            /// <summary>
            /// Makes 3d assets face the camera on the Y axis during a drop spawn.
            /// </summary>
            public bool FaceCameraAtSpawn { get; set; } = false;
        }

        public class BackgroundSettings
        {
            /// <summary>
            /// Toggles displaying the background in the 3D view.
            /// </summary>
            public bool Display { get; set; } = false;

            /// <summary>
            /// Gets or sets the top color backgroun gradient.
            /// </summary>
            public Vector3 TopColor { get; set; } = new Vector3(0.1f, 0.1f, 0.1f);

            /// <summary>
            /// Gets or sets the bottom color backgroun gradient.
            /// </summary>
            public Vector3 BottomColor { get; set; } = new Vector3(0.2f, 0.2f, 0.2f);
        }

        public class GridSettings
        {
            /// <summary>
            /// Toggles displaying the grid in the 3D view.
            /// </summary>
            public bool Display { get; set; } = true;

            /// <summary>
            /// Gets or sets the grid color.
            /// </summary>
            public Vector4 Color { get; set; } = new Vector4(0.3f, 0.3f, 0.3f, 1.0f);

            /// <summary>
            /// Gets or sets the number of grid cells.
            /// </summary>
            public int CellCount { get; set; } = 10;

            /// <summary>
            /// Gets or sets the size of grid cells.
            /// </summary>
            public float CellSize { get; set; } = 1;
        }

        public class BoneSettings
        {
            /// <summary>
            /// Toggles displaying the bone in the 3D view.
            /// </summary>
            public bool Display { get; set; } = false;

            /// <summary>
            /// Gets or sets the bone point size.
            /// </summary>
            public float Size { get; set; } = 0.1f;
        }

        public class CameraSettings
        {
            /// <summary>
            /// Gets or sets the camera controller mode to determine how the camera moves.
            /// </summary>
            public Camera.CameraMode Mode { get; set; } = GLFrameworkEngine.Camera.CameraMode.FlyAround;

            /// <summary>
            /// Toggles orthographic projection.
            /// </summary>
            public bool IsOrthographic { get; set; } = false;

            /// <summary>
            /// Gets or sets the field of view in degrees.
            /// </summary>
            public float FovDegrees { get; set; } = 45;

            /// <summary>
            /// Gets or sets the camera move speed using key inputs.
            /// </summary>
            public float KeyMoveSpeed { get; set; } = 100.0f;

            /// <summary>
            /// Gets or sets the camera move speed during panning.
            /// </summary>
            public float PanSpeed { get; set; } = 1.0f;

            /// <summary>
            /// Gets or sets the camera move speed during zooming.
            /// </summary>
            public float ZoomSpeed { get; set; } = 1.0f;

            /// <summary>
            /// Gets or sets the z near projection.
            /// </summary>
            public float ZNear { get; set; } = 1.0f;

            /// <summary>
            /// Gets or sets the z far projection.
            /// </summary>
            public float ZFar { get; set; } = 100000.0f;
        }
    }
}
