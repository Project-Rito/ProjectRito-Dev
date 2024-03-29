using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL4;
using ImGuiNET;
using MapStudio.UI;
using GLFrameworkEngine;
using MapStudio.WindowsApi;
using Toolbox.Core;

namespace MapStudio
{
    public class MainWindow : GameWindow
    {
        public System.Drawing.Icon OriginalIcon;
        bool ForceFocused = true;

        ImGuiController _controller;
        GlobalSettings GlobalSettings { get; set; }

        List<string> RecentFiles = new List<string>();
        List<string> RecentProjects = new List<string>();

        private ProcessLoading ProcessLoading;

        private ObservableCollection<Workspace> Workspaces = new ObservableCollection<Workspace>();

        private JumpListHelper jumpList;
        private Program.Arguments _arguments;

        private IPluginConfig[] PluginSettingsUI;

        private bool UpdateDockLayout = true;

        public MainWindow(GraphicsMode gMode, Program.Arguments arguments) : base(1600, 900, gMode,
                                TranslationSource.GetText("RITO_EDITOR"),
                                GameWindowFlags.Default,
                                DisplayDevice.Default,
                                3, 2, GraphicsContextFlags.ForwardCompatible)
        {
            Title += $": {TranslationSource.GetText("OPENGL_VERSION")}: " + GL.GetString(StringName.Version);

            _arguments = arguments;

            ProcessLoading = new ProcessLoading();
            ProcessLoading.OnUpdated += delegate
            {
                this.Update();
            };

            Workspaces.CollectionChanged += delegate
            {
                UpdateDockLayout = true;
            };
        }

        public void Update()
        {
            var cont = OpenTK.Graphics.GraphicsContext.CurrentContext;
            cont.Update(this.WindowInfo);

            OnRenderFrame(new FrameEventArgs(0.0001f));
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            

            _controller = new ImGuiController(Width, Height);

            //Disable the docking buttons
            ImGui.GetStyle().WindowMenuButtonPosition = ImGuiDir.None;
            //Direct click on drag elements
            ImGui.GetIO().ConfigDragClickToInputText = true;

            //Enable docking support
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.DockingEnable;

            //Enable up/down key navigation
            ImGui.GetIO().ConfigFlags |= ImGuiConfigFlags.NavEnableKeyboard;
            //Only move via the title bar instead of the whole window
            ImGui.GetIO().ConfigWindowsMoveFromTitleBarOnly = true;
            //Init common render resources typically for debugging purposes
            RenderTools.Init();
            //Load outlier icons to cache
            IconManager.LoadTextureFile("Node", Properties.Resources.Object, 32, 32);
            IconManager.LoadTextureFile("Warning", Properties.Resources.Warning, 32, 32);

            //Load icons for map objects                                                                     // - This should be handled per-plugin.
            if (Directory.Exists($"{Runtime.ExecutableDir}/Images/MapObjects"))                            //   This is for consistency reasons, and also 
            {                                                                                                //   so that there aren't name conflicts.
                foreach (var imageFile in Directory.GetFiles($"{Runtime.ExecutableDir}/Images/MapObjects"))//   When migrated, it should use
                {                                                                                            //   PluginConfig.GetCachePath().
                    IconManager.LoadTextureFile(imageFile, 32, 32);
                }
            }

            //Load theme files
            ThemeHandler.Load();

            //Load global settings like language configuration
            GlobalSettings = GlobalSettings.Load();
            GlobalSettings.ReloadLanguage();
            GlobalSettings.ReloadTheme();
            Icon = GlobalSettings.ThemeIcon(OriginalIcon);

            //Load recent file lists
            RecentFileHandler.LoadRecentList($"{Runtime.ExecutableDir}/Recent.txt", RecentFiles);
            RecentFileHandler.LoadRecentList($"{Runtime.ExecutableDir}/RecentProjects.txt", RecentProjects);

            InitDock();

            jumpList = new JumpListHelper();

            if (jumpList != null)
                jumpList.ReloadRecentList(RecentProjects);

            foreach (var file in _arguments.FileInput)
                LoadFileFormat(file);

            PluginSettingsUI = Toolbox.Core.FileManager.GetPluginSettings();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // Tell ImGui of the new size
            _controller.WindowResized(Width, Height);
        }

