using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Executes the verified native SaveLoadManager field assignments.
    /// Unassigned prefab fields and all original material assets are preserved.</summary>
    public static class RecoveredPieceConfiguration
    {
        [Serializable] public sealed class Configuration
        {
            public int version;
            public ComponentRules[] components;
            public TokenRule[] tokens;
        }
        [Serializable] public sealed class ComponentRules
        {
            public string type;
            public PrefixRule[] prefixes;
            public FieldWrite[] tokens;
        }
        [Serializable] public sealed class PrefixRule
        {
            public string prefix;
            public BoolWrite[] assignments;
            public string materialField;
            public string evidence;
        }
        [Serializable] public sealed class BoolWrite { public string field; public bool value; }
        [Serializable] public sealed class FieldWrite { public string source, target, evidence; }
        [Serializable] public sealed class TokenRule
        {
            public string field, start, end, evidence;
            public int firstCharactersOnly;
        }

        static Configuration configuration;
        static bool attemptedLoad;
        static readonly Dictionary<string, ComponentRules> components = new Dictionary<string, ComponentRules>(StringComparer.Ordinal);
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();

        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null) return;
            EnsureLoaded();
            string code = piece.OriginalConfiguration ?? "";
            string prefix = code.Length == 0 ? "" : code.Substring(0, 1);
            Dictionary<string, string> values = ParseTokens(code);
            bool hasFixedFlag = false, isFixed = false;
            // The original loader calls GameObject.GetComponent<T>(). Child
            // mechanisms retain their own prefab configuration.
            foreach (MonoBehaviour component in piece.GetComponents<MonoBehaviour>())
            {
                if (component == null || component == piece) continue;
                ComponentRules rules;
                if (components.TryGetValue(component.GetType().Name, out rules))
                {
                    if (rules.prefixes != null)
                        foreach (PrefixRule rule in rules.prefixes)
                            if (rule.prefix == prefix)
                            {
                                foreach (BoolWrite write in rule.assignments)
                                    SetField(component, write.field, write.value);
                                break;
                            }
                    if (rules.tokens != null)
                        foreach (FieldWrite write in rules.tokens)
                        {
                            string value;
                            if (values.TryGetValue(write.source, out value)) SetField(component, write.target, value);
                        }
                    ApplyVerifiedMaterial(component);
                }
                // ControlPieza.Start 0x1080C84: fixed visual/collision object
                // leaves the movable-block layer in ordinary game mode.
                var ordinaryPipe = component as ControlPieza;
                if (ordinaryPipe != null && ordinaryPipe.esPiezaFija && UIManagerSelScript.var_design == 0 && ordinaryPipe.objeto != null)
                    ordinaryPipe.objeto.layer = 0;
                FieldInfo fixedField = Field(component.GetType(), "esPiezaFija");
                if (fixedField != null && fixedField.FieldType == typeof(bool))
                {
                    hasFixedFlag = true;
                    isFixed |= (bool)fixedField.GetValue(component);
                }
            }
            // A recorded solution cannot change the original movement flag.
            piece.CanMove = hasFixedFlag && !isFixed;
        }

        static void EnsureLoaded()
        {
            if (attemptedLoad) return;
            attemptedLoad = true;
            TextAsset asset = Resources.Load<TextAsset>("NativeConfiguration");
            if (asset == null)
            {
                Debug.LogError("NativeConfiguration is missing from Resources.");
                return;
            }
            configuration = JsonUtility.FromJson<Configuration>(asset.text);
            if (configuration == null || configuration.components == null) return;
            foreach (ComponentRules item in configuration.components) components[item.type] = item;
        }

        static Dictionary<string, string> ParseTokens(string code)
        {
            var values = new Dictionary<string, string>(StringComparer.Ordinal);
            if (configuration != null && configuration.tokens != null)
                foreach (TokenRule token in configuration.tokens)
                {
                    int start = code.IndexOf(token.start, StringComparison.Ordinal);
                    if (start < 0) continue;
                    start += token.start.Length;
                    if (start >= code.Length) continue;
                    // Native one/two-character reads only test the opening
                    // marker. In particular the saved <p1p> is read despite
                    // the original closing-marker literal being ">p".
                    if (token.firstCharactersOnly > 0)
                    {
                        int count = Math.Min(token.firstCharactersOnly, code.Length - start);
                        if (token.firstCharactersOnly > 1 && !String.IsNullOrEmpty(token.end))
                        {
                            int ending = code.IndexOf(token.end, start, StringComparison.Ordinal);
                            if (ending >= start) count = Math.Min(count, ending - start);
                        }
                        values[token.field] = code.Substring(start, count);
                    }
                    else
                    {
                        int end = code.IndexOf(token.end, start, StringComparison.Ordinal);
                        if (end >= start) values[token.field] = code.Substring(start, end - start);
                    }
                }
            const string originMarkers = "juniop";
            for (int i = 0; i < originMarkers.Length; i++)
            {
                int index = code.IndexOf(originMarkers[i]);
                if (index >= 0 && index + 1 < code.Length && !(i == 0 && index > 0 && code[index - 1] == '<'))
                    values["str" + (i + 1)] = code.Substring(index + 1, 1);
            }
            string springConfiguration = Slice(code, "<x", "x>");
            if (springConfiguration != null)
                for (int i = 0; i < 4 && (i + 1) * 4 <= springConfiguration.Length; i++)
                    values["configRes" + (i + 1)] = springConfiguration.Substring(i * 4, 4);
            string posts = Slice(code, "sh", "eh");
            if (posts != null)
                for (int i = 0; i < Math.Min(4, posts.Length); i++) values["palo" + (i + 1)] = posts.Substring(i, 1);
            string walls;
            if (values.TryGetValue("walls", out walls))
            {
                for (int i = 0; i < Math.Min(4, walls.Length); i++) values["wall" + (i + 1)] = walls.Substring(i, 1);
                if (walls.Length >= 5) values["configFuente"] = walls.Substring(4, 1);
                if (walls.Length >= 6) values["configFuenteCentro"] = walls.Substring(5, 1);
                for (int i = 0; i < 4 && walls.Length >= 10 + i * 4; i++)
                    values["configPit" + (i + 1)] = walls.Substring(6 + i * 4, 4);
            }
            string fountain;
            if (values.TryGetValue("conF", out fountain) && fountain.Length > 0)
                values["configFB"] = fountain.Substring(0, 1);
            return values;
        }

        static string Slice(string text, string opening, string closing)
        {
            int start = text.IndexOf(opening, StringComparison.Ordinal);
            if (start < 0) return null;
            start += opening.Length;
            int end = text.IndexOf(closing, start, StringComparison.Ordinal);
            return end >= start ? text.Substring(start, end - start) : null;
        }

        static FieldInfo Field(Type type, string name)
        {
            Dictionary<string, FieldInfo> table;
            if (!fields.TryGetValue(type, out table))
            {
                table = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
                foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance)) table[field.Name] = field;
                fields[type] = table;
            }
            FieldInfo result;
            return table.TryGetValue(name, out result) ? result : null;
        }

        static void SetField(Component component, string name, object value)
        {
            FieldInfo field = Field(component.GetType(), name);
            if (field == null) return;
            if (field.FieldType == typeof(int) && value is string)
            {
                int number;
                if (Int32.TryParse((string)value, out number)) field.SetValue(component, number);
            }
            else if (field.FieldType.IsInstanceOfType(value)) field.SetValue(component, value);
        }

        static bool Flag(Component component, string name)
        {
            FieldInfo field = Field(component.GetType(), name);
            return field != null && field.FieldType == typeof(bool) && (bool)field.GetValue(component);
        }

        static Material MaterialField(Component component, string name)
        {
            FieldInfo field = Field(component.GetType(), name);
            return field == null ? null : field.GetValue(component) as Material;
        }

        static void ApplyVerifiedMaterial(MonoBehaviour control)
        {
            string type = control.GetType().Name;
            if (type == "ControlPiezaCubetaB")
            {
                var basin = (ControlPiezaCubetaB)control;
                if (basin.esTextura8)
                {
                    ApplyNamedMaterial(control, "objeto", basin.materialTextura7);
                    for (int i = 1; i <= 8; i++) ApplyNamedMaterial(control, "obj" + i, basin.materialTextura7);
                }
                else if (basin.esPiezaFija)
                {
                    ApplyMaterial(basin.objeto, basin.materialFijo);
                    ApplyMaterial(basin.objeto1, basin.materialFijo1);
                }
                return;
            }
            if (type == "ControlPiezaC")
            {
                var basin = (ControlPiezaC)control;
                if (basin.esTextura8 || basin.esTextura10) basin.materialTextura = basin.materialTextura7;
                if (basin.esTextura11) basin.materialTextura = basin.materialTextura10;
                if (basin.esTextura8)
                {
                    ApplyMaterial(basin.objeto, basin.materialTextura);
                    for (int i = 1; i <= 8; i++) ApplyNamedMaterial(control, "obj" + i, basin.materialTextura);
                }
                if (basin.esTextura11) basin.material5 = basin.materialTextura8;
                if (basin.esTextura12) basin.material5 = basin.materialTextura9;
                int texture;
                if (Int32.TryParse(basin.texturaS, out texture) && texture >= 1 && texture <= 7)
                    basin.material5 = MaterialField(control, "materialTextura" + (texture == 1 ? 10 : texture + 10));
                for (int i = 1; i <= 5; i++) ApplyNamedMaterial(control, "objED" + i, basin.material5);
                return;
            }
            bool ordinary = type == "ControlPieza" || type == "ControlFijo" || type == "ControlFijo1";
            bool source = type == "ControlPiezaFuente";
            bool jet = type == "ControlPiezaPfuente";
            bool mask = type == "cambiaMascara";
            if (!ordinary && !source && !jet && !mask) return;
            bool isFixed = Flag(control, "esPiezaFija");
            FieldInfo textureField = Field(control.GetType(), "texturaS");
            string textureCode = textureField == null ? null : textureField.GetValue(control) as string;
            Material material = jet && isFixed ? MaterialField(control, "materialFijo") : null;
            if (isFixed || source || jet || mask)
            {
                if (!String.IsNullOrEmpty(textureCode))
                {
                    int texture;
                    if (Int32.TryParse(textureCode, out texture) && texture >= 1 && texture <= 7)
                        material = MaterialField(control, "materialTextura" + (texture + 9));
                }
                else
                {
                    if (isFixed) material = MaterialField(control, "materialFijo");
                    string[] flags = { "esTextura2", "esTextura3", "esTextura5", "esTextura6", "esTextura7", "esTextura8", "esTextura11", "esTextura12" };
                    for (int i = 0; i < flags.Length; i++)
                        if (Flag(control, flags[i])) material = MaterialField(control, "materialTextura" + (i + 2));
                }
            }
            else
            {
                if (Flag(control, "esTextura2")) material = MaterialField(control, "materialTextura2");
                if (Flag(control, "esTextura3")) material = MaterialField(control, "materialTextura3");
            }
            if (material == null) return;
            if ((type == "ControlFijo" || type == "ControlFijo1") && control.gameObject.name.Contains("BloquePilars"))
            {
                for (int i = 1; i <= 4; i++) ApplyNamedMaterial(control, "pilar" + i, material);
            }
            else if (source || mask)
            {
                ApplyNamedMaterial(control, "objetoRenderer", material);
                FieldInfo semiCylinder = Field(control.GetType(), "semiCil");
                if (semiCylinder != null && !String.IsNullOrEmpty(semiCylinder.GetValue(control) as string))
                    ApplyNamedMaterial(control, "semiCilindro", material);
            }
            else ApplyNamedMaterial(control, "objeto", material);
        }

        static void ApplyNamedMaterial(Component component, string name, Material material)
        {
            FieldInfo field = Field(component.GetType(), name);
            if (field != null) ApplyMaterial(field.GetValue(component) as GameObject, material);
        }

        static void ApplyMaterial(GameObject target, Material material)
        {
            if (target == null || material == null) return;
            Renderer renderer = target.GetComponent<Renderer>();
            if (renderer != null) renderer.sharedMaterial = material;
        }
    }
}
