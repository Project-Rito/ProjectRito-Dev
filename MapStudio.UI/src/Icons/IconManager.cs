using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using GLFrameworkEngine;
using ImGuiNET;

namespace MapStudio.UI
{
    public class IconManager
    {
        static Dictionary<string, int> Icons = new Dictionary<string, int>();

        //Open Font Icons
        public const char MATERIAL_MASK_ICON = '\ue067';
        public const char MATERIAL_TRANSLUCENT_ICON = '\ue067';
        public const char MATERIAL_OPAQUE_ICON = '\ue067';
        public const char FOLDER_ICON = '\ue067';
        public const char FILE_ICON = '\ue061';
        public const char EYE_ON_ICON = '\ue05b';
        public const char EYE_OFF_ICON = '\ue05a';
        public const char MODEL_ICON = '\ue025';
        public const char MESH_ICON = '\ue00a';
        public const char X_ICON = '\ue00a';
        public const char Y_ICON = '\ue00a';
        public const char Z_ICON = '\ue00a';
        public const char W_ICON = '\ue00a';
        public const char ANIMATION_ICON = '\ue00a';
        public const char SEARCH_ICON = '\uf002';
        public const char SETTINGS_ICON = '\uf013';
        public const char FILTER_ICON = '\uf0b0';
        public const char SPARK_ICON = '\uf666';
        public const char PARTICLE_ICON = '\uf0d0';
        public const char BONE_ICON = '\uf5d7';
        public const char STAR_ICON = '\uf005';

        //Font Awesome
        public const char NEW_FILE_ICON = '\uf477';
        public const char OPEN_ICON = '\uf07c';
        public const char SAVE_ICON = '\uf0c7';
        public const char RECENT_ICON = '\uf07c';
        public const char PROJECT_ICON = '\uf1c4';
        public const char PLAY_ICON = '\uf04b';
        public const char PAUSE_ICON = '\uf04c';
        public const char UNDO_ICON = '\uf2ea';
        public const char REDO_ICON = '\uf2f9';
        public const char CAMERA_ICON = '\uf030';

        public const string ICON_3D = "3D";
        public const string ICON_2D = "2D";

        public const char ARROW_ICON = '\uf245';
        public const char TRANSLATE_ICON = '\uf0b2';
        public const char ROTATE_ICON = '\uf2f1';
        public const char SCALE_ICON = '\uf31e';
        public const char MULTI_GIZMO_ICON = '\uf233';
        public const char RECT_SCALE_ICON = '\uf5cb';
        public const char COPY_ICON = '\uf0c5';
        public const char PASTE_ICON = '\uf0ea';
        public const char MINUS_ICON = '\uf056';
        public const char ADD_ICON = '\uf055';
        public const char EYE_DROPPER_ICON = '\uf1fb';
        public const char DELETE_ICON = '\uf2ed';
        public const char THUMB_RESIZE_ICON = '\uf00a';

        public const char DESELECT_ICON = ' ';
        public const char SELECT_ICON = ' ';

        
        public const char VIDEO_ICON = '\uf03d';
        public const char AUDIO_UP_ICON = '\uf028';
        public const char FLAG_CHECKERED = '\uf11e';

        public const char PATH_ICON = '\uf55b';
        public const char ANIM_PATH_ICON = '\uf018'; //Todo find a better icon.

        public const char PATH_MOVE = '\uf0b2';
        public const char PATH_DRAW = '\uf1fc';
        public const char ERASER = '\uf12d';
        public const char PATH_CONNECT = '\uf337';
        public const char PATH_CONNECT_AUTO = '\uf126';

        public const int ICON_SIZE = 18;

        static bool init = false;

        /// <summary>
        /// Checks if a key exists in the icon list.
        /// </summary>
        public static bool HasIcon(string icon) => Icons.ContainsKey(icon);