        float font_scale = 1.0f;
        bool fullscreen = true;
        bool p_open = true;
        ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;

        private uint dock_id;
        private unsafe ImGuiWindowClass* window_class;

        Action ExecutedMenuAction = null;

        private unsafe void InitDock()
        {
            uint windowId = ImGui.GetID($"###window_main");

            var nativeConfig = ImGuiNative.ImGuiWindowClass_ImGuiWindowClass();
            (*nativeConfig).ClassId = windowId;
            (*nativeConfig).DockingAllowUnclassed = 0;
            this.window_class = nativeConfig;
        }

        bool renderingFrame = false;
        bool executingAction = false;


        System.Numerics.Vector2 loadingCenter;
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            if (renderingFrame) return;

            //When a menu is clicked, it is executed in the render loop normally
            //Instead we may need to execute ouside the loop so we can update the rendering during the action
            //This is used for the progress bar to update properly.s
            if (ExecutedMenuAction != null && !executingAction)
            {
                executingAction = true;
                ExecutedMenuAction.Invoke();
                ExecutedMenuAction = null;
                executingAction = false;
            }

            base.OnRenderFrame(e);

            if (!this.Focused && !ForceFocused && !ProcessLoading.IsLoading &&
                !(GLContext.ActiveContext != null && GLContext.ActiveContext.UpdateViewport))
            {
                System.Threading.Thread.Sleep(1);
                return;
            }

            //Only force the focus once
            if (ForceFocused)
                ForceFocused = false;

            renderingFrame = true;

            _controller.Update(this, (float)e.Time);

            ImGuiWindowFlags window_flags = ImGuiWindowFlags.NoDocking;

