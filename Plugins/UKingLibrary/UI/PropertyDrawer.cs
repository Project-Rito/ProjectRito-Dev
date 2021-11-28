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
            IDictionary<string, dynamic> properties = null;
            if (values.ContainsKey("!Parameters"))
                properties = (IDictionary<string, dynamic>)values["!Parameters"];

            float width = ImGui.GetWindowWidth();

            if (ImGui.CollapsingHeader(TranslationSource.GetText("OBJ"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Edit##obj", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show("Property Window", () => PropertiesDialog(values), null);

                ImGui.Columns(2);
                LoadProperties(values, callback);
                ImGui.Columns(1);
            }

            if (properties != null && ImGui.CollapsingHeader(TranslationSource.GetText("PROPERTIES"), ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (ImGui.Button("Edit##prop", new System.Numerics.Vector2(width, 22)))
                    DialogHandler.Show("Property Window", () => PropertiesDialog(properties), null);

                ImGui.Columns(2);
                LoadProperties(properties, callback);
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
                ImGui.Columns(2);
                foreach (var link in mapObject.DestLinks)
                    DrawLinkItem(link);

                ImGui.Columns(1);
            }
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

        static List<string> removedProperties = new List<string>();

        //A dialog to add/remove properties.
        static void PropertiesDialog(IDictionary<string, dynamic> properties)
        {
            if (isUpdating)
                return;

            if (ImGui.CollapsingHeader("Add Property", ImGuiTreeNodeFlags.DefaultOpen))
            {
                ImGui.PushItemWidth(100);
                if (ImGui.Combo("", ref selectedPropertyType, PropertyTypes, PropertyTypes.Length, 100))
                {

                }
                ImGui.PopItemWidth();

                ImGui.SameLine();

                ImGui.InputText($"##addpropname", ref addPropertyName, 0x100);
                ImGui.SameLine();

                ImGui.PushItemWidth(100);
                if (ImGui.Button("Add"))
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
            if (ImGui.CollapsingHeader("Properties", ImGuiTreeNodeFlags.DefaultOpen))
            {
                float window_width = ImGui.GetWindowWidth();
                ImGui.Columns(3);
                foreach (var pair in properties)
                {
                    string name = pair.Key;

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
                case "Float": return 0.0f;
                case "Int": return 0;
                case "Uint": return 0u;
                case "String": return "";
                case "Double": return 0d;
                case "ULong": return 0UL;
                case "Long": return 0L;
                case "Bool": return false;
                case "Float3":
                    var dict = new Dictionary<string, dynamic>();
                    dict.Add("X", 0.0f);
                    dict.Add("Y", 0.0f);
                    dict.Add("Z", 0.0f);
                    return dict;
                case "<NULL>": return null;
            }
            return null;
        }

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
            foreach (var pair in properties)
            {
                //Skip lists, scale, and rotate properties as they are loaded in the UI in other places
                if (pair.Key == "!Parameters" || pair.Key == "Scale" || pair.Key == "Translate" || pair.Key == "Rotate")
                    continue;

                if (pair.Value is IList<dynamic>)
                    continue;

                if (pair.Value.Invalid)
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
                Type type = value.Value.GetType();

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
            ImGui.PopItemWidth();
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
            var values = (IDictionary<string, dynamic>)properties[key].Value;
            var vec = new System.Numerics.Vector3(values["X"], values["Y"], values["Z"]);
            if (ImGui.DragFloat3($"##{key}", ref vec))
            {
                values["X"] = vec.X;
                values["Y"] = vec.Y;
                values["Z"] = vec.Z;
            }
        }

        public static void DrawString(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            string value = (string)properties[key].Value;
            if (ImGui.InputText($"##{key}", ref value, 0x200))
            {
                properties[key].Value = (string)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawDouble(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            double value = (double)properties[key].Value;
            if (ImGui.InputDouble($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (double)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawFloat(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            float value = (float)properties[key].Value;
            if (ImGui.DragFloat($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (float)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawInt(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)properties[key].Value;
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (int)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawUint(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            int value = (int)((uint)properties[key].Value);
            if (ImGui.DragInt($"##{key}", ref value, 1, 1))
            {
                properties[key].Value = (uint)value;
                if (callback != null)
                    callback(key);
            }
        }

        public static void DrawBool(IDictionary<string, dynamic> properties, string key, PropertyChangedCallback callback = null)
        {
            bool value = (bool)properties[key].Value;
            if (ImGui.Checkbox($"##{key}", ref value))
            {
                properties[key].Value = (bool)value;
                if (callback != null)
                    callback(key);
            }
        }

        public delegate void PropertyChangedCallback(string key);
    }
}
