// Original mechanism state, action and pose evidence is recorded in
// Validation/NativeAnalysis/native-spring-state-and-replay.json.
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Recovered.FlowWater
{
    [DefaultExecutionOrder(200)]
    public sealed class RecoveredNativeSprings : MonoBehaviour
    {
        const float Storey = 7.6f;
        const BindingFlags Flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        sealed class Actor
        {
            public RecoveryPiece piece;
            public MonoBehaviour source;
            public ObservadorResorte observer;
            public string top, bottom, move;
            public float moveSpeed = 5, rotationSpeed = 16;
            public readonly List<Binding> bindings = new List<Binding>();
            public readonly List<RecoveryPiece> carried = new List<RecoveryPiece>();
            public Vector3 target;
            public Quaternion targetRotation;
            public bool moving, blocked, turning;
            public Binding current;
            public int direction;
            public GameObject blockedModel;
        }
        sealed class Binding
        {
            public Actor actor;
            public int channel, position;
            public string actions;
            public readonly List<Vector3> poses = new List<Vector3>();
            public readonly List<Quaternion> rotations = new List<Quaternion>();
        }
        sealed class Channel
        {
            public readonly List<Binding> bindings = new List<Binding>();
            public int index, position, maximum, direction, manager;
            public bool wasActive, moving, blocked;
        }
        sealed class Plate
        {
            public RecoveryPiece piece;
            public MonoBehaviour source;
            public string channels;
            public int mode, phase = 1;
            public bool wasOccupied, output1, output2, published1, published2, pending;
            public float when;
            public RecoveryPiece occupant;
        }
        sealed class State
        {
            public ActorState[] actors;
            public BindingState[] bindings;
            public ChannelState[] channels;
            public PlateState[] plates;
        }
        sealed class ActorState
        {
            public bool moving, blocked, turning;
            public int currentChannel, direction;
            public Vector3 target;
            public Quaternion rotation;
            public int[] carried;
        }
        sealed class BindingState { public int position; public Vector3[] poses; public Quaternion[] rotations; }
        sealed class ChannelState { public int position, direction, manager; public bool wasActive, moving, blocked; }
        sealed class PlateState
        {
            public int phase; public bool wasOccupied, output1, output2, published1, published2, pending; public float remaining; public int occupant;
            public bool hasBody, hasAnimator, animatorEnabled, animatorPressed;
            public Vector3 bodyPosition, bodyScale; public Quaternion bodyRotation;
            public int animatorState; public float animatorTime;
        }
        readonly List<Actor> actors = new List<Actor>();
        readonly List<Binding> bindings = new List<Binding>();
        readonly List<Plate> plates = new List<Plate>();
        readonly Channel[] channels = new Channel[9];
        readonly List<string> collisionDiagnostics = new List<string>();
        readonly List<GameObject> models = new List<GameObject>();
        readonly Dictionary<RecoveryPiece, bool> originalMovement = new Dictionary<RecoveryPiece, bool>();
        RecoveredFlowGame game;
        Transform levelRoot;
        RuntimeWaterMechanisms bus;
        MethodInfo physicalSwitch;
        bool restoring;
        public int ObserverCount { get { return actors.Count; } }
        public int PhysicalSwitchCount { get { return plates.Count; } }
        public bool IsBusy { get; private set; }

        public static RecoveredNativeSprings Attach(RecoveredFlowGame game)
        {
            if (game == null) return null;
            RecoveredNativeSprings helper = game.GetComponent<RecoveredNativeSprings>();
            if (helper == null) helper = game.gameObject.AddComponent<RecoveredNativeSprings>();
            if (helper.levelRoot != game.PieceRoot) helper.Initialize(game);
            return helper;
        }

        void Initialize(RecoveredFlowGame current)
        {
            foreach (Actor actor in actors) GateNativeRotatingWater(actor, false);
            foreach (GameObject model in models) if (model != null) Destroy(model);
            models.Clear(); actors.Clear(); bindings.Clear(); plates.Clear(); originalMovement.Clear(); collisionDiagnostics.Clear();
            game = current; levelRoot = current.PieceRoot;
            bus = null; physicalSwitch = null;
            RefreshBus();
            for (int channel = 1; channel <= 8; channel++)
            {
                channels[channel] = new Channel { index = channel };
                Global("positionRes" + channel, 0);
                Global("activadoRes" + channel, false);
                Global("numPiezas" + channel, 0);
                Global("contPiezas" + channel, 0);
            }
            if (levelRoot == null) return;
            foreach (RecoveryPiece piece in current.Pieces)
            {
                if (piece == null || IsProxy(piece) || !piece.gameObject.activeInHierarchy) continue;
                originalMovement[piece] = piece.CanMove;
                foreach (MonoBehaviour source in piece.GetComponents<MonoBehaviour>())
                {
                    if (source == null || source is RecoveryPiece) continue;
                    string type = source.GetType().Name;
                    if (type == "Resorte")
                    {
                        // doProcesoPiezas 101B19C writes decoded st into Resorte
                        // separately from the ordinary component token dispatch.
                        string decoded = Token(piece.OriginalConfiguration, "st", "et");
                        if (!String.IsNullOrEmpty(decoded)) Set(source, "conResorte", decoded);
                        int part = UIManagerSelScript0.parte;
                        string material = part == 14 ? "materialTextura4" : part == 15 ? "materialTextura3" : part == 16 ? "materialTextura5" :
                            (part == 13 || (part >= 20 && part <= 23) || part == 32) ? "materialTextura2" : "";
                        AssignMaterial(Get(source, "objeto") as GameObject, Get(source, material) as Material);
                        GameObject body = Get(source, "objeto") as GameObject;
                        if (UIManagerSelScript.var_design == 0 && body != null) body.layer = 0; // Resorte.Start100B6DC..EC.
                        Set(source, "sphereRadius", 1f);
                        AddPlate(piece, source, Text(source, "conResorte"), 0);
                        continue;
                    }
                    if (!type.StartsWith("Control", StringComparison.Ordinal)) continue;
                    string button = Text(source, "conResorte3");
                    if (Configured(button))
                    {
                        GameObject model = Spawn(source, "prefabResorte3", Vector3.one);
                        Resorte3 plate = model == null ? null : model.GetComponent<Resorte3>();
                        if (plate != null)
                        {
                            // ControlFijo.Start10787D0..10788C4 moves the
                            // transparent floor button4.5 local units down.
                            if (source is ControlFijo && (piece.BasePrefab ?? "").IndexOf("Piezatrans1", StringComparison.Ordinal) >= 0)
                                model.transform.localPosition = new Vector3(0, -4.5f, 0);
                            plate.conResorte = button; plate.modoRes3 = Text(source, "modoRes3");
                            Set(plate, "sphereRadius", 1f); // Resorte3.ctor10295DC..E4.
                            Int32.TryParse(plate.modoRes3, out int mode);
                            AddPlate(piece, plate, button, mode);
                            PlateMaterial(plate, 1);
                        }
                    }
                    string controls = Text(source, "conResorte");
                    AddExtraObserver(piece, source);
                    if (!Configured(controls) || controls == "9") continue;
                    string sequence = Text(source, "tipoResorte");
                    if (String.IsNullOrEmpty(sequence)) continue;
                    GameObject observerObject = Spawn(source, "prefabResorte", Vector3.one);
                    ObservadorResorte observer = observerObject == null ? null : observerObject.GetComponent<ObservadorResorte>();
                    if (observer == null) continue;
                    observer.conResorte = controls; observer.tipoResorte = sequence;
                    observer.position = 0; Set(observer, "sphereRadius", 1f);
                    observer.valEncima = Text(source, "valEncima"); observer.valDebajo = Text(source, "valDebajo");
                    observer.valMove = Text(source, "valMove");
                    var actor = new Actor { piece = piece, source = source, observer = observer,
                        top = observer.valEncima, bottom = observer.valDebajo, move = observer.valMove,
                        moveSpeed = observer.moveSpeed, rotationSpeed = observer.rotationSpeed };
                    actors.Add(actor);
                    string[] sequences = sequence.Split('c');
                    for (int i = 0; i < controls.Length && i < 2 && i < sequences.Length; i++)
                    {
                        int channel = controls[i] - '0';
                        if (channel < 1 || channel > 8) continue;
                        string actions = sequences[i].Length > 3 ? sequences[i].Substring(0, 3) : sequences[i];
                        var binding = new Binding { actor = actor, channel = channel, actions = actions };
                        binding.poses.Add(piece.transform.position); binding.rotations.Add(piece.transform.rotation);
                        actor.bindings.Add(binding); bindings.Add(binding); channels[channel].bindings.Add(binding);
                        channels[channel].maximum = Mathf.Max(channels[channel].maximum, actions.Length);
                    }
                }
            }
            for (int i = 1; i <= 8; i++) Global("numPiezas" + i, channels[i].bindings.Count);
        }

        void LateUpdate()
        {
            RecoveredFlowGame current = RecoveredFlowGame.Instance;
            if (current == null) return;
            if (current != game || levelRoot != current.PieceRoot) Initialize(current);
            if (levelRoot == null || restoring) return;
            RefreshBus();
            PropertyInfo showingFinal = game.GetType().GetProperty("IsShowingFinalSolution");
            if (showingFinal != null && (bool)showingFinal.GetValue(game, null))
            {
                // Final solution geometry keeps original Resorte plates.
                // Their Update has no preview/victory-mode gate, and Total's
                // r+1.2 landing still relies on delayed doBajarPieza. Only
                // observer-driven spring motion is replaced by st9 proxies.
                UpdatePlates();
                IsBusy = false;
                for (int i = 1; i <= 8; i++) Global("activadoRes" + i, false);
                return;
            }
            UpdatePlates();
            IsBusy = false;
            for (int i = 1; i <= 8; i++) UpdateChannel(channels[i]);
            foreach (Actor actor in actors)
            {
                if (!actor.moving || actor.piece == null) continue;
                IsBusy = true;
                Transform transform = actor.piece.transform;
                if (actor.turning)
                    // Native doRotation computes speed*deltaTime/angle and
                    // passes it to SlerpUnclamped: RotateTowards, in degrees/s.
                    transform.rotation = Quaternion.RotateTowards(transform.rotation, actor.targetRotation, actor.rotationSpeed * Time.deltaTime);
                else transform.position = Vector3.MoveTowards(transform.position, actor.target, actor.moveSpeed * Time.deltaTime);
                for (int index = 0; index < actor.carried.Count; index++)
                {
                    RecoveryPiece carried = actor.carried[index];
                    if (carried == null) continue;
                    Vector3 target = transform.position + Vector3.up * Storey * (index + 1);
                    carried.transform.position = Vector3.MoveTowards(carried.transform.position, target, actor.moveSpeed * Time.deltaTime);
                }
                bool arrived = actor.turning ? Quaternion.Angle(transform.rotation, actor.targetRotation) < .01f : EachAxisClose(transform.position, actor.target);
                if (!arrived) continue;
                transform.position = actor.target; transform.rotation = actor.targetRotation;
                actor.moving = false;
                GateNativeRotatingWater(actor, false);
                RestoreMovable(actor.piece);
                actor.current.position += actor.direction;
                actor.observer.position = actor.current.position;
                Set(actor.observer, "activadoResorte", false);
                for (int index = 0; index < actor.carried.Count; index++)
                    if (actor.carried[index] != null) { actor.carried[index].transform.position = actor.target + Vector3.up * Storey * (index + 1); RestoreMovable(actor.carried[index]); }
                actor.carried.Clear();
                if (AudioManager.instance != null) AudioManager.instance.StopSound("Piedra");
            }
            for (int i = 1; i <= 8; i++) CompleteChannel(channels[i]);
            if (IsBusy) Physics.SyncTransforms();
        }

        void UpdateChannel(Channel channel)
        {
            if (channel == null || channel.bindings.Count == 0) return;
            int manager = GlobalInt("conResorte" + channel.index);
            // Native doPrevio10C8B34..54 reruns only on its channel's
            // manager/position change. Moving an unrelated player piece does
            // not retry a failed3/5 request while the intake stays wet.
            if (manager != channel.manager) { channel.manager = manager; channel.blocked = false; }
            if (manager == 3 || manager == 5)
            { channel.blocked = true; Global("activadoRes" + channel.index, false); return; }
            bool active = manager == 1;
            if (active != channel.wasActive) { channel.wasActive = active; channel.blocked = false; }
            if (channel.moving || channel.blocked) return;
            int target = active ? channel.maximum : 0;
            if (channel.position == target) { Global("activadoRes" + channel.index, false); return; }
            int direction = target > channel.position ? 1 : -1;
            foreach (Binding binding in channel.bindings)
            {
                if (binding.actor.moving) return;
                foreach (Actor other in actors) if (other.piece == binding.actor.piece && other.moving) return;
            }
            bool moved = false;
            foreach (Binding binding in channel.bindings)
            {
                int step = direction > 0 ? binding.position : binding.position - 1;
                if (step < 0 || step >= binding.actions.Length) continue;
                if (!Prepare(binding, step, direction))
                {
                    channel.blocked = true;
                    // doResorte forward10CA51C..554 and reverse10CA774..7A0
                    // publish3/5 through doChangeManager and stop this request.
                    channel.manager = direction > 0 ? 3 : 5;
                    Global("conResorte" + channel.index, channel.manager);
                    Global("activadoRes" + channel.index, false);
                    foreach (Binding cancel in channel.bindings)
                    {
                        cancel.actor.moving = false; GateNativeRotatingWater(cancel.actor, false); RestoreMovable(cancel.actor.piece);
                        foreach (RecoveryPiece carried in cancel.actor.carried) RestoreMovable(carried);
                        cancel.actor.carried.Clear();
                    }
                    return;
                }
                moved |= binding.actor.moving;
            }
            channel.direction = direction;
            channel.moving = true;
            Global("activadoRes" + channel.index, moved);
            if (moved && UIManagerScript.sonidoFuen == 1 && AudioManager.instance != null) AudioManager.instance.PlaySound("Piedra");
        }

        bool Prepare(Binding binding, int step, int direction)
        {
            Actor actor = binding.actor;
            if (actor.piece == null) return false;
            if (actor.blockedModel != null) { Destroy(actor.blockedModel); actor.blockedModel = null; }
            char action = binding.actions[step];
            Vector3 current = actor.piece.transform.position;
            Quaternion rotation = actor.piece.transform.rotation;
            Vector3 target = current;
            Quaternion targetRotation = rotation;
            if (direction < 0)
            {
                if (step >= binding.poses.Count) return false;
                // doCambioResorte captures the current pose when a different
                // channel takes control. doCalPuntoNext(comprueba=2) reverses
                // a translation from that pose; an old per-channel origin
                // would erase another channel's completed displacement.
                switch (action)
                {
                    case '1': case '2': case '3': targetRotation = binding.rotations[step]; break;
                    case '4': target += Vector3.forward * 20; break;
                    case '5': target += Vector3.back * 20; break;
                    case '6': target += Vector3.left * 20; break;
                    case '7': target += Vector3.right * 20; break;
                    case '8': target += Vector3.up * Storey; break;
                    case '9': target += Vector3.down * Storey; break;
                }
            }
            else
            {
                if (step == 0)
                {
                    // doCambioResorte captures a fresh origin when switching
                    // between two channels, preserving the other channel's pose.
                    binding.poses[0] = current; binding.rotations[0] = rotation;
                }
                switch (action)
                {
                    case '1': targetRotation = Quaternion.Euler(0, rotation.eulerAngles.y + 90, 0); break;
                    case '2': targetRotation = Quaternion.Euler(0, rotation.eulerAngles.y + 180, 0); break;
                    case '3': targetRotation = Quaternion.Euler(0, rotation.eulerAngles.y - 90, 0); break;
                    case '4': target += Vector3.back * 20; break;
                    case '5': target += Vector3.forward * 20; break;
                    case '6': target += Vector3.right * 20; break;
                    case '7': target += Vector3.left * 20; break;
                    case '8': target += Vector3.down * Storey; break;
                    case '9': target += Vector3.up * Storey; break;
                }
                while (binding.poses.Count <= step + 1) { binding.poses.Add(current); binding.rotations.Add(rotation); }
                binding.poses[step + 1] = target; binding.rotations[step + 1] = targetRotation;
            }
            actor.carried.Clear();
            bool turning = action >= '1' && action <= '3';
            if (action != '0' && (actor.top == "1" || actor.top == "2"))
            {
                bool fixedAbove=false;
                for (int level = 1; level <= 3; level++)
                {
                    bool fixedAtLevel;
                    RecoveryPiece top = NativeTopPiece(actor,current+Vector3.up*Storey*level,out fixedAtLevel);
                    fixedAbove|=fixedAtLevel;
                    if (top == null) break;
                    if (actor.top == "2" || turning || level == 3) return false;
                    actor.carried.Add(top);
                }
                // Native10CB2FC..35C tracks fixed overlaps separately from
                // counted movable pieces. A fixed roof does not prevent a
                // horizontal slide out from underneath it (gm678).
                if(fixedAbove && (action=='9' || action=='8' && direction<0)) return false;
            }
            // Native10CB05C gates the extra down probe on valDebajo==1;
            // 10CB144/10CB218 gates destination checks on valMove==1/2.
            // Missing optional flags deliberately allow authored paths through
            // decorative underground geometry (gm622 st2/sz8 actors).
            if (actor.bottom == "1" && target.y < current.y - .1f && !DownwardClear(actor, current - Vector3.up * Storey))
            { ShowBlocked(actor); return false; }
            bool checksDestination = actor.move == "1" || actor.move == "2";
            // Native10CB284..2B8 explicitly bypasses doValidarNext for
            // vertical8/9; optional valDebajo is their separate downward gate.
            // Otherwise vertically coupled actors with different sequences
            // (gm630 sz48/sz08) incorrectly block each other's current cells.
            bool checksHorizontalDestination = checksDestination && action != '8' && action != '9';
            if(checksHorizontalDestination && action!='0')
            {
                if(turning)
                {
                    // doValidarRotacion10CCF0C..7C probes X+, X-, Z+, Z-
                    // at20 world units, stopping on the first native collision.
                    foreach(Vector3 side in new[] {Vector3.right,Vector3.left,Vector3.forward,Vector3.back})
                        if(!DestinationClear(binding,target+side*20,actor.carried)) { ShowBlocked(actor); return false; }
                }
                else
                {
                    // Only horizontal valMove2 enters doValidarMovimientos0;
                    // rotation and vertical actions take earlier branches.
                    if(actor.move=="2")
                        for(int channel=1;channel<=8;channel++)
                            if(channel!=binding.channel && GlobalBool("activadoRes"+channel)) { ShowBlocked(actor); return false; }
                    if(!DestinationClear(binding,target,actor.carried)) { ShowBlocked(actor); return false; }
                    for(int level=0;level<actor.carried.Count;level++)
                        if(!DestinationClear(binding,target+Vector3.up*Storey*(level+1),actor.carried)) return false;
                }
            }
            actor.current = binding; actor.direction = direction; actor.target = target; actor.targetRotation = targetRotation;
            actor.turning = turning; actor.moving = action != '0'; actor.blocked = false;
            GateNativeRotatingWater(actor, actor.moving && actor.turning);
            if (actor.moving) { actor.piece.CanMove = false; foreach (RecoveryPiece carried in actor.carried) carried.CanMove = false; }
            if (!actor.moving) binding.position += direction;
            Set(actor.observer, "activadoResorte", actor.moving);
            actor.observer.managerResorte = direction > 0 ? 1 : 2;
            return true;
        }

        RecoveryPiece NativeTopPiece(Actor actor,Vector3 position,out bool fixedAbove)
        {
            fixedAbove=false; RecoveryPiece result=null;
            int mask=actor.observer==null?12289:actor.observer.mascaraBloque2.value;
            using(RecoveredNativeInteraction.QueryOriginalColliders(game))
            foreach(Collider collider in Physics.OverlapSphere(position,1f,mask,QueryTriggerInteraction.UseGlobal))
            {
                Transform parent=collider.transform.parent; if(parent==null) continue;
                RecoveryPiece owner=collider.GetComponentInParent<RecoveryPiece>();
                if(owner==null || owner==actor.piece || IsProxy(owner)) continue;
                // The original reads components on the collider's immediate
                // parent. Decorative nested water/plant geometry is not a
                // separate carryable piece.
                MonoBehaviour control=parent.GetComponent<ControlFijo>();
                if(control==null) control=parent.GetComponent<ControlPiezaC>();
                if(control==null) control=parent.GetComponent<ControlPieza>();
                if(control==null) control=parent.GetComponent<ControlPiezaPfuente>();
                if(control==null) continue;
                object value=Get(control,"esPiezaFija");
                if(value is bool && (bool)value) { fixedAbove=true; continue; }
                result=owner; // Native10CEF00..20 retains the last movable overlap.
            }
            return result;
        }

        bool DownwardClear(Actor actor, Vector3 position)
        {
            int mask = actor.observer == null ? 8193 : actor.observer.mascaraBloque3.value;
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
            {
                Collider[] hits = Physics.OverlapSphere(position, 1f, mask, QueryTriggerInteraction.UseGlobal);
                if (hits.Length == 0) return true;
                foreach (Collider hit in hits)
                    if (collisionDiagnostics.Count < 64) collisionDiagnostics.Add("down actor=" + actor.piece.SourceRecordIndex + " target=" + position.ToString("F3") + " hit=" + hit.name + " layer=" + hit.gameObject.layer + " mask=" + mask);
                return false;
            }
        }

        bool DestinationClear(Binding binding, Vector3 position, List<RecoveryPiece> carried)
        {
            // Native doCheckCollision 10CD6D8..10CD730 probes the actual
            // colliders with radius1 and mascaraBloque (authored12289).
            // A RecoveryPiece root is not a solid cube: underground border
            // pieces and arches can have their mesh/collider far from that root.
            int mask = binding.actor.observer == null ? 12289 : binding.actor.observer.mascaraBloque.value;
            float radius = Number(binding.actor.observer, "sphereRadius", 1f);
            if (radius <= 0) radius = 1f; // Original .ctor10CF1A8 initializes1.
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
            {
                foreach (Collider collider in Physics.OverlapSphere(position, radius, mask, QueryTriggerInteraction.UseGlobal))
                {
                    if (collider == null || !collider.gameObject.activeInHierarchy) continue;
                    RecoveryPiece other = collider.GetComponentInParent<RecoveryPiece>();
                    if (other != null && (other == binding.actor.piece || carried.Contains(other) || IsProxy(other))) continue;
                    Transform parent = collider.transform.parent;
                    if (parent != null && parent.name.IndexOf("Aguas", StringComparison.Ordinal) >= 0) continue;
                    bool sameGroup = false;
                    foreach (Binding member in channels[binding.channel].bindings)
                        if (member.actor.piece == other && Text(member.actor.source, "conResorte") == Text(binding.actor.source, "conResorte") &&
                            Text(member.actor.source, "tipoResorte") == Text(binding.actor.source, "tipoResorte")) { sameGroup = true; break; }
                    // Native10CD844..874/10CD984..99C exempts matching
                    // conResorte AND tipoResorte for the moving group.
                    if (sameGroup) continue;
                    string detail = "actor=" + binding.actor.piece.SourceRecordIndex + " target=" + position.ToString("F3") +
                        " hit=" + collider.name + " layer=" + collider.gameObject.layer + " other=" + (other == null ? -1 : other.SourceRecordIndex) + " mask=" + mask;
                    if (collisionDiagnostics.Count < 64 && !collisionDiagnostics.Contains(detail)) collisionDiagnostics.Add(detail);
                    return false;
                }
            }
            return true;
        }

        void CompleteChannel(Channel channel)
        {
            if (channel == null || !channel.moving) return;
            int complete = 0;
            foreach (Binding binding in channel.bindings) if (!binding.actor.moving) complete++;
            Global("contPiezas" + channel.index, complete);
            if (complete < channel.bindings.Count) { Global("activadoRes" + channel.index, true); return; }
            channel.position += channel.direction;
            channel.position = Mathf.Clamp(channel.position, 0, channel.maximum);
            channel.moving = false;
            Global("positionRes" + channel.index, channel.position);
            Global("contPiezas" + channel.index, 0);
            Global("activadoRes" + channel.index, false);
        }

        void AddPlate(RecoveryPiece piece, MonoBehaviour source, string controls, int mode)
        {
            if (String.IsNullOrEmpty(controls)) return;
            plates.Add(new Plate { piece = piece, source = source, channels = controls, mode = mode });
        }
        void AddExtraObserver(RecoveryPiece piece, MonoBehaviour source)
        {
            string config = Text(source, "conTipoRes3");
            if (config.Length < 2) return;
            int channel = config[0] - '0';
            if (channel < 1 || channel > 8) return;
            GameObject model = Spawn(source, "prefabResorte", Vector3.one);
            ObservadorResorte observer = model == null ? null : model.GetComponent<ObservadorResorte>();
            if (observer == null) return;
            observer.conResorte = config.Substring(0, 1); observer.tipoResorte = config.Substring(1, 1);
            observer.position = 0; Set(observer, "sphereRadius", 1f);
            observer.valEncima = Text(source, "valEncima"); observer.valDebajo = Text(source, "valDebajo"); observer.valMove = Text(source, "valMove");
            var actor = new Actor { piece = piece, source = source, observer = observer, top = observer.valEncima,
                bottom = observer.valDebajo, move = observer.valMove, moveSpeed = observer.moveSpeed, rotationSpeed = observer.rotationSpeed };
            var binding = new Binding { actor = actor, channel = channel, actions = observer.tipoResorte };
            binding.poses.Add(piece.transform.position); binding.rotations.Add(piece.transform.rotation);
            actors.Add(actor); actor.bindings.Add(binding); bindings.Add(binding); channels[channel].bindings.Add(binding);
            channels[channel].maximum = Mathf.Max(channels[channel].maximum, 1);
        }
        void UpdatePlates()
        {
            foreach (Plate plate in plates)
            {
                if (plate.source == null || plate.piece == null) continue;
                Vector3 center = plate.source.transform.position + Vector3.up * Number(plate.source, "altura", Storey);
                RecoveryPiece occupant = plate.source is Resorte ? FindOriginalPlateOccupant(plate, center) : FindOriginalToggleOccupant(plate, center);
                bool occupied = occupant != null;
                if (plate.pending && Time.time >= plate.when)
                {
                    plate.pending = false;
                    PublishPlate(plate);
                }
                if (occupied && !plate.wasOccupied)
                {
                    bool allowed = PlateAcceptsPress(plate);
                    if (allowed)
                    {
                        plate.occupant = occupant;
                        if (plate.mode == 0) plate.output1 = true;
                        else if (plate.mode == 2)
                        {
                            plate.output1 = plate.phase == 1;
                            plate.output2 = plate.phase == 3;
                            plate.phase = plate.phase % 4 + 1;
                        }
                        else { plate.output1 = !plate.output1; plate.phase = plate.output1 ? 2 : 1; }
                        Set(plate.source, "fase", plate.phase);
                        PlateMaterial(plate.source, PlateMaterialIndex(plate));
                        plate.pending = true; plate.when = Time.time + Number(plate.source, "tiempo1", .1f);
                        Animator animator = Get(plate.source, "animResorte") as Animator;
                        if (animator != null && !(plate.source is Resorte)) { animator.enabled = true; animator.SetBool("boolResorte", true); }
                        if (UIManagerScript.sonidoFuen == 1 && AudioManager.instance != null) AudioManager.instance.PlaySound("Resorte");
                    }
                }
                else if (!occupied && plate.wasOccupied && plate.mode == 0)
                {
                    plate.output1 = false;
                    plate.pending = true; plate.when = Time.time + Number(plate.source, "tiempo2", .1f);
                    Animator animator = Get(plate.source, "animResorte") as Animator;
                    if (animator != null) animator.SetBool("boolResorte", false);
                }
                plate.wasOccupied = occupied;
            }
        }

        static bool PlateAcceptsPress(Plate plate)
        {
            // Resorte3.docheckTerminado1: mode3 waits for all eight channels.
            if(plate.mode==3)
            {
                for(int channel=1;channel<=8;channel++) if(GlobalBool("activadoRes"+channel)) return false;
            }
            else if(plate.mode==2)
            {
                // Update1027FDC/1028088: phase1 waits for channel2, phase3
                // for channel1. Phase2/4 release immediately, even while busy.
                int at=plate.phase==1?1:plate.phase==3?0:-1;
                if(at>=0)
                {
                    if(plate.channels.Length<=at) return false;
                    int channel=plate.channels[at]-'0';
                    if(channel<1 || channel>8 || GlobalBool("activadoRes"+channel)) return false;
                }
            }
            return true;
        }
        static int PlateMaterialIndex(Plate plate)
        {
            // Mode2 phase cycle: pressed1(tex2), released1(tex3),
            // pressed2(tex2), released2(tex1). phase stores the next action.
            if(plate.mode==2) return plate.phase==2 || plate.phase==4?2:plate.phase==3?3:1;
            return plate.output1?2:1;
        }

        RecoveryPiece FindOriginalPlateOccupant(Plate plate, Vector3 center)
        {
            Resorte source = plate.source as Resorte;
            if (source == null) return null;
            RecoveryPiece result = null;
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
                foreach (Collider collider in Physics.OverlapSphere(center, 1f, source.mascaraSuelo.value, QueryTriggerInteraction.UseGlobal))
                {
                    if (collider.name.IndexOf("CuboResorte", StringComparison.Ordinal) >= 0) continue;
                    RecoveryPiece owner = collider.GetComponentInParent<RecoveryPiece>();
                    if (owner == null || owner == plate.piece || ReferenceEquals(Get(game, "dragging"), owner)) continue;
                    result = owner; // Native100BB18..38 retains the last overlap.
                }
            return result;
        }
        RecoveryPiece FindOriginalToggleOccupant(Plate plate, Vector3 center)
        {
            Resorte3 source = plate.source as Resorte3;
            if (source == null) return null;
            RecoveryPiece result = null;
            source.checkCollision = false;
            // Native10283BC..410 uses this serialized layer mask (4096 in
            // Resorte3.prefab) and ctor radius1, not root-center occupancy.
            using (RecoveredNativeInteraction.QueryOriginalColliders(game))
                foreach (Collider collider in Physics.OverlapSphere(center, 1f, source.mascaraBloque.value, QueryTriggerInteraction.UseGlobal))
                {
                    Transform parent = collider.transform.parent;
                    RecoveryPiece owner = collider.GetComponentInParent<RecoveryPiece>();
                    string parentName = parent != null && owner != null && parent == owner.transform ? owner.BasePrefab ?? parent.name : parent == null ? "" : parent.name;
                    source.checkCollision = !(UIManagerSelScript.var_design == 2 && parentName.IndexOf("BloqueFijo", StringComparison.Ordinal) >= 0);
                    source.otherObject = collider.gameObject;
                    source.otherObject1 = parent == null ? null : parent.gameObject;
                    source.layer_old = collider.gameObject.layer;
                    // Every iteration replaces the previous result, including
                    // the var_design2 ignored fixed hit (1028440..1028518).
                    result = source.checkCollision ? collider.GetComponentInParent<RecoveryPiece>() : null;
                }
            return result;
        }
        void PublishPlate(Plate plate)
        {
            // Resorte.Update invokes doBajarPieza after tiempo1. The original
            // native guide first lands1.2 higher; this separate delayed operation
            // lowers the pressing actor to the pressure-plate parent's Y.
            if (!restoring && plate.source is Resorte && plate.output1 && plate.occupant != null)
            {
                Animator animator = Get(plate.source, "animResorte") as Animator;
                if (animator != null) { animator.enabled = true; animator.SetBool("boolResorte", true); }
                Vector3 position = plate.occupant.transform.position;
                position.y = plate.source.transform.position.y;
                MoveBloque.fijarPieza = false; MoveBloque.MoviendoBloque = false;
                if (!RecoveredNativeDragMotion.FinishPressureLanding(plate.occupant, position))
                    plate.occupant.transform.position = position;
                Set(plate.source, "chequear", true);
                Physics.SyncTransforms();
            }
            Resorte3 toggle = plate.source as Resorte3;
            if (!restoring && toggle != null && (plate.output1 || plate.output2) && plate.occupant != null &&
                toggle.transform.parent != null && (toggle.transform.parent == plate.piece.transform ? plate.piece.BasePrefab ?? toggle.transform.parent.name : toggle.transform.parent.name).IndexOf("Piezatrans", StringComparison.Ordinal) < 0)
            {
                // doBajarPieza1028B70..C58: source-parent X/Z and sourceY+7.6.
                // Unlike Resorte.doBajarPieza, this method does not clear the
                // player's MoveBloque movement flags.
                Vector3 position = toggle.transform.parent.position;
                position.y = toggle.transform.position.y + Storey;
                plate.occupant.transform.position = position;
                toggle.valorY = position.y;
                toggle.chequear = true;
                Physics.SyncTransforms();
            }
            plate.published1 = plate.output1; plate.published2 = plate.output2;
            PublishPhysicalOutputs();
        }

        void RefreshBus()
        {
            // sceneLoaded can create the board before the network's
            // AfterSceneLoad callback. Keep the bus connection independent
            // from board initialization so a direct Play start also connects.
            RuntimeWaterMechanisms available = RecoveredWaterNetwork.Instance == null ? null :
                RecoveredWaterNetwork.Instance.GetComponent<RuntimeWaterMechanisms>();
            if (available == bus && physicalSwitch != null) return;
            bus = available;
            physicalSwitch = bus == null ? null : bus.GetType().GetMethod("SetPhysicalSwitch", BindingFlags.Instance | BindingFlags.Public);
            if (bus != null && physicalSwitch != null) PublishPhysicalOutputs();
        }

        void PublishPhysicalOutputs()
        {
            for (int channel = 1; channel <= 8; channel++)
            {
                bool active = false;
                bool defined = false;
                foreach (Plate member in plates)
                    for (int i = 0; i < member.channels.Length && i < 2; i++)
                        if (member.channels[i] - '0' == channel) { defined = true; active |= i == 0 ? member.published1 : member.published2; }
                if (!defined) continue;
                if (bus != null && physicalSwitch != null) physicalSwitch.Invoke(bus, new object[] { channel, active });
                else Global("conResorte" + channel, active ? 1 : 2);
            }
        }

        public object CaptureState()
        {
            var result = new State { actors = new ActorState[actors.Count], bindings = new BindingState[bindings.Count], channels = new ChannelState[9], plates = new PlateState[plates.Count] };
            for (int i = 0; i < actors.Count; i++)
            {
                Actor a = actors[i]; int[] carried = new int[a.carried.Count];
                for (int j = 0; j < carried.Length; j++) carried[j] = a.carried[j] == null ? -1 : a.carried[j].Index;
                result.actors[i] = new ActorState { moving = a.moving, blocked = a.blocked, turning = a.turning, currentChannel = a.current == null ? 0 : a.current.channel,
                    direction = a.direction, target = a.target, rotation = a.targetRotation, carried = carried };
            }
            for (int i = 0; i < bindings.Count; i++) result.bindings[i] = new BindingState { position = bindings[i].position, poses = bindings[i].poses.ToArray(), rotations = bindings[i].rotations.ToArray() };
            for (int i = 1; i <= 8; i++) { Channel c = channels[i]; result.channels[i] = new ChannelState { position = c.position, direction = c.direction, manager = c.manager, wasActive = c.wasActive, moving = c.moving, blocked = c.blocked }; }
            for (int i = 0; i < plates.Count; i++)
            {
                Plate p = plates[i]; var saved = new PlateState { phase = p.phase, wasOccupied = p.wasOccupied, output1 = p.output1, output2 = p.output2, published1 = p.published1, published2 = p.published2, pending = p.pending, remaining = p.when - Time.time, occupant = p.occupant == null ? -1 : p.occupant.Index };
                CapturePlateVisual(p, saved); result.plates[i] = saved;
            }
            return result;
        }

        public void RestoreState(object snapshot)
        {
            State state = snapshot as State;
            if (state == null) return;
            if (game == null || levelRoot != game.PieceRoot) Initialize(RecoveredFlowGame.Instance);
            if (state.actors.Length != actors.Count || state.bindings.Length != bindings.Count || state.plates.Length != plates.Count) return;
            restoring = true;
            foreach (var pair in originalMovement) if (pair.Key != null) pair.Key.CanMove = pair.Value;
            for (int i = 0; i < bindings.Count; i++) { Binding b = bindings[i]; BindingState s = state.bindings[i]; b.position = s.position; b.poses.Clear(); b.poses.AddRange(s.poses); b.rotations.Clear(); b.rotations.AddRange(s.rotations); }
            for (int i = 0; i < actors.Count; i++)
            {
                Actor a = actors[i]; ActorState s = state.actors[i]; a.moving = s.moving; a.blocked = s.blocked; a.turning = s.turning;
                if (a.blockedModel != null) { Destroy(a.blockedModel); a.blockedModel = null; }
                a.direction = s.direction; a.target = s.target; a.targetRotation = s.rotation; a.current = null; a.carried.Clear();
                foreach (Binding b in a.bindings) if (b.channel == s.currentChannel) a.current = b;
                foreach (int index in s.carried) { RecoveryPiece p = ByIndex(index); if (p != null) a.carried.Add(p); }
                if (a.moving) { a.piece.CanMove = false; foreach (RecoveryPiece p in a.carried) p.CanMove = false; }
            }
            for (int i = 1; i <= 8; i++) { Channel c = channels[i]; ChannelState s = state.channels[i]; c.position = s.position; c.direction = s.direction; c.manager = s.manager; c.wasActive = s.wasActive; c.moving = s.moving; c.blocked = s.blocked; Global("positionRes" + i, c.position); Global("activadoRes" + i, c.moving); }
            for (int i = 0; i < plates.Count; i++) { Plate p = plates[i]; PlateState s = state.plates[i]; p.phase = s.phase; p.wasOccupied = s.wasOccupied; p.output1 = s.published1; p.output2 = s.published2; p.pending = s.pending; p.when = Time.time + s.remaining; p.occupant = ByIndex(s.occupant); PublishPlate(p); p.output1 = s.output1; p.output2 = s.output2; Set(p.source,"fase",p.phase); PlateMaterial(p.source,PlateMaterialIndex(p)); RestorePlateVisual(p, s); }
            IsBusy = false;
            foreach (Actor actor in actors) { IsBusy |= actor.moving; GateNativeRotatingWater(actor,actor.moving && actor.turning); }
            restoring = false;
        }

        static void CapturePlateVisual(Plate plate, PlateState saved)
        {
            GameObject body = Get(plate.source, "objeto") as GameObject;
            if (body != null) { saved.hasBody = true; saved.bodyPosition = body.transform.localPosition; saved.bodyRotation = body.transform.localRotation; saved.bodyScale = body.transform.localScale; }
            Animator animator = Get(plate.source, "animResorte") as Animator;
            if (animator == null) return;
            saved.hasAnimator = true; saved.animatorEnabled = animator.enabled;
            saved.animatorPressed = animator.GetBool("boolResorte");
            if (animator.enabled) { AnimatorStateInfo info = animator.GetCurrentAnimatorStateInfo(0); saved.animatorState = info.fullPathHash; saved.animatorTime = info.normalizedTime; }
        }
        static void RestorePlateVisual(Plate plate, PlateState saved)
        {
            Animator animator = Get(plate.source, "animResorte") as Animator;
            if (saved.hasAnimator && animator != null)
            {
                animator.enabled = true;
                if (saved.animatorState == 0) animator.Rebind();
                animator.SetBool("boolResorte", saved.animatorPressed);
                if (saved.animatorState != 0) animator.Play(saved.animatorState, 0, saved.animatorTime);
                animator.Update(0);
                animator.enabled = saved.animatorEnabled;
            }
            GameObject body = Get(plate.source, "objeto") as GameObject;
            if (saved.hasBody && body != null) { body.transform.localPosition = saved.bodyPosition; body.transform.localRotation = saved.bodyRotation; body.transform.localScale = saved.bodyScale; }
        }

        static void GateNativeRotatingWater(Actor actor, bool disabled)
        {
            // Original Update10C8404..8458 gates only rotation modes1/2/3.
            // doInactivarAguas10C8D48..98 disables parent children named
            // AguaControl5C/AguaControlChorro1, not ordinary pipe water or
            // carried blocks. C.Awake constructs this exact controller family.
            if(actor==null || actor.piece==null || !(actor.source is ControlPiezaC)) return;
            RecoveredWaterNetwork water=RecoveredWaterNetwork.Instance;
            if(water==null) return;
            MethodInfo set=water.GetType().GetMethod("SetPieceMoving",BindingFlags.Instance|BindingFlags.Public);
            if(set!=null) set.Invoke(water,new object[] { actor.piece,disabled });
        }
        void OnDestroy() { foreach(Actor actor in actors) GateNativeRotatingWater(actor,false); }

        RecoveryPiece ByIndex(int index) { foreach (RecoveryPiece piece in game.Pieces) if (piece != null && piece.Index == index) return piece; return null; }
        void RestoreMovable(RecoveryPiece piece) { if (piece != null && originalMovement.TryGetValue(piece, out bool movable)) piece.CanMove = movable; }
        RecoveryPiece PieceAt(Vector3 position, RecoveryPiece ignore, float verticalTolerance)
        {
            RecoveryPiece result = null; float nearest = float.PositiveInfinity;
            foreach (RecoveryPiece piece in game.Pieces)
            {
                if (piece == null || !piece.gameObject.activeInHierarchy || piece == ignore || IsProxy(piece) || Get(game, "dragging") == (object)piece) continue;
                Vector3 delta = piece.transform.position - position;
                if (Mathf.Abs(delta.x) > 1 || Mathf.Abs(delta.z) > 1 || Mathf.Abs(delta.y) > verticalTolerance) continue;
                if (delta.sqrMagnitude < nearest) { nearest = delta.sqrMagnitude; result = piece; }
            }
            return result;
        }
        static bool IsProxy(RecoveryPiece piece) { return piece.OriginalConfiguration != null && piece.OriginalConfiguration.Contains("st9et"); }
        static bool EachAxisClose(Vector3 a, Vector3 b) { Vector3 d = a - b; return Mathf.Abs(d.x) < .01f && Mathf.Abs(d.y) < .01f && Mathf.Abs(d.z) < .01f; }
        GameObject Spawn(MonoBehaviour source, string field, Vector3 scale)
        {
            GameObject prefab = Get(source, field) as GameObject;
            if (prefab == null) return null;
            GameObject instance = Instantiate(prefab, Vector3.zero, Quaternion.identity);
            instance.transform.SetParent(source.transform, true); instance.transform.localPosition = Vector3.zero; instance.transform.localScale = scale;
            foreach (MonoBehaviour script in instance.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
            models.Add(instance); return instance;
        }
        static bool Configured(string value) { return !String.IsNullOrEmpty(value) && value != "0"; }
        static void PlateMaterial(object source, int index)
        {
            Material material = Get(source, "materialTextura" + index) as Material;
            foreach (string field in new[] { "cilinder1", "cilinder2" })
            { GameObject target = Get(source, field) as GameObject; Renderer renderer = target == null ? null : target.GetComponent<Renderer>(); if (renderer != null && material != null) renderer.sharedMaterial = material; }
        }
        void ShowBlocked(Actor actor)
        {
            GameObject prefab = actor.observer == null ? null : actor.observer.prefabBloqueado;
            if (prefab == null || actor.blockedModel != null) return;
            actor.blockedModel = Instantiate(prefab, Vector3.zero, Quaternion.Euler(-90, 0, 0));
            actor.blockedModel.transform.SetParent(actor.observer.transform, true);
            actor.blockedModel.transform.localPosition = new Vector3(0, .35f, 0);
            actor.blockedModel.transform.localScale = new Vector3(101, 101, 39);
            foreach (MonoBehaviour script in actor.blockedModel.GetComponentsInChildren<MonoBehaviour>(true)) script.enabled = false;
            foreach (Collider collider in actor.blockedModel.GetComponentsInChildren<Collider>(true)) collider.enabled = false;
        }
        static void AssignMaterial(GameObject target, Material material)
        {
            Renderer renderer = target == null ? null : target.GetComponent<Renderer>();
            if (renderer != null && material != null) renderer.sharedMaterial = material;
        }
        static object Get(object source, string name) { FieldInfo field = source == null ? null : source.GetType().GetField(name, Flags); return field == null ? null : field.GetValue(source); }
        static void Set(object source, string name, object value) { FieldInfo field = source == null ? null : source.GetType().GetField(name, Flags); if (field != null) field.SetValue(source, value); }
        static string Token(string source, string start, string end) { if (source == null) return ""; int a = source.IndexOf(start, StringComparison.Ordinal); if (a < 0) return ""; a += start.Length; int b = source.IndexOf(end, a, StringComparison.Ordinal); return b < a ? "" : source.Substring(a, b-a); }
        static string Text(object source, string name) { return Get(source, name) as string ?? ""; }
        static float Number(object source, string name, float fallback) { object value = Get(source, name); return value is float ? (float)value : fallback; }
        static void Global(string name, object value) { FieldInfo field = typeof(UIManagerScript).GetField(name, BindingFlags.Static | BindingFlags.Public); if (field != null) field.SetValue(null, value); }
        static int GlobalInt(string name) { FieldInfo field = typeof(UIManagerScript).GetField(name, BindingFlags.Static | BindingFlags.Public); return field == null ? 0 : (int)field.GetValue(null); }
        static bool GlobalBool(string name) { FieldInfo field = typeof(UIManagerScript).GetField(name, BindingFlags.Static | BindingFlags.Public); return field != null && (bool)field.GetValue(null); }
    }
}