            if (fullscreen)
            {
                ImGuiViewportPtr viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.WorkPos);
                ImGui.SetNextWindowSize(viewport.WorkSize);
                ImGui.SetNextWindowViewport(viewport.ID);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 0.0f);
                ImGui.PushStyleVar(ImGuiStyleVar.WindowBorderSize, 0.0f);
                window_flags |= ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoMove;
                window_flags |= ImGuiWindowFlags.NoBringToFrontOnFocus | ImGuiWindowFlags.NoNavFocus;
            }

            if ((dockspace_flags & ImGuiDockNodeFlags.PassthruCentralNode) != 0)
                window_flags |= ImGuiWindowFlags.NoBackground;

            //Set the adjustable global font scale
            ImGui.GetIO().FontGlobalScale = font_scale;

            //Set custom cursor if one is set
            if (MouseEventInfo.MouseCursor == MouseEventInfo.Cursor.EyeDropper)
            {
                //Hide the cursor from SDL
                this.CursorVisible = false;
                //Enable drawing custom cursor
                ImGui.GetIO().MouseDrawCursor = true;
                //Draw the cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
            } //Check if a cursor is currently custom
            if (MouseEventInfo.MouseCursor == MouseEventInfo.Cursor.Eraser)
            {
                //Hide the cursor from SDL
                this.CursorVisible = false;
                //Enable drawing custom cursor
                ImGui.GetIO().MouseDrawCursor = true;
                //Draw the cursor
                ImGui.SetMouseCursor(ImGuiMouseCursor.None);
            }
            else if (MouseEventInfo.MouseCursor == MouseEventInfo.Cursor.None)
            {
                // Hide cursor from SDL
                this.CursorVisible = false;
                // Make sure ImGui isn't doing anything
                ImGui.GetIO().MouseDrawCursor = false;
            }
            else if (MouseEventInfo.MouseCursor == MouseEventInfo.Cursor.Default)
            {
                //Enable the cursor and set back to defaults
                this.CursorVisible = true;
                ImGui.GetIO().MouseDrawCursor = false;
                ImGui.SetMouseCursor(ImGuiMouseCursor.Arrow);
            }

            ImGui.Begin("WindowSpace", ref p_open, window_flags);
            ImGui.PopStyleVar(2);

            LoadFileMenu();

            if (Workspaces.Count == 0)
                LoadStartScreen();

            LoadWorkspaces();

            if (ProcessLoading.IsLoading)
            {

                ImGui.SetNextWindowPos(loadingCenter);
                var flags = ImGuiWindowFlags.AlwaysAutoResize;

                if (ImGui.Begin("Loading", ref ProcessLoading.IsLoading, flags))
                {
                    float progress = (float)ProcessLoading.ProcessAmount / ProcessLoading.ProcessTotal;
                    ImGui.ProgressBar(progress, new System.Numerics.Vector2(300, 20));

                    ImGuiHelper.DrawCenteredText($"{ProcessLoading.ProcessName}");
                }

                loadingCenter = ImGui.GetMainViewport().GetCenter();
                loadingCenter.X -= ImGui.GetWindowWidth() / 2;
                loadingCenter.Y -= ImGui.GetWindowHeight() / 2;
                
                ImGui.End();
            }

            DialogHandler.RenderActiveWindows();

            ImGui.End();

            GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
            GL.Viewport(0, 0, Width, Height);

            _controller.Render();

            SwapBuffers();

            renderingFrame = false;
        }


        private unsafe void LoadWorkspaces()
        {
            //Window spawn sizes
            var contentSize = ImGui.GetWindowSize();

            SetupDocks();

            List<Workspace> removedWindows = new List<Workspace>();
            for (int i = 0; i < Workspaces.Count; i++)
            {
                if (!Workspaces[i].Opened)
                {
                    removedWindows.Add(Workspaces[i]);
                }
            }

            if (removedWindows.Count > 0)
                RemoveWorkspaces(removedWindows);

            for (int i = 0; i < Workspaces.Count; i++)
                Workspaces[i].InitWindowDocker(i);

            for (int i = 0; i < Workspaces.Count; i++)
            {
                if (!Workspaces[i].Opened)
                    continue;

                var workspace = Workspaces[i];

                uint dockspaceId = ImGui.GetID($"###DOCKSPACE{i}");
                workspace.DockID = dockspaceId;

                //Constrain the docked windows within a workspace using window classes
                ImGui.SetNextWindowClass(window_class);
                //Set the workspace size on load
                ImGui.SetNextWindowSize(contentSize, ImGuiCond.Once);

                workspace.PushStyling();
                bool visible = ImGui.Begin(workspace.Name + $"###window{dockspaceId}", ref workspace.Opened);
                workspace.PopStyling();

                if (ImGui.DockBuilderGetNode(dockspaceId).NativePtr == null || workspace.UpdateDockLayout)
                    workspace.ReloadDockLayout(dockspaceId, (int)workspace.DockID);

                if (visible && ImGui.IsWindowFocused())
                    Workspace.UpdateActive(workspace);

                ImGui.DockSpace(dockspaceId, new System.Numerics.Vector2(0, 0),
                    ImGuiDockNodeFlags.CentralNode, workspace.window_class);

                if (visible)
                    workspace.RenderWindows();

                if (!workspace.Opened)
                {
                    int result = TinyFileDialog.MessageBoxInfoYesNo("Are you sure you want to remove the workspace? You may lose unsaved progress!");
                    if (result == 0)
                        workspace.Opened = true;
                }
                ImGui.End();
            }
        }

        private void RemoveWorkspaces(List<Workspace> workspaces)
        {
            foreach (var workspace in workspaces)
            {

                Workspaces.Remove(workspace);
                workspace.Dispose();
            }
            if (Workspaces.Count == 0)
                Workspace.ActiveWorkspace = null;
        }

        private void SetupDocks()
        {
            var dock_id = ImGui.GetID("##DockspaceRoot");

            SetupParentDock(dock_id, Workspaces.Select(x => (DockWindow)x).ToList());
        }

        private unsafe void SetupParentDock(uint parentDockID, List<DockWindow> children)
        {
            //Check if the dock has been created or needs to be updated
            if (ImGui.DockBuilderGetNode(parentDockID).NativePtr == null || UpdateDockLayout)
            {
                ImGuiDockNodeFlags dockspace_flags = ImGuiDockNodeFlags.None;
                uint main_id = parentDockID;

                ImGui.DockBuilderRemoveNode(parentDockID); // Clear out existing layout
                ImGui.DockBuilderAddNode(parentDockID, dockspace_flags); // Add empty node

                for (int i = 0; i < children.Count; i++)
                {
                    DockWindow workspace = children[i];
                    ImGui.DockBuilderDockWindow($"{workspace.Name}###window{ImGui.GetID($"###DOCKSPACE{i}")}", main_id);
                }

                ImGui.DockBuilderFinish(parentDockID);
                UpdateDockLayout = false;
            }

            unsafe
            {
                //Create an inital dock space for docking workspaces.
                ImGui.DockSpace(parentDockID, new System.Numerics.Vector2(0.0f, 0.0f), 0, window_class);
            }
        }

        private void LoadStartScreen()
        {
        }

        bool showStyleEditor = false;
        bool showDemoWindow = false;

        private void LoadFileMenu()
        {
            bool canSave = Workspace.ActiveWorkspace != null;
            bool canRename = canSave && Workspace.ActiveWorkspace.Name != TranslationSource.GetText("NEW_PROJECT");

            if (ImGui.BeginMainMenuBar())
            {
                ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, new System.Numerics.Vector2(8, 6));
                ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new System.Numerics.Vector2(8, 4));
                ImGui.PushStyleColor(ImGuiCol.Separator, new System.Numerics.Vector4(0.4f, 0.4f, 0.4f, 1.0f));

                if (ImGui.BeginMenu($"{TranslationSource.GetText("MENU_FILE")}##MAIN00"))
                {
                    if (ImGui.MenuItem($"  {IconManager.NEW_FILE_ICON}    {TranslationSource.GetText("MENU_NEW")}##MAIN01"))
                    {
                        ExecutedMenuAction = CreateNewProject;
                    }
                    if (ImGui.MenuItem($"  {IconManager.OPEN_ICON}    {TranslationSource.GetText("MENU_OPEN")}##MAIN01", "Ctrl+O", false))
                    {
                        ExecutedMenuAction = OpenFileWithDialog;
                    }
                    if (ImGui.BeginMenu($"       {TranslationSource.GetText("MENU_RECENT")}##MAIN09"))
                    {
                        foreach (var file in RecentFiles)
                        {
                            if (ImGui.Selectable(file))
                            {
                                ExecutedMenuAction = () => { LoadFileFormat(file); };
                            }
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem($"  {IconManager.SAVE_ICON}    {TranslationSource.GetText("MENU_SAVE_EDITOR")}##MAIN08", "", false, canSave))
                    {
                        Workspace.ActiveWorkspace.SaveFileData();
                    }
                    if (ImGui.MenuItem($"  {IconManager.SAVE_ICON}    {TranslationSource.GetText("MENU_SAVE_EDITOR_AS")}##MAIN08", "", false, canSave))
                    {
                        Workspace.ActiveWorkspace.SaveFileWithDialog();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem($"  {IconManager.PROJECT_ICON}    {TranslationSource.GetText("MENU_OPEN_PROJECT")}##MAIN02"))
                    {
                        var projectList = new ProjectList();
                        DialogHandler.Show("", () =>
                        {
                            projectList.Render();
                        }, (e) =>
                        {
                            if (e)
                                LoadFileFormat($"{projectList.SelectedProject}/Project.json");
                        });
                    }
                    if (ImGui.BeginMenu($"       {TranslationSource.GetText("MENU_RECENT_PROJECTS")}##MAIN03"))
                    {
                        foreach (var project in RecentProjects)
                        {
                            DisplayRecentProject(project);
                        }
                        ImGui.EndMenu();
                    }
                    if (ImGui.MenuItem($"       {TranslationSource.GetText("MENU_SAVE_PROJECT")}##MAIN04", "Ctrl+S", false, canSave))
                    {
                        SaveProject();
                    }
                    if (ImGui.MenuItem($"       {TranslationSource.GetText("MENU_SAVE_PROJECT_AS")}##MAIN05", "Ctrl+Alt+S", false, canSave))
                    {
                        SaveProjectWithDialog();
                    }
                    if (ImGui.MenuItem($"       {TranslationSource.GetText("MENU_RENAME_PROJECT")}##MAIN06", "", false, canRename))
                    {
                        RenameProjectWithDialog();
                    }
                    ImGui.Separator();
                    if (ImGui.MenuItem($"       {TranslationSource.GetText("MENU_CLEAR_WORKSPACE")}##MAIN06"))
                    {
                        this.ClearWorkspace();
                    }
                    if (ImGui.MenuItem($"       {TranslationSource.GetText("MENU_EXIT")}##MAIN07"))
                    {
                        this.Exit();
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu($"{TranslationSource.GetText("EDIT")}"))
                {
                    var workspace = Workspace.ActiveWorkspace;
                    if (workspace != null)
                    {
                        foreach (var menu in workspace.GetEditMenus())
                            ImGuiHelper.LoadMenuItem(menu);
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu($"{TranslationSource.GetText("MENU_SETTINGS")}##MAIN11s"))
                {
                    if (PluginSettingsUI != null)
                    {
                        foreach (var plugin in PluginSettingsUI)
                            plugin.DrawUI();
                    }

                    ImguiCustomWidgets.PathSelector(TranslationSource.GetText("PROJECT_SAVE_DIRECTORY"), ref GlobalSettings.Program.ProjectDirectory);

                    var language = TranslationSource.LanguageKey;
                    if (ImGui.BeginCombo($"{TranslationSource.GetText("LANGUAGE")}", language))
                    {
                        foreach (var lang in TranslationSource.GetLanguages())
                        {
                            string name = Path.GetFileNameWithoutExtension(lang);
                            bool isSelected = name == language;
                            if (ImGui.Selectable(name, isSelected))
                            {
                                TranslationSource.Instance.Update(name);
                                GlobalSettings.Program.Language = name;
                                GlobalSettings.Save();

                                if (Workspace.ActiveWorkspace != null)
                                {
                                    Workspace.ActiveWorkspace.UpdateDockLayout = true;
                                    Workspace.ActiveWorkspace.OnLanguageChanged();
                                }
                            }

                            if (isSelected)
                                ImGui.SetItemDefaultFocus();
                        }

                        ImGui.EndCombo();
                    }

                    string currentThemeName = GlobalSettings.Program.Theme.ToString();
                    if (ImGui.BeginCombo(TranslationSource.GetText("THEME"), TranslationSource.GetText(currentThemeName)))
                    {
                        foreach (var colorTheme in ThemeHandler.Themes)
                        {
                            string themeName = colorTheme.Name;
                            string name = TranslationSource.GetText(themeName);
                            bool selected = themeName == currentThemeName;
                            if (ImGui.Selectable(name, selected))
                            {
                                //Set the current theme instance
                                ThemeHandler.UpdateTheme(colorTheme);
                                Icon = ThemeHandler.ThemeIcon(OriginalIcon, colorTheme);

                                GlobalSettings.Program.Theme = colorTheme.Name;
                                GlobalSettings.Save();
                            }
                            if (selected)
                                ImGui.SetItemDefaultFocus();
                        }
                        ImGui.EndCombo();
                    }

                    bool updateSettings = false;
                    if (ImGui.BeginMenu($"{TranslationSource.GetText("BACKGROUND")}##vmenu00"))
                    {
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DISPLAY"), ref DrawableBackground.Display);
                        updateSettings |= ImGui.ColorEdit3(TranslationSource.GetText("COLOR_TOP"), ref DrawableBackground.BackgroundTop);
                        updateSettings |= ImGui.ColorEdit3(TranslationSource.GetText("COLOR_BOTTOM"), ref DrawableBackground.BackgroundBottom);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu($"{TranslationSource.GetText("GRID")}##vmenu01"))
                    {
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DISPLAY"), ref DrawableGridFloor.Display);
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("SOLID"), ref DrawableInfiniteFloor.IsSolid);
                        updateSettings |= ImGui.ColorEdit4(TranslationSource.GetText("COLOR"), ref DrawableGridFloor.GridColor);
                        updateSettings |= ImGui.InputInt(TranslationSource.GetText("GRID_COUNT"), ref Runtime.GridSettings.CellAmount);
                        updateSettings |= ImGui.InputFloat(TranslationSource.GetText("GRID_SIZE"), ref Runtime.GridSettings.CellSize);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu($"{TranslationSource.GetText("BONES")}##vmenu02"))
                    {
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DISPLAY"), ref Runtime.DisplayBones);
                        updateSettings |= ImGui.InputFloat(TranslationSource.GetText("POINT_SIZE"), ref Runtime.BonePointSize);
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu($"{TranslationSource.GetText("SHADOWS")}##vmenu03"))
                    {
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DISPLAY"), ref ShadowMainRenderer.Display);
                        updateSettings |= ImGui.InputFloat(TranslationSource.GetText("SCALE"), ref ShadowBox.UnitScale);
                        updateSettings |= ImGui.InputFloat(TranslationSource.GetText("DISTANCE"), ref ShadowBox.Distance);
#if DEBUG
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DEBUG"), ref ShadowMainRenderer.DEBUG_QUAD);
#endif
                        ImGui.EndMenu();
                    }
                    if (ImGui.BeginMenu($"{TranslationSource.GetText("CULLING")}##vmenu04"))
                    {
#if DEBUG
                        updateSettings |= ImGui.Checkbox(TranslationSource.GetText("DEBUG"), ref Runtime.RenderBoundingBoxes);
#endif
                        ImGui.EndMenu();
                    }

                    int newObjectLoc = (int)GlobalSettings.Editor.NewObjectLocation;
                    updateSettings |= ImGui.SliderInt($"{TranslationSource.GetText("NEW_OBJECT_LOCATION")}", ref newObjectLoc, 0, Enum.GetValues(typeof(GlobalSettings.NewObjectLocation)).Length - 1, GlobalSettings.PlaceObjectLocationNames[newObjectLoc]);
                    GlobalSettings.Editor.NewObjectLocation = (GlobalSettings.NewObjectLocation)newObjectLoc;

                    ImGui.Checkbox($"{TranslationSource.GetText("CREATE_NEW_WORKSPACE")}", ref createNewWorkspace);

                    if (updateSettings && GLContext.ActiveContext != null)
                    {
                        //Reload existing set values then save
                        GlobalSettings.LoadCurrentSettings();
                        GlobalSettings.Save();
                        GLContext.ActiveContext.UpdateViewport = true;
                    }
                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu(TranslationSource.GetText("WINDOWS")))
                {
                    //Docked windows
                    if (Workspace.ActiveWorkspace != null)
                    {
                        foreach (var window in Workspace.ActiveWorkspace.DockWindows)
                        {
                            if (ImGui.MenuItem(TranslationSource.GetText(window.Name), "", window.Opened))
                            {
                                window.Opened = window.Opened ? false : true;
                            }
                        }
                    }

                    if (ImGui.MenuItem($"{TranslationSource.GetText("STYLE_EDITOR")}", "", showStyleEditor))
                    {
                        showStyleEditor = showStyleEditor ? false : true;
                    }

#if DEBUG
                    if (ImGui.MenuItem($"Demo Window", "", showDemoWindow))
                    {
                        showDemoWindow = showDemoWindow ? false : true;
                    }
#endif

                    if (Workspace.ActiveWorkspace != null)
                    {
                        foreach (var window in Workspace.ActiveWorkspace.Windows)
                        {
                            if (ImGui.MenuItem(window.Name, "", window.Opened))
                            {
                                window.Opened = window.Opened ? false : true;
                            }
                        }
                    }

                    ImGui.EndMenu();
                }
                if (ImGui.BeginMenu(TranslationSource.GetText("PLUGINS")))
                {
                    foreach (var plugin in PluginManager.LoadPlugins().GroupBy(x => x.PluginHandler.Name).Select(x => x.First())) // Not sure why there are duplicate Toolbox.Cores... but....
                    {
                        ImGui.Text(plugin.PluginHandler.Name);
                    }
                    ImGui.EndMenu();
                }

                if (Workspace.ActiveWorkspace != null)
                    Workspace.ActiveWorkspace.DrawMenu();

                ImGui.PopStyleVar(2);
                ImGui.PopStyleColor(1);

                if (showStyleEditor)
                {
                    if (ImGui.Begin("Style Editor", ref showStyleEditor))
                    {
                        ImGui.ShowStyleEditor();
                        ImGui.End();
                    }
                }
                if (showDemoWindow)
                {
                    ImGui.ShowDemoWindow();
                }

                //Display FPS at right side of screen
                float width = ImGui.GetWindowWidth();
                float framerate = ImGui.GetIO().Framerate;

                ImGui.SetCursorPosX(width - 6.25f * ImGui.GetFontSize());
                ImGui.Text($"({framerate:0.#} FPS)");
                ImGui.EndMainMenuBar();
            }
        }

        private void CreateNewProject()
        {
            // Todo - figure out editor stuff. Creating a new model editor and assigning it to workspace.ActiveEditor
            // seems to fix problems, but that's probably not the best solution.
            ProcessLoading.IsLoading = true;

            var workspace = new Workspace(GlobalSettings, Workspaces.Count, this);
            workspace.CreateNewProject();
            Workspaces.Add(workspace);

            ProcessLoading.IsLoading = false;
            ForceFocused = true;
        }

        private void DisplayRecentProject(string folder)
        {
            string thumbFile = $"{folder}/Thumbnail.png";
            string projectFile = $"{folder}/Project.json";
            string projectName = new DirectoryInfo(folder).Name;

            if (!File.Exists(projectFile))
                return;

            var icon = IconManager.GetTextureIcon("BLANK");
            if (File.Exists(thumbFile))
            {
                IconManager.LoadTextureFile(thumbFile, 64, 64);
                icon = IconManager.GetTextureIcon(thumbFile);
            }

            //Make the whole menu item region selectable
            var width = ImGui.CalcItemWidth();
            var size = new System.Numerics.Vector2(width, 64);

            var pos = ImGui.GetCursorPos();
            bool isSelected = ImGui.Selectable($"##{folder}", false, ImGuiSelectableFlags.None, size);
            ImGui.SetCursorPos(pos);

            //Load an icon preview of the project
            ImGui.AlignTextToFramePadding();
            ImGui.Image((IntPtr)icon, new System.Numerics.Vector2(64, 64));
            ImGui.SameLine();
            //Project name
            ImGui.SameLine();
            ImGui.Text(projectName);
            //Load file when clicked on
            if (isSelected)
                ExecutedMenuAction = () => { LoadFileFormat(projectFile); };
        }

        private void OpenFileWithDialog()
        {
            ImguiFileDialog ofd = new ImguiFileDialog();
            if (ofd.ShowDialog("OPEN_FILE", true))
            {
                foreach (var file in ofd.FilePaths)
                    LoadFileFormat(file);
            }
        }

        private void SaveProject()
        {
            if (Workspace.ActiveWorkspace.Name == TranslationSource.GetText("NEW_PROJECT"))
            {
                SaveProjectWithDialog();
                return;
            }

            var workspace = Workspace.ActiveWorkspace;
            var settings = GlobalSettings.Current;

            string dir = $"{settings.Program.ProjectDirectory}/{workspace.Name}";

            workspace.SaveProject(dir);

            RecentFileHandler.SaveRecentFile(dir, "RecentProjects.txt", this.RecentProjects);
        }

        private void SaveProjectWithDialog()
        {
            var workspace = Workspace.ActiveWorkspace;
            var settings = GlobalSettings.Current;

            string oldProjectDir = $"{settings.Program.ProjectDirectory}/{workspace.Name}";

            ProjectSaveDialog projectDialog = new ProjectSaveDialog(workspace.Name);
            DialogHandler.Show(TranslationSource.GetText("MENU_SAVE_PROJECT"), () =>
            {
                projectDialog.LoadUI();
            }, (confirmed) =>
            {
                if (!confirmed)
                    return;

                workspace.UpdateProjectAssetPaths(oldProjectDir, projectDialog.GetProjectDirectory());
                workspace.SaveProject(projectDialog.GetProjectDirectory());
                RecentFileHandler.SaveRecentFile(projectDialog.GetProjectDirectory(), "RecentProjects.txt", this.RecentProjects);
            });
        }

        private void RenameProjectWithDialog()
        {
            var workspace = Workspace.ActiveWorkspace;
            var settings = GlobalSettings.Current;

            string oldProjectDir = $"{settings.Program.ProjectDirectory}/{workspace.Name}";

            ProjectSaveDialog projectDialog = new ProjectSaveDialog(workspace.Name);
            DialogHandler.Show(TranslationSource.GetText("RENAME_PROJECT"), () =>
            {
                projectDialog.LoadUI();
            }, (confirmed) =>
            {
                if (!confirmed)
                    return;

                workspace.Name = projectDialog.GetProjectDirectory().Split("/").Last();
                workspace.UpdateProjectAssetPaths(oldProjectDir, projectDialog.GetProjectDirectory());
                Directory.Move(oldProjectDir, projectDialog.GetProjectDirectory());
                RecentFileHandler.SaveRecentFile(projectDialog.GetProjectDirectory(), "RecentProjects.txt", this.RecentProjects);
            });
        }

        bool createNewWorkspace = true;
        private void LoadFileFormat(string filePath)
        {
            this.ForceFocused = true;

            //Check if the format is supported in the current editors.
            string ext = Path.GetExtension(filePath);
            bool isProject = filePath.EndsWith("Project.json");
            ProcessLoading.IsLoading = true;

            //Load asset based format
            if (!isProject)
            {
                if (createNewWorkspace)
                {
                    Workspace workspace = new Workspace(this.GlobalSettings, Workspaces.Count, this);
                    var editor = workspace.LoadFileFormat(filePath);
                    if (editor == null)
                    {
                        ProcessLoading.IsLoading = false;
                        ForceFocused = true;
                        return;
                    }
                    Workspaces.Add(workspace);

                    RecentFileHandler.SaveRecentFile(filePath, "Recent.txt", this.RecentFiles);
                }
            }
            else
            {
                //Load project format
                var workspace = new Workspace(GlobalSettings, Workspaces.Count, this);
                bool initialized = workspace.LoadProjectFile(filePath);
                if (!initialized)
                    return;

                Workspaces.Add(workspace);
            }

            ProcessLoading.IsLoading = false;
            ForceFocused = true;
        }

        private void ClearWorkspace()
        {
            foreach (var workspace in Workspaces)
                workspace.Dispose();

            Workspaces.Clear();
            GC.Collect();
        }

        protected override void OnFileDrop(FileDropEventArgs e)
        {
            base.OnFileDrop(e);

            LoadFileFormat(e.FileName);
        }

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            base.OnKeyPress(e);
            _controller.PressChar(e.KeyChar);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);

            //Make sure the key cannot be repeated when held down
            if (!e.IsRepeat)
            {
                if (Keyboard.GetState().IsKeyDown(Key.ControlLeft) && e.Key == Key.S && Workspace.ActiveWorkspace != null)
                {
                    if (Keyboard.GetState().IsKeyDown(Key.AltLeft))
                        SaveProjectWithDialog();
                    else
                        SaveProject();
                }
            }

            Workspace.ActiveWorkspace?.OnKeyDown(InputState.CreateKeyState(), e.IsRepeat);
        }

        protected override void OnKeyUp(KeyboardKeyEventArgs e)
        {
            base.OnKeyUp(e);

            Workspace.ActiveWorkspace?.OnKeyUp(InputState.CreateKeyState());
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);

            Workspace.ActiveWorkspace?.OnMouseDown();
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);

            Workspace.ActiveWorkspace?.OnMouseUp();
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);

            Workspace.ActiveWorkspace?.OnMouseMove();
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);

            Workspace.ActiveWorkspace?.OnMouseWheel();
        }

        protected override void OnFocusedChanged(EventArgs e)
        {
            base.OnFocusedChanged(e);

            if (Workspace.ActiveWorkspace != null)
                Workspace.ActiveWorkspace.OnApplicationEnter();
        }

        public override void Exit()
        {
            base.Exit();
        }
    }
}
