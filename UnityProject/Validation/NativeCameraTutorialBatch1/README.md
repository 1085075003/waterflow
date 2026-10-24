# Original camera / rotation tutorial batch 1

This immutable batch changes the two existing serialized source classes and adds one binding helper and one real Unity validation entry. Do not copy anything from CameraTutorialWork. Existing HelpRotate/SwipeHandler2 meta GUIDs are preserved.

## Required root integration (not included, because core/UI are owned by root)

1. Remove `SetActive(controller.helpRotate, false)` in `FlowWaterRecoveryUI.ConfigureOriginalScene`. Keep the original scene's HelpRotate subtree and serialized Animator/Text/Rotation references.
2. After each real `LoadLevel` board/camera creation, call `RecoveredNativeCamera.ConfigureLevel(CurrentLevelId, controller.helpRotate)` (tutorialRoot optional). This re-enables the retained SwipeHandler2 source component and binds its original EsferaCamera; initializes native state once; resets girado; applies the exact native forced-help level IDs; and shows/hides the authored tutorial by PlayerPrefs helpRotation.
3. Right-mouse orbit remains a desktop adaptation. After updating its pose, call `RecoveredNativeCamera.SynchronizeExternalOrbit(true)`. If core rotates the camera child rather than EsferaCamera, preserve that actual child pose; the helper syncs the sphere's target and marks girado so native tutorial Update dismisses it. Avoid applying the same pointer delta twice.
4. Root's source-script blanket disable must not run after ConfigureLevel. BeginRotation rejects UI GraphicRaycaster hits, any valid native movable pick, and the native MoviendoBloque && modoSolucion==0 gate, preventing camera and piece drag from consuming the same gesture.

## Production APIs for test and desktop integration

- `SwipeHandler2.BeginRotation(Vector2 pointer, bool mobile=false, Camera camera=null, int pointerId=-1)` performs the real entrada-layer raycast (ray direction*10, maxDistance1000) plus the desktop UI/movable priority gates. Returns actual admission.
- `ContinueRotation(Vector2 pointer, float mouseAxis=0)`, `EndRotation(Vector2 pointer, bool cancelled=false)`, `TickRotation(float deltaTime)` are the same paths called by production Update. Tests should yield real frames rather than inject SW/force/girado.
- `GirarDerecha()` / `GirarIzquierda()` set the original target by +/-90 and girado. They also admit interpolation for a direct desktop/public control; original native invoked these while a swipe was already admitted. SmoothSpeed stays serialized (game scenes1); do not assign arbitrary settle speed. Wait DeltaAngle <.25 or10 scaled seconds.
- `SynchronizeExternalRotation(bool markRotated=false)` rebases the absolute target after a desktop orbit.
- `HelpRotate.CheckRotation()` is production Update: only girado && checkSwipe enables original fadeOut, writes helpRotation=1, disables Rotation, clears checkSwipe.

## Native proof and intentional desktop adaptations

Swipe Start1054F1C captures EsferaCamera, zeroes rotationFinal/degree, sets entrada mask. Update1054FE4 calls only mobile handler if the native selected-piece gate allows. Native Mouse105593C is present but unused by Android Update; calling it with no touches is explicitly the desktop adaptation. No invented inertia: native active methods only add a quarter turn for flickType!=Inertia and abs(clampedForce)>requiredForce.

Mobile1055070: moved signed displacement magnitude*dt*.2*-.4*multiplicador (constants60008C/5FFE70), end length*magnitude*dt*.35 (5FFBA4); PC uses Mouse X*multiplicador and end*.5 with the original opposite sign. Both use original maxForce/threshold/flickType values. Idle interpolation is Mathf.LerpAngle(current,target,smoothSpeed*dt). Stationary/cancel semantics retain original mobile no-flick behavior.

Help Start10C3EE0 and Update10C4084 are restored. Text1/2 return20 original language branches (Evidence JSON includes native comparison VAs), English fallback, and doLanguage's Arabic character reversal. ResetTutorial rebinding only restores the authored disabled first frame when this reconstruction reuses a scene across levels.

Original ControladorJuego resets helpRotation on levels1,2,7,51,52,251,401,402,403,451,452,601,602,1051,1651; every start resets girado=false. No test flag hides the tutorial. Board validation may use the real GirarDerecha then GirarIzquierda with real interpolation to dismiss it before screenshots, preserving original camera pose.

## Validation

Compile with Unity2022.3.28f1 Roslyn runtime.rsp then editor.rsp. Unity actual execution is owned by root; not run by this agent.

Entry: `Recovered.FlowWater.Editor.FlowWaterCameraTutorialValidation.Run`
Report: Validation/CameraTutorial/camera-tutorial-validation.json

The fixture opens Portada then actual401, checks forced help and exact text, performs real +/-90 interpolation and checks actual gameplay Camera followed, verifies native dismissal/pref/Animator, external orbit target retention, selected-piece gate, real Begin/Continue/cancel with an actual entrada hit. It backs up helpRotation and other preferences, scene setup and timescale, restores source-sphere/gameplay-camera poses, then restores preferences/scenes on exit. Timeout90real seconds.

No board water/network/physics/Spring behavior changes are included.
