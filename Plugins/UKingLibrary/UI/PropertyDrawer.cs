using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using MapStudio.UI;
using System.Numerics;

namespace UKingLibrary.UI
{
    public class PropertyDrawer
    {
        static bool isUpdating = false;

        public static void Draw(MapObject mapObject, IDictionary<string, dynamic> values, PropertyChangedCallback callback = null)
        {
            IDictionary<string, dynamic> parameters = null;
            if (values.ContainsKey("!Parameters"))
                parameters = (IDictionary<string, dynamic>)values["!Parameters"];

            float width = ImGui.GetWindowWidth();

            if (ImGui.CollapsingHeader(TranslationSource.GetText("OBJ"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button($"{TranslationSource.GetText("EDIT")}##obj", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show($"{TranslationSource.GetText("PROPERTY_WINDOW")}", () => PropertiesDialog(values), null);

                ImGui.Columns(2);
                LoadProperties(values, callback);
                ImGui.Columns(1);
            }

            if (parameters != null && ImGui.CollapsingHeader(TranslationSource.GetText("PARAMETERS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button($"{TranslationSource.GetText("EDIT")}##param", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show($"{TranslationSource.GetText("PROPERTY_WINDOW")}", () => PropertiesDialog(parameters), null);

                ImGui.Columns(2);
                LoadProperties(parameters, callback);
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("INCOMING_LINKS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                foreach (var link in mapObject.SourceLinks)
                    DrawLinkItem(link);
                ImGui.Columns(1);
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("OUTGOING_LINKS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button($"{TranslationSource.GetText("EDIT")}##link", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show($"{TranslationSource.GetText("LINK_WINDOW")}", () => LinksDialog(mapObject.DestLinks, mapObject), null);

                ImGui.Columns(2);
                foreach (var link in mapObject.DestLinks)
                    DrawLinkItem(link);
                ImGui.Columns(1);
            }

            if (TranslationSource.HasKey($"ACTOR_DOCS {mapObject.Name}"))
                if (ImGui.CollapsingHeader(TranslationSource.GetText("ACTOR_DOCUMENTATION"), ImGuiTreeNodeFlags.DefaultOpen))
                    ImGui.TextWrapped(TranslationSource.GetText($"ACTOR_DOCS {mapObject.Name}"));

            if (TranslationSource.HasKey($"ID_DOCS {mapObject.HashId}"))
                if (ImGui.CollapsingHeader(TranslationSource.GetText("INSTANCE_DOCUMENTATION"), ImGuiTreeNodeFlags.DefaultOpen))
                    ImGui.TextWrapped(TranslationSource.GetText($"ID_DOCS {mapObject.HashId}"));
        }

        static void DrawLinkItem(MapObject.LinkInstance link)
        {
            string def = link.Properties["DefinitionName"].Value;
            uint dest = (uint)link.Properties["DestUnitHashId"].Value;
            bool selected = ImGui.Selectable(def, false, ImGuiSelectableFlags.SpanAllColumns);

            if (ImGui.IsItemHovered() && ImGui.IsMouseDoubleClicked(0))
            {
                //Deselect all objects in the scene
                var context = GLFrameworkEngine.GLContext.ActiveContext;
                context.Scene.DeselectAll(context);
                //Select the clicked link object
                link.Object.Render.IsSelected = true;
            }

            ImGui.NextColumn();
            ImGui.Text($"{link.Object.Properties["UnitConfigName"].Value}");
            ImGui.NextColumn();
        }

        // A dialog to add/remove links
        static void LinksDialog(List<MapObject.LinkInstance> links, MapObject mapObject)
        {
            if (ImGui.CollapsingHeader(TranslationSource.GetText("ADD_LINK"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushItemWidth(100);
                ImGui.Combo("##addlinktype", ref selectedLinkType, LinkTypes, LinkTypes.Length, 100);
                ImGui.PopItemWidth();

                ImGui.SameLine();
                ImGui.InputInt($"##addlinkid", ref addLinkHashId, 0, 0, ImGuiInputTextFlags.CharsDecimal);

                ImGui.SameLine();
                ImGui.PushItemWidth(100);
                if (ImGui.Button(TranslationSource.GetText("ADD")))
                {
                    MapObject.LinkInstance link = new MapObject.LinkInstance(
                        (uint)addLinkHashId, 
                        new Dictionary<string, dynamic>()
                        {
                            { "!Parameters", new Dictionary<string, dynamic>() },
                            { "DefinitionName", new MapData.Property<dynamic>(LinkTypes[selectedLinkType]) },
                            { "DestUnitHashId", new MapData.Property<dynamic>((uint)addLinkHashId) }
                        }
                    );
                    link.Object.SourceLinks.Add(new MapObject.LinkInstance()
                    {
                        Properties = link.Properties,
                        Object = mapObject
                    });
                    mapObject.Render.DestObjectLinks.Add(link.Object.Render);
                    links.Add(link);
                }
            }

            if (ImGui.CollapsingHeader(TranslationSource.GetText("LINKS"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                float window_width = ImGui.GetWindowWidth();
                ImGui.Columns(3);
                foreach (MapObject.LinkInstance link in links)
                {
                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    int editSelectedLinkType = Array.IndexOf(LinkTypes, (string)link.Properties["DefinitionName"].Value);
                    ImGui.Combo($"##editlinktype##{link.Properties["DefinitionName"].Value}{link.Properties["DestUnitHashId"].Value}", ref editSelectedLinkType, LinkTypes, LinkTypes.Length, 100);
                    link.Properties["DefinitionName"] = new MapData.Property<dynamic>(LinkTypes[editSelectedLinkType]);
                    ImGui.PopItemWidth();

                    ImGui.NextColumn();

                    int editLinkHashId = (int)link.Properties["DestUnitHashId"].Value;
                    ImGui.InputInt($"##editlinkid##{link.Properties["DefinitionName"].Value}{link.Properties["DestUnitHashId"].Value}", ref editLinkHashId, 0, 0, ImGuiInputTextFlags.CharsDecimal);
                    if (editLinkHashId != (int)link.Properties["DestUnitHashId"].Value)
                    {
                        link.Object.SourceLinks.RemoveAll(x => x.Properties["DestUnitHashId"].Value == mapObject.HashId);
                        mapObject.Render.DestObjectLinks.Remove(link.Object.Render);
                        link.Object = ((UKingEditor)Workspace.ActiveWorkspace.ActiveEditor).ActiveMapLoader.MapObjectByHashId((uint)editLinkHashId);
                        link.Object.SourceLinks.Add(new MapObject.LinkInstance()
                        {
                            Properties = link.Properties,
                            Object = mapObject
                        });
                        mapObject.Render.DestObjectLinks.Add(link.Object.Render);
                    }
                    link.Properties["DestUnitHashId"] = new MapData.Property<dynamic>((uint)editLinkHashId);

                    ImGui.NextColumn();

                    ImGui.PushItemWidth(80);

                    if (ImGui.Button($"Remove##{link.Properties["DefinitionName"].Value}{link.Properties["DestUnitHashId"].Value}"))
                        removedLinks.Add(link);

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                }
                ImGui.Columns(1);

                foreach (var link in removedLinks)
                {
                    link.Object.SourceLinks.RemoveAll(x => x.Properties["DestUnitHashId"].Value == mapObject.HashId);
                    mapObject.Render.DestObjectLinks.Remove(link.Object.Render);
                    links.Remove(link);
                }

                if (removedLinks.Count > 0)
                    removedLinks.Clear();
            }
        }

        static List<MapObject.LinkInstance> removedLinks = new List<MapObject.LinkInstance>();

        static int addLinkHashId = 0;

        static int selectedLinkType = 0;

        static string[] LinkTypes = new string[] // Loll this list goes on foreverrrrr
        {
            "-AxisX", "-AxisY", "-AxisZ", "AreaCol", "AxisX", "AxisY", "AxisZ", "BAndSCs", "BAndSLimitAngYCs", "BasicSig", "BasicSigOnOnly", "ChangeAtnSig", "CogWheelCs", "CopyWaitRevival", "Create", "DeadUp", "Delete", "DemoMember", "FixedCs", "ForSale", "ForbidAttention", "Freeze", "GimmickSuccess", "HingeCs", "LifeZero", "LimitHingeCs", "ModelBind", "MtxCopyCreate", "OffWaitRevival", "PhysSystemGroup", "PlacementLOD", "PulleyCs", "RackAndPinionCs", "Recreate", "Reference", "Remains", "SensorBlind", "SliderCs", "Stable", "StackLink", "SyncLink", "VelocityControl"
        };

        // A dialog to add/remove properties.
        static void PropertiesDialog(IDictionary<string, dynamic> properties)
        {
            if (isUpdating)
                return;

            if (ImGui.CollapsingHeader(TranslationSource.GetText("ADD_PROPERTY"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushItemWidth(100);
                ImGui.Combo("##addproptype", ref selectedPropertyType, PropertyTypes, PropertyTypes.Length, 100);
                ImGui.PopItemWidth();

                ImGui.SameLine();
                ImGui.InputText($"##addpropname", ref addPropertyName, 0x100);

                ImGui.SameLine();
                ImGui.PushItemWidth(100);
                if (ImGui.Button(TranslationSource.GetText("ADD")))
                {
                    if (!string.IsNullOrEmpty(addPropertyName))
                    {
                        isUpdating = true;

                        //Remove the existing property if exist. User may want to update the data type.
                        if (properties.ContainsKey(addPropertyName))
                            properties.Remove(addPropertyName);
                        properties.Add(addPropertyName, CreateDefaultProperty());
                        //Resort the properties as they are alphabetically ordered
                        var ordered = properties.OrderBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value);
                        properties.Clear();
                        foreach (var pair in ordered)
                            properties.Add(pair.Key, pair.Value);

                        isUpdating = false;
                    }
                }
                ImGui.PopItemWidth();
            }
            if (ImGui.CollapsingHeader(TranslationSource.GetText("PROPERTIES"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                float window_width = ImGui.GetWindowWidth();
                ImGui.Columns(3);
                foreach (KeyValuePair<string, dynamic> pair in properties)
                {
                    string name = pair.Key;

                    switch (name)
                    {
                        case "!Parameters":
                            continue;
                        case "LinksToObj":
                            continue;
                        case "Translate":
                            continue;
                        case "Rotate":
                            continue;
                        case "Scale":
                            continue;
                    }

                    ImGui.PushItemWidth(ImGui.GetColumnWidth());
                    ImGui.InputText($"##name{name}", ref name, 0x100);
                    ImGui.PopItemWidth();

                    ImGui.NextColumn();

                    DrawPropertiesDynamic(properties, pair.Key, pair.Value);

                    ImGui.NextColumn();

                    ImGui.PushItemWidth(80);

                    if (ImGui.Button($"Remove##{pair.Key}"))
                        removedProperties.Add(pair.Key);

                    ImGui.PopItemWidth();

                    ImGui.NextColumn();
                }
                ImGui.Columns(1);

                foreach (var prop in removedProperties)
                    properties.Remove(prop);

                if (removedProperties.Count > 0)
                    removedProperties.Clear();
            }
        }

        static dynamic CreateDefaultProperty()
        {
            switch (PropertyTypes[selectedPropertyType])
            {
                case "Float": return new MapData.Property<dynamic>(0.0f);
                case "Int": return new MapData.Property<dynamic>(0);
                case "Uint": return new MapData.Property<dynamic>(0u);
                case "String": return new MapData.Property<dynamic>("");
                case "Double": return new MapData.Property<dynamic>(0d);
                case "ULong": return new MapData.Property<dynamic>(0UL);
                case "Long": return new MapData.Property<dynamic>(0L);
                case "Bool": return new MapData.Property<dynamic>(false);
                case "Float3":
                    var dict = new Dictionary<string, dynamic>();
                    dict.Add("X", new MapData.Property<dynamic>(0.0f));
                    dict.Add("Y", new MapData.Property<dynamic>(0.0f));
                    dict.Add("Z", new MapData.Property<dynamic>(0.0f));
                    return dict;
                case "<NULL>": return null;
            }
            return null;
        }

        static List<string> removedProperties = new List<string>();

        static string addPropertyName = "";

        static int selectedPropertyType = 0;

        static string[] PropertyTypes = new string[]
        {
            "Float", "Int", "String", "Bool", "Float3", "Uint", "Double", "ULong", "Long", "<NULL>"
        };

        public static void LoadPropertyUI(IDictionary<string, dynamic> properties, string category = "PROPERTIES")
        {
            if (ImGui.CollapsingHeader(TranslationSource.GetText(category), ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.Columns(2);
                LoadProperties(properties);
                ImGui.Columns(1);
            }
        }

        static void LoadProperties(IDictionary<string, dynamic> properties, PropertyChangedCallback callback = null)
        {
            foreach (var pair in properties.ToList())
            {
                //Skip lists, scale, and rotate properties as they are loaded in the UI in other places
                if (pair.Key == "!Parameters" || pair.Key == "Scale" || pair.Key == "Translate" || pair.Key == "Rotate")
                    continue;

                if (pair.Value is IList<dynamic>)
                    continue;

                if (pair.Value is MapData.Property<dynamic> && pair.Value.Invalid)
                {
                    Vector2 p_min = ImGui.GetCursorScreenPos();
                    Vector2 p_max = new Vector2(p_min.X + ImGui.GetContentRegionAvail().X, p_min.Y + ImGui.GetFrameHeight());
                    ImGui.GetWindowDrawList().AddRectFilled(p_min, p_max, ImGui.GetColorU32(new Vector4(0.7f, 0, 0, 1)));
                }

                ImGui.Text(pair.Key);
                ImGui.NextColumn();

                DrawPropertiesDynamic(properties, pair.Key, pair.Value, callback);


                ImGui.NextColumn();
            }
        }

        static void DrawPropertiesDynamic(IDictionary<string, dynamic> properties, string key, dynamic value, PropertyChangedCallback callback = null)
        {
            float colwidth = ImGui.GetColumnWidth();
            float width = ImGui.GetWindowWidth();
            ImGui.SetColumnOffset(1, width * 0.5f);

            ImGui.PushItemWidth(colwidth);
            if (value != null)
            {
                Type type;
                if (value is MapData.Property<dynamic>)
                    type = value.Value.GetType();
                else
                    type = value.GetType();

                //Check type and set property UI here
                if (type == typeof(float))
                    DrawFloat(properties, key, callback);
                else if (type == typeof(double))
                    DrawDouble(properties, key, callback);
                else if (type == typeof(int))
                    DrawInt(properties, key, callback);
                else if (type == typeof(uint))
                    DrawUint(properties, key, callback);
                else if (type == typeof(string))
                    DrawString(properties, key, callback);
                else if (type == typeof(bool))
                    DrawBool(properties, key, callback);
                else if (IsXYZ(value))
                    DrawXYZ(properties, key, callback);
                else
                    ImGui.Text(value.ToString());
            }
            else
                ImGui.Text("<NULL>");

            if (value is MapData.Property<dynamic> && value.Value is string)
            {
                ImGui.PushFont(ImGuiController.DefaultFontBold);
                string translation = GetStringTranslation(key, value.Value);
                if (translation != null)
                    ImGui.Text(translation);
                ImGui.PopFont();
            }

            ImGui.PopItemWidth();
        }

        private static string GetStringTranslation(string key, string value)
        {
            switch (key)
            {
                case "UnitConfigName":
                    if (TranslationSource.HasKey($"ACTOR_NAME {value}"))
                        return TranslationSource.GetText($"ACTOR_NAME {value}");
                    return null;
            }
            return null;
        }

        public static bool IsXYZ(dynamic prop)
        {
            return prop is IDictionary<string, dynamic> &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("X") &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("Y") &&
                ((IDictionary<string, dynamic>)prop).ContainsKey("Z");
        }

        public static void DrawXYZ(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            dynamic values;
            if (properties[key] is MapData.Property<dynamic>)
                values = (IDictionary<string, dynamic>)properties[key].Value;
            else
                values = (IDictionary<string, dynamic>)properties[key];
            var vec = new System.Numerics.Vector3(values["X"].Value, values["Y"].Value, values["Z"].Value);
            if (ImGui.DragFloat3($"##{key}", ref vec))
            {
                values["X"].Value = vec.X;
                values["Y"].Value = vec.Y;
                values["Z"].Value = vec.Z;
            }
        }

        public static void DrawString(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            string value = (string)properties[key].Value;
            if (ImGui.InputText($"##{key}", ref value, 0x200))
            {
                properties[key].Value = (string)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public static void DrawDouble(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            double value = (double)properties[key].Value;
            if (ImGui.InputDouble($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (double)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public static void DrawFloat(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            float value = (float)properties[key].Value;
            if (ImGui.DragFloat($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (float)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public static void DrawInt(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)properties[key].Value;
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (int)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public static void DrawUint(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)((uint)properties[key].Value);
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (uint)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public static void DrawBool(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            bool value = (bool)properties[key].Value;
            if (ImGui.Checkbox($"##{key}", ref value))
            {
                properties[key].Value = (bool)value;
                if (callback != null)
                    callback(key, properties[key]);
            }
        }

        public delegate void PropertyChangedCallback(string key, MapData.Property<dynamic> property);
    }
}