        //Icon list of cached textures.
        static void InitTextures() {
            if (init) return;

            Icons.Add("SAVE_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.Save).ID);
            Icons.Add("CHECKERBOARD", GLTexture2D.FromBitmap(Properties.Resources.CheckerBackground).ID);
            Icons.Add("IMG_EDIT_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.Edit).ID);
            Icons.Add("IMG_ALPHA_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.AlphaIcon).ID);
            Icons.Add("IMG_NOALPHA_BUTTON", GLTexture2D.FromBitmap(Properties.Resources.AlphaIconDisabled).ID);
            Icons.Add("TEXTURE", GLTexture2D.FromBitmap(Properties.Resources.Texture).ID);
            Icons.Add("UNDO", GLTexture2D.FromBitmap(Properties.Resources.Undo).ID);
            Icons.Add("REDO", GLTexture2D.FromBitmap(Properties.Resources.Redo).ID);
            Icons.Add("SCREENSHOT", GLTexture2D.FromBitmap(Properties.Resources.Screenshot).ID);
            Icons.Add("PLAY", GLTexture2D.FromBitmap(Properties.Resources.Play).ID);
            Icons.Add("STOP", GLTexture2D.FromBitmap(Properties.Resources.Pause).ID);
            Icons.Add("BLANK", GLTexture2D.CreateConstantColorTexture(4, 4, 0, 0, 0, 255).ID);
            Icons.Add("TRANS_GIZMO", GLTexture2D.FromBitmap(Properties.Resources.TranslateGizmo).ID);
            Icons.Add("ROTATE_GIZMO", GLTexture2D.FromBitmap(Properties.Resources.RotateGizmo).ID);
            Icons.Add("SCALE_GIZMO", GLTexture2D.FromBitmap(Properties.Resources.ScaleGizmo).ID);
            Icons.Add("NO_GIZMO", GLTexture2D.FromBitmap(Properties.Resources.NoGizmo).ID);
            Icons.Add("FOLDER", GLTexture2D.FromBitmap(Properties.Resources.Folder).ID);
            Icons.Add("COLLISION_TOGGLE_ON", GLTexture2D.FromBitmap(Properties.Resources.CollisionToggle).ID);
            Icons.Add("COLLISION_TOGGLE_OFF", GLTexture2D.FromBitmap(Properties.Resources.CollisionToggleOff).ID);
            Icons.Add("CAMERA_RECORD", GLTexture2D.FromBitmap(Properties.Resources.CameraRecord).ID);
            Icons.Add("SELECTION_TOOL", GLTexture2D.FromBitmap(Properties.Resources.SelectionTool).ID);
            Icons.Add("ERASER", GLTexture2D.FromBitmap(Properties.Resources.Eraser).ID);

            init = true;
        }

        /// <summary>
        /// Adds an icon from key and a bitmap byte array.
        /// Returns false if key already exists.
        /// </summary>
        public static bool TryAddIcon(string key, GLTexture image)
        {
            if (Icons.ContainsKey(key))
                return false;

            Icons.Add(key, image.ID);
            return true;
        }

        /// <summary>
        /// Adds an icon from key and a bitmap byte array.
        /// Returns false if key already exists.
        /// </summary>
        public static bool TryAddIcon(string key, byte[] image)
        {
            if (Icons.ContainsKey(key))
                return false;

            Icons.Add(key, GLTexture2D.FromBitmap(image).ID);
            return true;
        }

        /// <summary>
        /// Draws an icon with a custom color style.
        /// </summary>
        public static void DrawIcon(char icon)
        {
            Vector4 color = new Vector4(1.0f);

            if (icon == FOLDER_ICON) color = new Vector4(0.921f, 0.78f, 0.376f, 1.0f);
            if (icon == MESH_ICON) color = new Vector4(0.5f, 0.5f, 0.5f, 1.0f);
            if (icon == MODEL_ICON) color = new Vector4(0.4f, 0.4f, 0.4f, 1.0f);
            if (icon == SPARK_ICON) color = new Vector4(1, 1, 0.3f, 1.0f);
            if (icon == PARTICLE_ICON) color = new Vector4(0.4f, 0.8f, 1, 1.0f);
            
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            ImGui.Text(icon.ToString());
            ImGui.PopStyleColor();
        }

        /// <summary>
        /// Gets the texture ID from the given icon key.
        /// </summary>
        public static int GetTextureIcon(string key)
        {
            if (!init)
                InitTextures();

            if (Icons.ContainsKey(key))
                return Icons[key];

            return -1;
        }

        /// <summary>
        /// Loads an icon from a given key, bitmap, width and height.
        /// </summary>
        public static void LoadTextureFile(string key, byte[] bitmap, int width, int height)
        {
            if (!Icons.ContainsKey(key))
            {
                var texture = GLTexture2D.FromBitmap(bitmap);
                Icons.Add(key, texture.ID);
            }
        }


        /// <summary>
        /// Loads an icon from a given file path, width and height.
        /// When forced to update, the bitmap will be recreated and added onto the list.
        /// </summary>
        public static void LoadTextureFile(string filePath, int width, int height, bool forceUpdate = false)
        {
            if (forceUpdate && Icons.ContainsKey(filePath)) {
                OpenTK.Graphics.OpenGL.GL.DeleteTexture(Icons[filePath]);
                Icons.Remove(filePath);
            }

            if (!Icons.ContainsKey(filePath)) {
                var texture = GLTexture2D.FromBitmap(System.IO.File.ReadAllBytes(filePath));
                //Make the icon clear to see
                texture.MagFilter = OpenTK.Graphics.OpenGL.TextureMagFilter.Nearest;
                texture.MinFilter = OpenTK.Graphics.OpenGL.TextureMinFilter.Nearest;
                texture.Bind();
                texture.UpdateParameters();
                texture.Unbind();
                //Add the icon to the list
                Icons.Add(filePath, texture.ID);
            }
        }

        /// <summary>
        /// Loads an icon from a given key and generic texture.
        /// </summary>
        public static void DrawTexture(string key, Toolbox.Core.STGenericTexture texture) {
            DrawTexture(key, texture, ICON_SIZE, ICON_SIZE);
        }

        /// <summary>
        /// Loads an icon from a given key, texture id, width and height.
        /// </summary>
        public static void DrawTexture(string key, int id)
        {
            if (!Icons.ContainsKey(key))
                Icons.Add(key, id);

            DrawImage(Icons[key], ICON_SIZE, ICON_SIZE);
        }

        /// <summary>
        /// Loads an icon from a given key, generic texture, width and height.
        /// </summary>
        public static void DrawTexture(string key, Toolbox.Core.STGenericTexture texture, int width, int height)
        {
            if (!Icons.ContainsKey(key))
            {
                //Draw the icon into a smaller image to use as an icon with a unique ID.
                var iconID = IconRender.CreateTextureRender(texture, width, height);
                if (iconID == -1) { //Display a default icon instead
                    ImGui.Image((IntPtr)Icons["TEXTURE"], new Vector2(width, height));
                    return;
                }
                Icons.Add(key, iconID);
            }

            DrawImage(Icons[key], width, height);
        }

        /// <summary>
        /// Draws an icon into the GUI.
        /// </summary>
        public static void DrawIcon(string key, int size = ICON_SIZE) {
            if (!init)
                InitTextures();

            if (Icons.ContainsKey(key))
                DrawImage(Icons[key], size, size);
        }

        static void DrawImage(int id, int width, int height) {
            ImGui.Image((IntPtr)id, new System.Numerics.Vector2(width, height));
        }
    }
}
