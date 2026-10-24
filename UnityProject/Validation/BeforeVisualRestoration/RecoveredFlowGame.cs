// Recovery implementation created for this export. This is not recovered original source.
// Movement and completion use decoded gm/so records. Advanced fijo/excep mechanisms are retained,
// not represented as an invented reimplementation of the missing original game logic.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace Recovered.FlowWater
{
    public sealed class RecoveredFlowGame : MonoBehaviour
    {
        public static RecoveredFlowGame Instance { get; private set; }
        public event Action StateChanged;
        public int[] AvailableLevelIds { get; private set; } = new int[0];
        public int CurrentLevelId { get; private set; }
        public int MoveCount { get; private set; }
        public int PieceCount { get { return pieces.Count; } }
        public int UnresolvedPrefabCount { get; private set; }
        public bool IsComplete { get; private set; }
        public bool HasSolution { get; private set; }
        public string Status { get; private set; } = "Select a recovered level.";
        public Transform PieceRoot { get; private set; }
        public IReadOnlyList<RecoveryPiece> Pieces { get { return pieces; } }
        public Camera GameCamera { get; private set; }
        public int SelectedPieceIndex { get; private set; } = -1;
        public int SolutionStepCount { get { return steps.Count; } }
        public int AppliedSolutionSteps { get { return stepCursor; } }
        public bool IsLevelActive { get { return boardRoot != null && boardRoot.activeSelf; } }
        public float MinimumHeight { get { return Mathf.Min(BaseHeight, boardMin.y); } }

        const float Cell = 20f;
        const float Storey = 7.6f;
        const float BaseHeight = 3.95f;
        const float PositionTolerance = .7f;
        readonly List<RecoveryPiece> pieces = new List<RecoveryPiece>();
        readonly List<GameObject> hiddenRoots = new List<GameObject>();
        readonly Dictionary<int, RecoveryLevelEntry> levels = new Dictionary<int, RecoveryLevelEntry>();
        readonly Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, RecoveryAlias> aliases = new Dictionary<string, RecoveryAlias>(StringComparer.Ordinal);
        readonly Stack<Snapshot> history = new Stack<Snapshot>();
        readonly List<RecordedStep> steps = new List<RecordedStep>();
        readonly List<Material> ownedMaterials = new List<Material>();
        GameObject boardRoot;
        RecoverySave layout;
        RecoverySave solution;
        int stepCursor;
        bool stepSolution;
        bool solutionInventoryMatches;
        string solutionWarning;
        Vector3 boardMin, boardMax;
        RecoveryPiece dragging;
        Vector3 dragStart, dragOffset;
        float dragHeight;
        bool touchDragging;
        LineRenderer selectedOutline;
        Vector3 viewCenter;
        float viewYaw = -40f;

        sealed class RecordedStep
        {
            public int pieceIndex;
            public Vector3 origin, destination;
            public string exception;
            public bool marker;
        }

        sealed class Snapshot
        {
            public Vector3[] positions;
            public int moves, cursor;
            public bool complete;
        }

        public static RecoveredFlowGame EnsureCreated()
        {
            if (Instance == null)
            {
                var root = new GameObject("Recovery Runtime");
                DontDestroyOnLoad(root);
                Instance = root.AddComponent<RecoveredFlowGame>();
            }
            return Instance;
        }

        public static RecoveryDataValidationReport ValidateAllData()
        {
            return RecoveryDataValidation.ValidateAllData();
        }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadCatalog();
        }

        void LoadCatalog()
        {
            RecoveryCatalog catalog = RecoveryCatalog.Load();
#if UNITY_EDITOR
            if (catalog == null)
            {
                RecoveryCatalogBuilder.Build();
                catalog = RecoveryCatalog.Load();
            }
#endif
            if (catalog == null)
            {
                Status = "Runtime catalog is missing. Build it with Tools > Flow Water > Build Runtime Recovery Catalog.";
                Debug.LogError(Status);
                return;
            }
            levels.Clear(); prefabs.Clear(); aliases.Clear();
            foreach (RecoveryLevelEntry entry in catalog.levels) levels[entry.assetId] = entry;
            var ids = new List<int>(levels.Keys);
            ids.Sort(); AvailableLevelIds = ids.ToArray();
            foreach (RecoveryPrefabEntry entry in catalog.prefabs)
                if (entry.prefab != null && !prefabs.ContainsKey(entry.name)) prefabs.Add(entry.name, entry.prefab);
            if (catalog.aliases != null)
            {
                RecoveryAliasFile file = JsonUtility.FromJson<RecoveryAliasFile>(catalog.aliases.text);
                if (file != null && file.aliases != null)
                    foreach (RecoveryAlias alias in file.aliases) aliases[alias.originalName] = alias;
            }
        }

        public void LoadLevel(int assetId)
        {
            RecoveryLevelEntry entry;
            if (!levels.TryGetValue(assetId, out entry) || entry.layout == null)
            {
                Status = "No decoded gm" + assetId + " level exists.";
                Notify(); return;
            }
            RecoveryEnvelope envelope = JsonUtility.FromJson<RecoveryEnvelope>(entry.layout.text);
            if (envelope == null || envelope.data == null || !envelope.data.IsValidLayout())
            {
                Status = "Invalid layout in gm" + assetId + ".";
                Debug.LogError(Status); Notify(); return;
            }
            CancelDrag();
            ClearBoard();
            HideOriginalSceneRoots();
            layout = envelope.data;
            RecoveryEnvelope solutionEnvelope = entry.solution == null ? null : JsonUtility.FromJson<RecoveryEnvelope>(entry.solution.text);
            solution = solutionEnvelope == null ? null : solutionEnvelope.data;
            CurrentLevelId = assetId; MoveCount = 0; IsComplete = false;
            UnresolvedPrefabCount = 0; stepCursor = 0; solutionWarning = "";
            history.Clear(); steps.Clear();
            boardRoot = new GameObject("RECOVERED Board gm" + assetId);
            boardRoot.transform.SetParent(transform, false);
            PieceRoot = new GameObject("RECOVERED Original Prefab Pieces").transform;
            PieceRoot.SetParent(boardRoot.transform, false);
            boardMin = new Vector3(0, BaseHeight, 0);
            boardMax = new Vector3(80, BaseHeight, 80);
            for (int i = 0; i < layout.pieza.Count; i++)
            {
                RecoveryAlias alias;
                aliases.TryGetValue(layout.pieza[i], out alias);
                string prefabName = alias == null ? NormalizeName(layout.pieza[i]) : alias.basePrefab;
                GameObject prefab;
                GameObject instance;
                if (prefabs.TryGetValue(prefabName, out prefab)) instance = Instantiate(prefab, PieceRoot);
                else
                {
                    instance = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    instance.transform.SetParent(PieceRoot, false);
                    instance.transform.localScale = new Vector3(18, 7.6f, 18);
                    UnresolvedPrefabCount++;
                    Debug.LogError("Missing recovery prefab: " + layout.pieza[i] + " -> " + prefabName);
                }
                instance.name = "[" + i.ToString("D3") + "] " + layout.pieza[i];
                instance.transform.position = layout.Position(i);
                MakeInert(instance);
                RecoveryPiece piece = instance.AddComponent<RecoveryPiece>();
                piece.Index = i; piece.OriginalName = layout.pieza[i]; piece.BasePrefab = prefabName;
                piece.OriginalConfiguration = layout.fijo[i]; piece.ColorVariant = alias == null ? 0 : alias.colorSuffix;
                piece.InitialPosition = instance.transform.position;
                boardMin.y = Mathf.Min(boardMin.y, piece.InitialPosition.y);
                boardMax.y = Mathf.Max(boardMax.y, piece.InitialPosition.y);
                // Only the explicitly observed free marker is interpreted here. Recorded actual movement
                // below may additionally identify a movable piece. All other configuration text remains intact.
                piece.CanMove = string.Equals(layout.fijo[i], "N", StringComparison.Ordinal);
                pieces.Add(piece);
                if (piece.CanMove)
                {
                    boardMin = Vector3.Min(boardMin, piece.InitialPosition);
                    boardMax = Vector3.Max(boardMax, piece.InitialPosition);
                }
                AddPickingCollider(piece);
            }
            ConfigureSolution();
            foreach (RecoveryPiece piece in pieces)
            {
                if (!piece.HasGoal) continue;
                boardMin = Vector3.Min(boardMin, piece.GoalPosition);
                boardMax = Vector3.Max(boardMax, piece.GoalPosition);
            }
            CreateBoardAndView();
            SelectedPieceIndex = -1;
            Status = "gm" + assetId + " · Drag pieces onto the grid. " +
                (HasSolution ? (stepSolution ? "Recorded move solution available." : "Recorded final layout available.") : "Solution requires unresolved original mechanisms.") +
                (string.IsNullOrEmpty(solutionWarning) ? "" : " " + solutionWarning);
            CheckComplete(false);
            Debug.Log("Recovery loaded gm" + assetId + ": " + pieces.Count + " pieces, " + UnresolvedPrefabCount + " unresolved prefabs. " + Status);
            Notify();
        }

        static string NormalizeName(string value)
        {
            return value == null ? "" : value.Replace("(Clone)", "").Trim();
        }

        string GeometryKey(string originalName)
        {
            RecoveryAlias alias;
            if (aliases.TryGetValue(originalName, out alias)) return alias.basePrefab + "#" + alias.colorSuffix;
            return NormalizeName(originalName);
        }

        void ConfigureSolution()
        {
            stepSolution = solution != null && solution.IsValidSteps();
            solutionInventoryMatches = false;
            HasSolution = false;
            if (stepSolution)
            {
                var positions = new Vector3[pieces.Count];
                for (int i = 0; i < pieces.Count; i++) positions[i] = pieces[i].InitialPosition;
                bool valid = true;
                for (int i = 0; i < solution.move.Count; i++)
                {
                    Vector3 origin = StepPosition(solution.orix[i], solution.oriy[i], solution.oriz[i]);
                    Vector3 destination = StepPosition(solution.desx[i], solution.desy[i], solution.desz[i]);
                    int pieceIndex = FindAt(positions, origin, PositionTolerance);
                    bool marker = (origin - destination).sqrMagnitude < .01f;
                    if (pieceIndex < 0 && !marker)
                    {
                        valid = false;
                        solutionWarning = "Recorded step " + (i + 1) + " could not be matched; advanced mechanism needs recovery.";
                        break;
                    }
                    var step = new RecordedStep
                    {
                        pieceIndex = pieceIndex, origin = origin, destination = destination,
                        exception = solution.excep != null && i < solution.excep.Count ? solution.excep[i] : "",
                        marker = marker
                    };
                    steps.Add(step);
                    if (pieceIndex >= 0 && !marker)
                    {
                        positions[pieceIndex] = destination;
                        pieces[pieceIndex].CanMove = true;
                    }
                }
                if (valid)
                {
                    for (int i = 0; i < pieces.Count; i++)
                    {
                        pieces[i].GoalPosition = positions[i];
                        pieces[i].HasGoal = true;
                    }
                    solutionInventoryMatches = true;
                    HasSolution = true;
                }
                return;
            }
            if (solution == null || !solution.IsValidLayout()) return;
            var used = new bool[solution.pieza.Count];
            var matches = new int[pieces.Count];
            for (int i = 0; i < matches.Length; i++) matches[i] = -1;
            // Unity serialization order can change between two saves even for fixed, identical blocks.
            // Reserve unchanged geometry/positions first so those blocks retain their fixed state.
            for (int i = 0; i < pieces.Count; i++)
                for (int j = 0; j < solution.pieza.Count; j++)
                {
                    if (used[j] || GeometryKey(pieces[i].OriginalName) != GeometryKey(solution.pieza[j]) ||
                        (pieces[i].InitialPosition - solution.Position(j)).sqrMagnitude > PositionTolerance * PositionTolerance) continue;
                    matches[i] = j; used[j] = true; break;
                }
            int assigned = 0;
            for (int i = 0; i < pieces.Count; i++)
            {
                RecoveryPiece piece = pieces[i];
                int match = matches[i];
                if (match < 0)
                {
                    float nearest = float.PositiveInfinity;
                    for (int j = 0; j < solution.pieza.Count; j++)
                    {
                        if (used[j] || GeometryKey(piece.OriginalName) != GeometryKey(solution.pieza[j])) continue;
                        float distance = (piece.InitialPosition - solution.Position(j)).sqrMagnitude;
                        if (distance < nearest) { nearest = distance; match = j; }
                    }
                }
                if (match < 0) continue;
                used[match] = true; assigned++;
                piece.GoalPosition = solution.Position(match); piece.HasGoal = true;
                if ((piece.GoalPosition - piece.InitialPosition).sqrMagnitude > PositionTolerance * PositionTolerance) piece.CanMove = true;
            }
            solutionInventoryMatches = assigned == pieces.Count && assigned == solution.pieza.Count;
            HasSolution = solutionInventoryMatches;
            if (!solutionInventoryMatches)
                solutionWarning = "Original gm/so piece inventories differ; unmatched records are preserved and completion is withheld.";
        }

        public static Vector3 StepPosition(int x, int y, int z)
        {
            return new Vector3((x - 3) * Cell, BaseHeight + (y - 1) * Storey, (z - 3) * Cell);
        }

        static int FindAt(Vector3[] positions, Vector3 target, float tolerance)
        {
            int found = -1; float best = tolerance * tolerance;
            for (int i = 0; i < positions.Length; i++)
            {
                float distance = (positions[i] - target).sqrMagnitude;
                if (distance <= best) { best = distance; found = i; }
            }
            return found;
        }

        void MakeInert(GameObject instance)
        {
            foreach (MonoBehaviour behaviour in instance.GetComponentsInChildren<MonoBehaviour>(true))
                if (behaviour != null) behaviour.enabled = false;
            foreach (Animator animator in instance.GetComponentsInChildren<Animator>(true)) animator.enabled = false;
            foreach (Animation animation in instance.GetComponentsInChildren<Animation>(true)) animation.enabled = false;
            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true)) { body.isKinematic = true; body.useGravity = false; }
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            foreach (ParticleSystem particles in instance.GetComponentsInChildren<ParticleSystem>(true))
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            foreach (AudioSource audioSource in instance.GetComponentsInChildren<AudioSource>(true)) { audioSource.Stop(); audioSource.playOnAwake = false; }
        }

        void AddPickingCollider(RecoveryPiece piece)
        {
            Bounds bounds = new Bounds(piece.transform.position, new Vector3(18, 7, 18));
            bool found = false;
            foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>(false))
            {
                if (!found) { bounds = renderer.bounds; found = true; }
                else bounds.Encapsulate(renderer.bounds);
            }
            var proxy = new GameObject("Recovery Pick Collider");
            proxy.transform.position = bounds.center;
            proxy.transform.SetParent(piece.transform, true);
            // Keep the proxy's world scale independent of the heterogeneous authored prefab scales.
            Vector3 scale = piece.transform.lossyScale;
            proxy.transform.localScale = new Vector3(1 / Mathf.Max(Mathf.Abs(scale.x), .0001f),
                1 / Mathf.Max(Mathf.Abs(scale.y), .0001f), 1 / Mathf.Max(Mathf.Abs(scale.z), .0001f));
            proxy.transform.rotation = Quaternion.identity;
            BoxCollider pick = proxy.AddComponent<BoxCollider>();
            pick.size = new Vector3(Mathf.Clamp(bounds.size.x, 10, 19), Mathf.Clamp(bounds.size.y, 6, 18), Mathf.Clamp(bounds.size.z, 10, 19));
        }

        void CreateBoardAndView()
        {
            Material dark = NewMaterial(new Color(.22f, .30f, .26f));
            Material light = NewMaterial(new Color(.28f, .37f, .30f));
            for (float x = boardMin.x; x <= boardMax.x + .1f; x += Cell)
                for (float z = boardMin.z; z <= boardMax.z + .1f; z += Cell)
                {
                    GameObject tile = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    tile.name = "Recovery Grid " + x + "," + z;
                    tile.transform.SetParent(boardRoot.transform, false);
                    // Many original layouts contain underground layers. Put the helper grid below the
                    // lowest saved piece so an opaque replacement floor cannot hide recovered geometry.
                    tile.transform.position = new Vector3(x, MinimumHeight - 4.65f, z);
                    tile.transform.localScale = new Vector3(19.7f, .8f, 19.7f);
                    tile.GetComponent<Collider>().enabled = false;
                    tile.GetComponent<Renderer>().sharedMaterial = ((Mathf.RoundToInt(x / Cell) + Mathf.RoundToInt(z / Cell)) & 1) == 0 ? dark : light;
                }
            Bounds viewBounds = new Bounds(new Vector3((boardMin.x + boardMax.x) * .5f, 5, (boardMin.z + boardMax.z) * .5f),
                new Vector3(boardMax.x - boardMin.x + 22, 15, boardMax.z - boardMin.z + 22));
            foreach (RecoveryPiece piece in pieces)
                foreach (MeshRenderer renderer in piece.GetComponentsInChildren<MeshRenderer>(false)) viewBounds.Encapsulate(renderer.bounds);
            viewCenter = viewBounds.center;
            GameObject cameraObject = new GameObject("Recovery Game Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.transform.SetParent(boardRoot.transform, false);
            GameCamera = cameraObject.GetComponent<Camera>();
            GameCamera.orthographic = true;
            GameCamera.orthographicSize = Mathf.Max(68, viewBounds.extents.magnitude * .88f);
            GameCamera.nearClipPlane = .1f; GameCamera.farClipPlane = 3000;
            GameCamera.clearFlags = CameraClearFlags.SolidColor;
            GameCamera.backgroundColor = new Color(.10f, .15f, .18f);
            // Leave space above/below for the recovery HUD at both portrait and landscape ratios.
            GameCamera.rect = new Rect(0, .10f, 1, .78f);
            UpdateCameraPosition();
            GameObject lightObject = new GameObject("Recovery Directional Light", typeof(Light));
            lightObject.transform.SetParent(boardRoot.transform, false);
            lightObject.transform.rotation = Quaternion.Euler(55, -35, 0);
            Light sun = lightObject.GetComponent<Light>(); sun.type = LightType.Directional; sun.intensity = 1.05f;
            sun.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.64f, .68f, .72f);
            selectedOutline = new GameObject("Recovery Selected Piece Outline", typeof(LineRenderer)).GetComponent<LineRenderer>();
            selectedOutline.transform.SetParent(boardRoot.transform, false);
            selectedOutline.useWorldSpace = true; selectedOutline.positionCount = 5;
            selectedOutline.widthMultiplier = .7f;
            selectedOutline.sharedMaterial = NewMaterial(new Color(1f, .83f, .25f), true);
            selectedOutline.enabled = false;
            Physics.SyncTransforms();
        }

        Material NewMaterial(Color color, bool unlit = false)
        {
            Shader shader = Shader.Find(unlit ? "Unlit/Color" : "Standard");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            Material material = new Material(shader); material.color = color; ownedMaterials.Add(material); return material;
        }

        void UpdateCameraPosition()
        {
            if (GameCamera == null) return;
            Vector3 direction = Quaternion.Euler(0, viewYaw, 0) * new Vector3(0, 1.15f, -1.2f);
            GameCamera.transform.position = viewCenter + direction.normalized * 450;
            GameCamera.transform.LookAt(viewCenter);
            float aspect = Mathf.Max(.2f, GameCamera.aspect);
            // Wider boards stay visible in portrait game views as well.
            if (aspect < 1) GameCamera.orthographicSize = Mathf.Max(GameCamera.orthographicSize, (boardMax.x - boardMin.x + 50) / (2 * aspect));
        }

        public bool TryMove(int pieceIndex, Vector3 destination)
        {
            if (pieceIndex < 0 || pieceIndex >= pieces.Count || !IsLevelActive) return false;
            RecoveryPiece piece = pieces[pieceIndex];
            if (!piece.CanMove) { Status = "This piece is fixed in the recovered records."; Notify(); return false; }
            if (float.IsNaN(destination.x) || float.IsNaN(destination.y) || float.IsNaN(destination.z) ||
                float.IsInfinity(destination.x) || float.IsInfinity(destination.y) || float.IsInfinity(destination.z)) return false;
            destination = Snap(piece, destination);
            if ((piece.transform.position - destination).sqrMagnitude < .05f) return false;
            if (destination.x < boardMin.x - .1f || destination.x > boardMax.x + .1f || destination.z < boardMin.z - .1f ||
                destination.z > boardMax.z + .1f || destination.y < MinimumHeight - .5f || destination.y > Mathf.Max(boardMax.y + Storey, 57.2f))
            { Status = "Place the piece inside the grid."; Notify(); return false; }
            if (Occupant(destination, pieceIndex) >= 0)
            { Status = "That grid position is occupied. Use an empty position or another height."; Notify(); return false; }
            PushSnapshot();
            piece.transform.position = destination;
            MoveCount++; SelectedPieceIndex = pieceIndex;
            Status = "Piece moved. Drag to place; mouse wheel while dragging changes height.";
            CheckComplete(true); Physics.SyncTransforms(); Notify(); return true;
        }

        Vector3 Snap(RecoveryPiece piece, Vector3 value)
        {
            return new Vector3(Mathf.Round(value.x / Cell) * Cell,
                piece.InitialPosition.y + Mathf.Round((value.y - piece.InitialPosition.y) / Storey) * Storey,
                Mathf.Round(value.z / Cell) * Cell);
        }

        int Occupant(Vector3 position, int except)
        {
            for (int i = 0; i < pieces.Count; i++)
                if (i != except && Mathf.Abs(pieces[i].transform.position.x - position.x) < 1 &&
                    Mathf.Abs(pieces[i].transform.position.z - position.z) < 1 &&
                    Mathf.Abs(pieces[i].transform.position.y - position.y) < 3) return i;
            return -1;
        }

        public void ApplyHint()
        {
            CancelDrag();
            if (!HasSolution || IsComplete) { Notify(); return; }
            if (stepSolution)
            {
                if (stepCursor >= steps.Count)
                {
                    Status = "Recorded steps were applied. Restart to replay the original sequence from its initial layout.";
                    Notify(); return;
                }
                PushSnapshot();
                RecordedStep step = steps[stepCursor++];
                if (!step.marker && step.pieceIndex >= 0)
                    pieces[step.pieceIndex].transform.position = step.destination;
                MoveCount++;
                SelectedPieceIndex = step.pieceIndex;
                Status = "Recorded hint " + stepCursor + "/" + steps.Count +
                    (string.IsNullOrEmpty(step.exception) ? "." : " (original mechanism code: " + step.exception + "; placement replay).");
                CheckComplete(true); Physics.SyncTransforms(); Notify(); return;
            }
            // Prefer a goal with no occupant. This avoids overlap and produces an actionable move.
            for (int i = 0; i < pieces.Count; i++)
            {
                RecoveryPiece piece = pieces[i];
                if (!piece.HasGoal || AtGoal(piece) || Occupant(piece.GoalPosition, i) >= 0) continue;
                PushSnapshot(); piece.transform.position = piece.GoalPosition;
                MoveCount++; SelectedPieceIndex = i;
                Status = "Hint placed one piece at its recorded solution position.";
                CheckComplete(true); Physics.SyncTransforms(); Notify(); return;
            }
            // A cycle of interchangeable pieces can occupy every goal. Move one obstructing movable
            // piece into a genuine empty cell, so following hints can finish the placement cycle.
            for (int i = 0; i < pieces.Count; i++)
            {
                RecoveryPiece piece = pieces[i];
                if (!piece.HasGoal || AtGoal(piece) || !piece.CanMove) continue;
                for (float x = boardMin.x; x <= boardMax.x; x += Cell)
                    for (float z = boardMin.z; z <= boardMax.z; z += Cell)
                    {
                        Vector3 empty = new Vector3(x, piece.InitialPosition.y, z);
                        if (Occupant(empty, i) >= 0 || (empty - piece.transform.position).sqrMagnitude < 1) continue;
                        PushSnapshot(); piece.transform.position = empty;
                        MoveCount++; SelectedPieceIndex = i;
                        Status = "Hint cleared an occupied target; apply another hint to continue.";
                        CheckComplete(true); Physics.SyncTransforms(); Notify(); return;
                    }
            }
            Status = "No empty position is available for the next hint."; Notify();
        }

        /// <summary>Explicit solution preview; completion is still checked against actual piece positions.</summary>
        public void Solve()
        {
            CancelDrag();
            if (!HasSolution || IsComplete) return;
            PushSnapshot();
            int changed = 0;
            foreach (RecoveryPiece piece in pieces)
                if (piece.HasGoal && !AtGoal(piece)) { piece.transform.position = piece.GoalPosition; changed++; }
            MoveCount += Mathf.Max(1, changed); stepCursor = steps.Count;
            Status = "Recorded solution preview applied.";
            CheckComplete(true); Physics.SyncTransforms(); Notify();
        }

        bool AtGoal(RecoveryPiece piece)
        {
            return (piece.transform.position - piece.GoalPosition).sqrMagnitude <= PositionTolerance * PositionTolerance;
        }

        void CheckComplete(bool report)
        {
            bool complete = HasSolution && solutionInventoryMatches && pieces.Count > 0 && UnresolvedPrefabCount == 0;
            if (complete)
            {
                // Equal prefab/color pieces are interchangeable. Compare the actual geometry/position
                // multiset rather than object identities, otherwise swapping identical pipes cannot win.
                var matched = new bool[pieces.Count];
                foreach (RecoveryPiece goal in pieces)
                {
                    if (!goal.HasGoal) { complete = false; break; }
                    int found = -1;
                    for (int i = 0; i < pieces.Count; i++)
                        if (!matched[i] && GeometryKey(pieces[i].OriginalName) == GeometryKey(goal.OriginalName) &&
                            (pieces[i].transform.position - goal.GoalPosition).sqrMagnitude <= PositionTolerance * PositionTolerance)
                        { found = i; break; }
                    if (found < 0) { complete = false; break; }
                    matched[found] = true;
                }
            }
            IsComplete = complete;
            if (complete)
            {
                Status = "Recorded solution matched · " + MoveCount + " moves. Advanced water/mechanism behavior remains a reconstruction.";
                PlayerPrefs.SetInt("Recovery.Completed.gm" + CurrentLevelId, 1);
                PlayerPrefs.Save();
                if (report) Debug.Log("Recovery solution matched gm" + CurrentLevelId + " by real piece coordinates.");
            }
        }

        void PushSnapshot()
        {
            var positions = new Vector3[pieces.Count];
            for (int i = 0; i < pieces.Count; i++) positions[i] = pieces[i].transform.position;
            history.Push(new Snapshot { positions = positions, moves = MoveCount, cursor = stepCursor, complete = IsComplete });
        }

        public void Undo()
        {
            CancelDrag();
            if (history.Count == 0) return;
            Snapshot saved = history.Pop();
            for (int i = 0; i < pieces.Count; i++) pieces[i].transform.position = saved.positions[i];
            MoveCount = saved.moves; stepCursor = saved.cursor; IsComplete = saved.complete;
            Status = "Last move undone."; CheckComplete(false); Physics.SyncTransforms(); Notify();
        }

        public void RestartLevel()
        {
            if (CurrentLevelId > 0) LoadLevel(CurrentLevelId);
        }

        public void NextLevel()
        {
            for (int i = 0; i < AvailableLevelIds.Length; i++)
                if (AvailableLevelIds[i] > CurrentLevelId) { LoadLevel(AvailableLevelIds[i]); return; }
            Status = "This is the last recovered asset ID."; Notify();
        }

        public void RaiseSelectedPiece() { MoveSelectedHeight(Storey); }
        public void LowerSelectedPiece() { MoveSelectedHeight(-Storey); }

        void MoveSelectedHeight(float delta)
        {
            if (dragging != null)
            {
                dragHeight = Mathf.Clamp(dragHeight + delta, MinimumHeight, Mathf.Max(boardMax.y + Storey, 57.2f));
                return;
            }
            if (SelectedPieceIndex >= 0 && SelectedPieceIndex < pieces.Count)
                TryMove(SelectedPieceIndex, pieces[SelectedPieceIndex].transform.position + Vector3.up * delta);
        }

        public void ReturnToOriginalScene()
        {
            CancelDrag(); ClearBoard();
            foreach (GameObject root in hiddenRoots) if (root != null) root.SetActive(true);
            hiddenRoots.Clear();
            Status = "Select a recovered level."; Notify();
        }

        void HideOriginalSceneRoots()
        {
            for (int sceneIndex = 0; sceneIndex < SceneManager.sceneCount; sceneIndex++)
            {
                Scene scene = SceneManager.GetSceneAt(sceneIndex);
                if (!scene.isLoaded) continue;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (!root.activeSelf || root == gameObject || root.name.StartsWith("Recovery", StringComparison.OrdinalIgnoreCase) ||
                        root.name.StartsWith("RECOVERED", StringComparison.OrdinalIgnoreCase) || root.GetComponentInChildren<EventSystem>(true) != null) continue;
                    hiddenRoots.Add(root); root.SetActive(false);
                }
            }
        }

        void ClearBoard()
        {
            if (boardRoot != null) { boardRoot.SetActive(false); Destroy(boardRoot); }
            boardRoot = null; PieceRoot = null; GameCamera = null; selectedOutline = null;
            pieces.Clear(); SelectedPieceIndex = -1;
            foreach (Material material in ownedMaterials) if (material != null) Destroy(material);
            ownedMaterials.Clear();
        }

        void Update()
        {
            if (!IsLevelActive || GameCamera == null) return;
            if (Input.GetKeyDown(KeyCode.H)) ApplyHint();
            if (Input.GetKeyDown(KeyCode.R)) RestartLevel();
            if (Input.GetKeyDown(KeyCode.Z) && (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))) Undo();
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                if (touch.phase == TouchPhase.Began)
                {
                    if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject(touch.fingerId)) BeginDrag(touch.position);
                    touchDragging = true;
                }
                else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) ContinueDrag(touch.position);
                else if (touch.phase == TouchPhase.Ended) { EndDrag(); touchDragging = false; }
                else if (touch.phase == TouchPhase.Canceled) { CancelDrag(); touchDragging = false; }
            }
            else if (!touchDragging)
            {
                if (Input.GetMouseButtonDown(0) && (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())) BeginDrag(Input.mousePosition);
                if (Input.GetMouseButton(0)) ContinueDrag(Input.mousePosition);
                if (Input.GetMouseButtonUp(0)) EndDrag();
                float wheel = Input.mouseScrollDelta.y;
                if (Mathf.Abs(wheel) > .01f)
                {
                    if (dragging != null) dragHeight = Mathf.Clamp(dragHeight + Mathf.Sign(wheel) * Storey, MinimumHeight, Mathf.Max(boardMax.y + Storey, 57.2f));
                    else GameCamera.orthographicSize = Mathf.Clamp(GameCamera.orthographicSize - wheel * 5, 35, 250);
                }
                if (Input.GetMouseButton(1)) { viewYaw += Input.GetAxis("Mouse X") * 3; UpdateCameraPosition(); }
            }
            if (SelectedPieceIndex >= 0 && SelectedPieceIndex < pieces.Count && dragging == null)
            {
                float dy = Input.GetKeyDown(KeyCode.PageUp) ? Storey : Input.GetKeyDown(KeyCode.PageDown) ? -Storey : 0;
                if (dy != 0) TryMove(SelectedPieceIndex, pieces[SelectedPieceIndex].transform.position + Vector3.up * dy);
            }
            UpdateSelectionOutline();
        }

        void BeginDrag(Vector2 pointer)
        {
            if (!GameCamera.pixelRect.Contains(pointer)) return;
            Ray ray = GameCamera.ScreenPointToRay(pointer);
            RaycastHit[] hits = Physics.RaycastAll(ray, 3000);
            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
            foreach (RaycastHit hit in hits)
            {
                RecoveryPiece piece = hit.collider.GetComponentInParent<RecoveryPiece>();
                if (piece == null) continue;
                SelectedPieceIndex = piece.Index;
                if (!piece.CanMove) { Status = "Fixed recovered piece. Its configuration is preserved."; Notify(); return; }
                dragging = piece; dragStart = piece.transform.position; dragHeight = dragStart.y;
                Plane plane = new Plane(Vector3.up, new Vector3(0, dragHeight, 0)); float distance;
                dragOffset = plane.Raycast(ray, out distance) ? dragStart - ray.GetPoint(distance) : Vector3.zero;
                Status = "Drag onto a grid position; wheel changes height; release to place."; Notify(); return;
            }
        }

        void ContinueDrag(Vector2 pointer)
        {
            if (dragging == null) return;
            Ray ray = GameCamera.ScreenPointToRay(pointer);
            Plane plane = new Plane(Vector3.up, new Vector3(0, dragHeight, 0)); float distance;
            if (plane.Raycast(ray, out distance))
            {
                Vector3 point = ray.GetPoint(distance) + dragOffset; point.y = dragHeight;
                dragging.transform.position = Snap(dragging, point);
            }
        }

        void EndDrag()
        {
            if (dragging == null) return;
            RecoveryPiece piece = dragging; Vector3 destination = piece.transform.position;
            piece.transform.position = dragStart; dragging = null;
            TryMove(piece.Index, destination);
        }

        void CancelDrag()
        {
            if (dragging != null) dragging.transform.position = dragStart;
            dragging = null; touchDragging = false;
        }

        void UpdateSelectionOutline()
        {
            if (selectedOutline == null) return;
            selectedOutline.enabled = SelectedPieceIndex >= 0 && SelectedPieceIndex < pieces.Count;
            if (!selectedOutline.enabled) return;
            Vector3 p = pieces[SelectedPieceIndex].transform.position + Vector3.up * 4.4f;
            selectedOutline.SetPositions(new[] { p + new Vector3(-9.5f, 0, -9.5f), p + new Vector3(9.5f, 0, -9.5f),
                p + new Vector3(9.5f, 0, 9.5f), p + new Vector3(-9.5f, 0, 9.5f), p + new Vector3(-9.5f, 0, -9.5f) });
        }

        void Notify() { if (StateChanged != null) StateChanged(); }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}
