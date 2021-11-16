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

        public PathSettings PathDrawer { get; set; } = new PathSettings();

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

        public GlobalSettings() { Current = this; }

        public GlobalSettings(GLContext context)
        {
            _context = context;
            Current = this;
        }

        /// <summary>
        /// Loads the config json file on disc or creates a new one if it does not exist.
        /// </summary>
        /// <returns></returns>
        public static GlobalSettings Load()
        {
            if (!File.Exists($"{Runtime.ExecutableDir}\\ConfigGlobal.json")) { new GlobalSettings().Save(); }

            var config = JsonConvert.DeserializeObject<GlobalSettings>(File.ReadAllText($"{Runtime.ExecutableDir}\\ConfigGlobal.json"), new
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
            if (ThemeHandler.Themes.ContainsKey(Program.Theme))
                ThemeHandler.UpdateTheme(ThemeHandler.Themes[Program.Theme]);
        }

        /// <summary>
        /// Updates the gl context settings from the configuration file.
        /// </summary>
        public void ReloadContext(GLContext context)
        {
            _context = context;
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
            File.WriteAllText($"{Runtime.ExecutableDir}\\ConfigGlobal.json", JsonConvert.SerializeObject(this, Formatting.Indented));
            ApplyConfiguration();
        }

        /// <summary>
        /// Called when the config file has been loaded or saved.
        /// </summary>
        public void ApplyConfiguration()
        {
            if (_context != null)
            {
                _context.Camera.Mode = Camera.Mode;
                _context.Camera.IsOrthographic = Camera.IsOrthographic;
                _context.Camera.KeyMoveSpeed = Camera.KeyMoveSpeed;
                _context.Camera.PanSpeed = Camera.PanSpeed;
                _context.Camera.ZoomSpeed = Camera.ZoomSpeed;
                _context.Camera.ZNear = Camera.ZNear;
                _context.Camera.ZFar = Camera.ZFar;
                _context.Camera.FovDegrees = Camera.FovDegrees;

                _context.EnableFog = Viewer.DisplayFog;
                _context.EnableBloom = Viewer.DisplayBloom;
            }

            DrawableBackground.Display = Background.Display;
            DrawableBackground.BackgroundTop = Background.TopColor;
            DrawableBackground.BackgroundBottom = Background.BottomColor;

            DrawableFloor.Display = Grid.Display;
            DrawableFloor.GridColor = Grid.Color;
            Toolbox.Core.Runtime.GridSettings.CellSize = Grid.CellSize;
            Toolbox.Core.Runtime.GridSettings.CellAmount = Grid.CellCount;

            Toolbox.Core.Runtime.DisplayBones = Bones.Display;
            Toolbox.Core.Runtime.BonePointSize = Bones.Size;
        }

        /// <summary>
        /// Updates the configuration with the current program values.
        /// </summary>
        public void LoadCurrentSettings()
        {
            Program.Language = TranslationSource.LanguageKey;

            Camera.Mode = _context.Camera.Mode;
            Camera.IsOrthographic = _context.Camera.IsOrthographic;
            Camera.KeyMoveSpeed = _context.Camera.KeyMoveSpeed;
            Camera.ZoomSpeed = _context.Camera.ZoomSpeed;
            Camera.PanSpeed = _context.Camera.PanSpeed;
            Camera.ZNear = _context.Camera.ZNear;
            Camera.ZFar = _context.Camera.ZFar;
            Camera.FovDegrees = _context.Camera.FovDegrees;

            Viewer.DisplayBloom = _context.EnableBloom;
            Viewer.DisplayFog = _context.EnableFog;

            Background.Display = DrawableBackground.Display;
            Background.TopColor = DrawableBackground.BackgroundTop;
            Background.BottomColor = DrawableBackground.BackgroundBottom;

            Grid.Display = DrawableFloor.Display;
            Grid.Color = DrawableFloor.GridColor;
            Grid.CellSize = Toolbox.Core.Runtime.GridSettings.CellSize;
            Grid.CellCount = Toolbox.Core.Runtime.GridSettings.CellAmount;

            Bones.Display = Toolbox.Core.Runtime.DisplayBones;
            Bones.Size = Toolbox.Core.Runtime.BonePointSize;
        }

        public class ProgramSettings
        {
            public string Theme { get; set; } = "DARK_THEME";

            /// <summary>
            /// The language of the program.
            /// </summary>
            public string Language { get; set; } = "English";

            /// <summary>
            /// Gets the current project directory.
            /// </summary>
            public string ProjectDirectory = DefaultProjectPath();

            /// <summary>
            /// Gets the default project directory from the user's local application directory.
            /// </summary>
            /// <returns></returns>
            static string DefaultProjectPath()
            {
                string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                return $"{local}\\MapStudio";
            }
        }

        public class PathSettings
        {
            public PathColor EnemyColor = new PathColor(new Vector3(255, 0, 0), new Vector3(255, 255, 0), new Vector3(255, 128, 0));
            public PathColor ItemColor = new PathColor(new Vector3(0, 255, 0), new Vector3(36, 165, 36), new Vector3(0, 128, 0));
            public PathColor GlideColor = new PathColor(new Vector3(255, 128, 0), new Vector3(255, 255, 128), new Vector3(255, 200, 0));
            public PathColor PullColor = new PathColor(new Vector3(165, 61, 158), new Vector3(208, 158, 208), new Vector3(255, 65, 244));
            public PathColor RailColor = new PathColor(new Vector3(170, 0, 160), new Vector3(255, 64, 255), new Vector3(255, 64, 255));

            public PathColor LapColor = new PathColor(new Vector3(0, 0, 255), new Vector3(0, 0, 200), new Vector3(80, 0, 0));
            public PathColor GravityColor = new PathColor(new Vector3(200, 0, 200), new Vector3(255, 85, 255), new Vector3(180, 0, 255));
            public PathColor GravityCameraColor = new PathColor(new Vector3(240, 100, 255), new Vector3(255, 85, 255), new Vector3(180, 9, 255));

            public PathColor SteerAssistColor = new PathColor(new Vector3(100, 100, 100), new Vector3(150, 150, 150), new Vector3(70, 70, 70));
        }

        public class PathColor
        {
            public Vector3 PointColor = new Vector3(0, 0, 0);
            public Vector3 LineColor = new Vector3(0, 0, 0);
            public Vector3 ArrowColor = new Vector3(0, 0, 0);

            [JsonIgnore]
            public EventHandler OnColorChanged;

            public PathColor(Vector3 point, Vector3 line, Vector3 arrow)
            {
                PointColor = new Vector3(point.X / 255f, point.Y / 255f, point.Z / 255f);
                LineColor = new Vector3(line.X / 255f, line.Y / 255f, line.Z / 255f);
                ArrowColor = new Vector3(arrow.X / 255f, arrow.Y / 255f, arrow.Z / 255f);
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
            /// Makes 3d assets face the camera on the Y axis during a drop spawn..
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
