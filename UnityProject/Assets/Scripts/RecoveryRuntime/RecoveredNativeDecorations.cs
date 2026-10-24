// Recovery implementation transcribed from the original ARM64 methods.
// Evidence: Validation/NativeAnalysis/native-light-prefab-recipes.json and
// Methods/{ControlPieza,ControlFijo,ControlFijo1,ControlPiezaC,
// ControlPiezaFuente,ControlPiezaPfuente,cambioPlantas,cambioPlantas1}_Start*.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    /// <summary>Restores the original prefab decorations after native configuration.
    /// Positions, rotations, scales and material indices come from native code.</summary>
    public static class RecoveredNativeDecorations
    {
        const string AppliedMarker = "__Recovered Native Decorations";
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        static readonly Dictionary<Type, Dictionary<string, FieldInfo>> Fields = new Dictionary<Type, Dictionary<string, FieldInfo>>();
        static readonly Vector3[] SidePositions = { new Vector3(-1.01f, -.66f, 0), new Vector3(0, -.66f, 1.01f), new Vector3(1.01f, -.66f, 0), new Vector3(0, -.66f, -1.01f) };
        static readonly float[] SideYaw = { -90, 0, 90, 180 };

        public static void Apply(RecoveryPiece piece)
        {
            if (piece == null || piece.transform.Find(AppliedMarker) != null) return;
            var marker = new GameObject(AppliedMarker);
            marker.transform.SetParent(piece.transform, false);
            foreach (MonoBehaviour source in piece.GetComponents<MonoBehaviour>())
            {
                if (source == null || source == piece) continue;
                string type = source.GetType().Name;
                ApplyLights(source, type);
                if (!Bool(source, "esTextura4")) continue;
                switch (type)
                {
                    case "ControlPieza": ApplySides(source, piece, type); break;
                    case "ControlFijo": if (source.name.Contains("BloqueFijo")) ApplySides(source, piece, type); break;
                    case "ControlFijo1": if (source.name.Contains("BloFijo")) ApplySides(source, piece, type); break;
                    case "ControlPiezaC":
                        if (!Bool(source, "esTextura10")) ApplySides(source, piece, type);
                        ApplyCubeWallPlants(source);
                        break;
                    case "ControlPiezaPfuente": ApplySides(source, piece, type); break;
                    case "ControlPiezaFuente": ApplyFountainPlants(source, piece); break;
                    case "cambiaMascara": ApplyMaskPlants(source); break;
                }
            }
            foreach (MonoBehaviour source in piece.GetComponentsInChildren<MonoBehaviour>(true))
                if (source != null && source.transform != piece.transform && source.GetType().Name == "cambiaMascara" && Bool(source, "esTextura4"))
                    ApplyMaskPlants(source);
            // Authored plant meshes also use these original material selectors.
            foreach (MonoBehaviour plant in piece.GetComponentsInChildren<MonoBehaviour>(true))
                if (plant != null && (plant.GetType().Name == "cambioPlantas" || plant.GetType().Name == "cambioPlantas1"))
                    ApplyPlantMaterial(plant, Int(plant, "texturaPlant"));
        }

        static void ApplyLights(MonoBehaviour source, string type)
        {
            if (type != "ControlPieza" && type != "ControlFijo" && type != "ControlFijo1" && type != "ControlPiezaC" && type != "ControlPiezaFuente") return;
            // All C's four helper call sites are after Start's 1084F88 test.
            if (type == "ControlPiezaC" && !Bool(source, "esTextura10")) return;
            string code = String(source, "configLights");
            if (code.Length < 2) return;
            Set(source, "lig1", code.Substring(0, 1));
            Set(source, "lig2", code.Substring(1, 1));
            // The paired store of (2,0) at .rodata 0x6005A8 initializes valx/y.
            Set(source, "valx", 2f); Set(source, "valy", 0f); Set(source, "valz", 2f);
            int[] methods = LightMethods(type, code);
            foreach (int number in methods)
            {
                // C.Start creates a light only inside ladoN == 7's wall branch.
                if (type == "ControlPiezaC" && Int(source, "lado" + number) != 7) continue;
                Vector3 lightPosition = number == 5 ? Vector3.zero :
                    number == 1 ? new Vector3(-2, 0, 0) : number == 2 ? new Vector3(0, 0, 2) :
                    number == 3 ? new Vector3(2, 0, 0) : new Vector3(0, 0, -2);
                GameObject light = Spawn(source, "prefabLight1", lightPosition, Vector3.zero, null);
                Set(source, "Lights" + (number == 5 ? 1 : number), light);
                if (type == "ControlFijo1")
                {
                    // doLight1..4 set intensity 8 only for first digit 6;
                    // doLight5 sets intensity 9 only for first digit 5.
                    if (light != null && ((number <= 4 && code[0] == '6') || (number == 5 && code[0] == '5')))
                    {
                        Light lamp = light.GetComponent<Light>();
                        if (lamp != null) { lamp.intensity = number == 5 ? 9f : 8f; Set(source, "myLight", lamp); }
                    }
                    continue;
                }
                int side = number == 5 ? 4 : number;
                float edge = type == "ControlPiezaC" ? 1.02f : 1.01f;
                Vector3 focusPosition = side == 1 ? new Vector3(-edge, 0, 0) : side == 2 ? new Vector3(0, 0, edge) :
                    side == 3 ? new Vector3(edge, 0, 0) : new Vector3(0, 0, -edge);
                float yaw = side == 1 ? 90f : side == 2 ? 180f : side == 3 ? -90f : 0f;
                Set(source, "Foco" + side, Spawn(source, "prefabFoco1", focusPosition, new Vector3(0, yaw, 0), null));
            }
        }

        static int[] LightMethods(string type, string code)
        {
            char first = code[0], second = code[1];
            if (type == "ControlPiezaC")
            {
                // Original four independent equality groups, with no 3* shortcut.
                switch (code)
                {
                    case "11": return new[] { 1 }; case "12": return new[] { 2 };
                    case "13": return new[] { 3 }; case "14": return new[] { 4 };
                    case "21": return new[] { 1, 2 }; case "22": return new[] { 1, 3 };
                    case "23": return new[] { 1, 4 }; case "24": return new[] { 2, 3 };
                    case "25": return new[] { 2, 4 }; case "26": return new[] { 3, 4 };
                    default: return new int[0];
                }
            }
            if (first == '1' || (type == "ControlFijo1" && first == '6'))
                return second >= '1' && second <= '4' ? new[] { second - '0' } : new int[0];
            if (first == '2')
                switch (second)
                {
                    case '1': return new[] { 1, 2 }; case '2': return new[] { 1, 3 };
                    case '3': return new[] { 1, 4 }; case '4': return new[] { 2, 3 };
                    case '5': return new[] { 2, 4 }; case '6': return new[] { 3, 4 };
                }
            if (first == '3') return new[] { 1, 2, 3, 4 };
            if ((type == "ControlPieza" && first == '4') || (type == "ControlFijo1" && (first == '4' || first == '5'))) return new[] { 5 };
            return new int[0];
        }

        static void ApplySides(MonoBehaviour source, RecoveryPiece piece, string type)
        {
            int texture = PlantType(String(source, "tipoPlant"));
            bool block = ParentName(source, "objeto", piece).Contains("Bloque");
            string length = String(source, "longPlant");
            bool compact = ParentY(source, "objeto") < 4f;
            if (type == "ControlPieza" || type == "ControlFijo" || type == "ControlFijo1") compact |= length == "1";
            if (type == "ControlPieza" || type == "ControlPiezaC" || type == "ControlPiezaPfuente")
            {
                string spring = String(source, "conResorte");
                compact |= spring != "" && spring != "0";
            }
            for (int i = 0; i < 4; i++)
            {
                int side = i + 1;
                string prefab = "plantasPrefab4";
                if (type == "ControlPieza")
                {
                    bool open = Bool(source, "origen" + side);
                    if (open && !block) continue;
                    if (open) prefab = "plantasPrefab3";
                }
                else if (type == "ControlPiezaC")
                {
                    bool open = Bool(source, "origen" + side);
                    if (open && Int(source, "dirCentro" + side) != 1) continue;
                    if (open) prefab = "plantasPrefab3";
                }
                else if (type == "ControlPiezaPfuente")
                {
                    // Original Awake copies oriN into origN before Start.
                    bool open = Bool(source, "ori" + side);
                    Set(source, "orig" + side, open);
                    if (open) prefab = "plantasPrefab3";
                }
                else if (type == "ControlFijo1")
                {
                    if (length == "2") prefab = "plantasPrefab3";
                    Set(source, "plantasPrefab", Get(source, prefab));
                }
                Vector3 position = SidePositions[i];
                position.y = compact ? -.06f : -.66f;
                Vector3 scale = new Vector3(1, compact ? .8f : 2f, 1);
                SetPlantTransformFields(source, position, scale, SideYaw[i]);
                GameObject instance = Spawn(source, prefab, position, new Vector3(0, SideYaw[i], 0), scale);
                Set(source, "objeto1", instance);
                ApplyNewPlantMaterial(instance, texture);
            }
            bool top = type == "ControlPieza" ? block : type != "ControlPiezaC";
            if (top)
            {
                // q0 at .rodata 0x601D60=(0,.282,0,2), d0 at
                // 0x600468=(1,2). ControlFijo1/Pfuente use yaw zero.
                float yaw = type == "ControlFijo1" || type == "ControlPiezaPfuente" ? 0 : 90;
                var position = new Vector3(0, .282f, 0);
                var scale = new Vector3(2, 1, 2);
                SetPlantTransformFields(source, position, scale, yaw);
                GameObject instance = Spawn(source, "plantasPrefab5", position, new Vector3(0, yaw, 0), scale);
                Set(source, "objeto1", instance);
                ApplyNewPlantMaterial(instance, texture);
            }
        }

        static void SetPlantTransformFields(Component source, Vector3 position, Vector3 scale, float yaw)
        {
            Set(source, "posx", position.x); Set(source, "posy", position.y); Set(source, "posz", position.z);
            Set(source, "varScalex", scale.x); Set(source, "varScaley", scale.y); Set(source, "varScalez", scale.z);
            Set(source, "var_rota", yaw);
        }

        static void ApplyFountainPlants(MonoBehaviour source, RecoveryPiece piece)
        {
            int texture = PlantType(String(source, "tipoPlant"));
            string parent = ParentName(source, "objetoRenderer", piece);
            if (parent.Contains("Ladrillo"))
            {
                // 1090AAC: (position 0, scale 1), followed by four independent
                // cascade direction writes. The last true flag wins.
                float yaw = Float(source, "var_rota");
                if (Bool(source, "cascada1")) yaw = -180;
                if (Bool(source, "cascada2")) yaw = -90;
                if (Bool(source, "cascada3")) yaw = 0;
                if (Bool(source, "cascada4")) yaw = 90;
                string semi = String(source, "semiCil");
                if (semi == "1" || semi == "2")
                {
                    if (semi == "1") Active(source, "objetoRenderer", false);
                    Active(source, "semiCilindro", true);
                    var scale = new Vector3(1, 2, 1);
                    SetPlantTransformFields(source, Vector3.zero, Vector3.one, yaw);
                    GameObject half = Spawn(source, "plantasPrefab6", Vector3.zero, new Vector3(0, yaw, 0), scale);
                    Set(source, "objeto", half);
                    ApplyNewPlantMaterial(half, texture);
                }
                if (semi != "1")
                {
                    SetPlantTransformFields(source, Vector3.zero, Vector3.one, yaw);
                    GameObject plant = Spawn(source, "plantasPrefab1", Vector3.zero, new Vector3(0, yaw, 0), Vector3.one);
                    Set(source, "objeto", plant);
                    ApplyNewPlantMaterial(plant, texture);
                }
            }
            if (!parent.Contains("Bloque")) return;
            bool compact = ParentY(source, "objetoRenderer") < 4 || String(source, "longPlant") == "1";
            for (int i = 0; i < 4; i++)
            {
                Vector3 position = SidePositions[i];
                position.y = compact ? -.06f : -.66f;
                var scale = new Vector3(1, compact ? .8f : 2, 1);
                SetPlantTransformFields(source, position, scale, SideYaw[i]);
                GameObject plant = Spawn(source, Bool(source, "cascada" + (i + 1)) ? "plantasPrefab3" : "plantasPrefab4", position, new Vector3(0, SideYaw[i], 0), scale);
                Set(source, "objeto", plant);
                ApplyNewPlantMaterial(plant, texture);
            }
            var topPosition = new Vector3(0, .282f, 0);
            var topScale = new Vector3(2, 1, 2);
            SetPlantTransformFields(source, topPosition, topScale, 90);
            GameObject top = Spawn(source, "plantasPrefab5", topPosition, new Vector3(0, 90, 0), topScale);
            Set(source, "objeto", top);
            ApplyNewPlantMaterial(top, texture);
        }

        static void ApplyMaskPlants(MonoBehaviour source)
        {
            // FCCBB0: the original side-wall child uses a ten-unit plant mesh.
            GameObject target = Get(source, "objetoRenderer") as GameObject;
            string parent = target != null && target.transform.parent != null ? target.transform.parent.name : source.name;
            float yaw = Float(source, "var_rota");
            if (parent.Contains("LadrilloLateral1") || parent.Contains("LadrilloLat1H")) yaw = 0;
            if (parent.Contains("LadrilloLateral2") || parent.Contains("LadrilloLat2H")) yaw = 90;
            if (parent.Contains("LadrilloLateral3") || parent.Contains("LadrilloLat3H")) yaw = -180;
            if (parent.Contains("LadrilloLateral4") || parent.Contains("LadrilloLat4H")) yaw = -90;
            var scale = new Vector3(10, 10, 10);
            SetPlantTransformFields(source, Vector3.zero, scale, yaw);
            GameObject plant = Spawn(source, "plantasPrefab1", Vector3.zero, new Vector3(0, yaw, 0), scale);
            Set(source, "objeto", plant);
            ApplyNewPlantMaterial(plant, PlantType(String(source, "tipoPlant")));
        }

        static void ApplyCubeWallPlants(MonoBehaviour source)
        {
            // ControlPiezaC.doTextura4 108D240. This is separate from Start's
            // origen/dirCentro plants and therefore intentionally adds another set.
            int texture = PlantType(String(source, "tipoPlant"));
            bool ed = source.name.Contains("ED");
            string spring = String(source, "conResorte");
            bool compact = ParentY(source, "objeto") < 4 || String(source, "longPlant") == "1" || (spring != "" && spring != "0");
            Vector3[] positions = { new Vector3(-1.015f, -.66f, 0), new Vector3(0, -.66f, 1.02f), new Vector3(1.018f, -.67f, 0), new Vector3(0, -.66f, -1.012f) };
            bool top = ed;
            for (int i = 0; i < 4; i++)
            {
                int side = Int(source, "lado" + (i + 1));
                top |= side == 7;
                string prefab = side == 1 || side == 7 || (side == 4 && ed) ? "plantasPrefab4" :
                    side == 2 || side == 8 ? "plantasPrefab3" : side == 9 || (side == 3 && ed) ? "plantasPrefab6" : null;
                if (prefab == null) continue;
                Vector3 position = positions[i];
                if (compact) position.y = -.06f;
                var scale = new Vector3(1, compact ? .8f : 2, 1);
                SetPlantTransformFields(source, position, scale, SideYaw[i]);
                GameObject plant = Spawn(source, prefab, position, new Vector3(0, SideYaw[i], 0), scale);
                Set(source, "objeto1", plant);
                ApplyNewPlantMaterial(plant, texture);
            }
            if (top)
            {
                var position = new Vector3(0, .271f, 0);
                var scale = new Vector3(2, 1, 2);
                SetPlantTransformFields(source, position, scale, 90);
                GameObject plant = Spawn(source, "plantasPrefab5", position, new Vector3(0, 90, 0), scale);
                Set(source, "objeto1", plant);
                ApplyNewPlantMaterial(plant, texture);
            }
            if (!Bool(source, "esTextura10plant")) return;
            // Original q0 constants 6022E0, 6014A0, 602140, 600DE0.
            Vector3[] capPositions = { new Vector3(-.829f, .3f, 0), new Vector3(0, .3f, .829f), new Vector3(.829f, .3f, 0), new Vector3(0, .3f, -.829f) };
            for (int i = 0; i < 4; i++)
            {
                int side = Int(source, "lado" + (i + 1));
                if (side != 1 && side != 2) continue;
                float yaw = i == 1 || i == 3 ? 90 : 0;
                var scale = new Vector3(.35f, 1, 1);
                SetPlantTransformFields(source, capPositions[i], scale, yaw);
                GameObject plant = Spawn(source, "plantasPrefab5", capPositions[i], new Vector3(0, yaw, 0), scale);
                Set(source, "obj10", plant);
                ApplyNewPlantMaterial(plant, texture);
            }
        }

        static int PlantType(string code)
        {
            int value;
            if (int.TryParse(code, NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) return value;
            return code.Length == 1 && code[0] >= 'A' && code[0] <= 'J' ? code[0] - 'A' + 10 : 0;
        }

        static void ApplyNewPlantMaterial(GameObject instance, int texture)
        {
            if (instance == null) return;
            foreach (MonoBehaviour component in instance.GetComponents<MonoBehaviour>())
                if (component != null && (component.GetType().Name == "cambioPlantas" || component.GetType().Name == "cambioPlantas1"))
                {
                    Set(component, "texturaPlant", texture);
                    ApplyPlantMaterial(component, texture);
                }
        }

        static void ApplyPlantMaterial(MonoBehaviour plant, int texture)
        {
            bool doubleMaterial = plant.GetType().Name == "cambioPlantas1";
            int first = texture >= 1 && texture <= 19 ? texture + 1 : 1;
            int second = first;
            if (doubleMaterial)
            {
                // FCE04C defaults (1,3); case 1=(2,4), cases 2..19=(2N+1,2N+2).
                first = texture == 1 ? 2 : texture >= 2 && texture <= 19 ? texture * 2 + 1 : 1;
                second = texture == 1 ? 4 : texture >= 2 && texture <= 19 ? texture * 2 + 2 : 3;
            }
            Material material = Get(plant, "materialTextura" + first) as Material;
            Material other = Get(plant, "materialTextura" + second) as Material;
            Set(plant, "material1", material);
            if (doubleMaterial) Set(plant, "material2", other);
            for (int i = 0; i < 5; i++)
            {
                GameObject target = Get(plant, i == 0 ? "objetoRenderer" : "objetoRenderer" + i) as GameObject;
                Material chosen = doubleMaterial && i == 4 ? other : material;
                if (target != null && chosen != null)
                {
                    Renderer renderer = target.GetComponent<Renderer>();
                    if (renderer != null) renderer.sharedMaterial = chosen;
                }
            }
        }

        static GameObject Spawn(Component source, string field, Vector3 position, Vector3 euler, Vector3? scale)
        {
            GameObject prefab = Get(source, field) as GameObject;
            if (prefab == null) return null;
            // Native Instantiate(position, worldRotation), SetParent(worldPositionStays),
            // then localPosition preserves the prefab's world scale for light fixtures.
            GameObject instance = UnityEngine.Object.Instantiate(prefab, Vector3.zero, Quaternion.Euler(euler));
            instance.name = prefab.name + " [Native Decoration]";
            foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true)) if (script != null) script.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
            foreach (AudioSource audio in instance.GetComponentsInChildren<AudioSource>(true)) { audio.Stop(); audio.playOnAwake = false; }
            instance.transform.SetParent(source.transform, true);
            instance.transform.localPosition = position;
            if (scale.HasValue) instance.transform.localScale = scale.Value;
            return instance;
        }

        static string ParentName(Component source, string field, RecoveryPiece fallback)
        {
            GameObject objeto = Get(source, field) as GameObject;
            return objeto != null && objeto.transform.parent != null ? objeto.transform.parent.name : fallback.OriginalName ?? source.name;
        }
        static float ParentY(Component source, string field)
        {
            GameObject objeto = Get(source, field) as GameObject;
            return objeto != null && objeto.transform.parent != null ? objeto.transform.parent.position.y : source.transform.position.y;
        }
        static FieldInfo Field(Type type, string name)
        {
            Dictionary<string, FieldInfo> cache;
            if (!Fields.TryGetValue(type, out cache)) { cache = new Dictionary<string, FieldInfo>(StringComparer.Ordinal); Fields[type] = cache; }
            FieldInfo field;
            if (!cache.TryGetValue(name, out field)) { field = type.GetField(name, Flags); cache[name] = field; }
            return field;
        }
        static object Get(object source, string name) { FieldInfo f = Field(source.GetType(), name); return f == null ? null : f.GetValue(source); }
        static void Set(object source, string name, object value) { FieldInfo f = Field(source.GetType(), name); if (f != null && (value == null || f.FieldType.IsInstanceOfType(value))) f.SetValue(source, value); }
        static string String(object source, string name) { return Get(source, name) as string ?? ""; }
        static bool Bool(object source, string name) { object value = Get(source, name); return value is bool && (bool)value; }
        static int Int(object source, string name) { object value = Get(source, name); return value is int ? (int)value : 0; }
        static float Float(object source, string name) { object value = Get(source, name); return value is float ? (float)value : 0; }
        static void Active(object source, string name, bool active) { GameObject target = Get(source, name) as GameObject; if (target != null) target.SetActive(active); }
    }
}
