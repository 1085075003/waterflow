using UnityEngine;
using UnityEngine.EventSystems;
using Recovered.FlowWater;

// Restores the original 1054EFC..1055D48 input path. Mouse handling is the
// shipping unused mouse method, called as the desktop adaptation of Update.
public class SwipeHandler2 : MonoBehaviour
{
    public enum FlickType { Inertia = 0, MoveOne = 1 }
    public Transform EsferaCamera;
    public Vector3 posicionInicial, posicionFinal;
    public bool hayClick;
    public float smoothSpeed, degree, multiplicador;
    public Vector3 rotationInicial, rotationFinal;
    public GameObject getAreaRotation;
    public bool areaCorrecta;
    private int layer_mask;
    private bool inicio, permitirClick;
    private float diferenciaInicial, diferencia, nextFire;
    public float porcentajeDiferencia;
    public bool handleSwipes = true, handleFlicks = true;
    public float requiredForceForFlick = 7;
    public static bool girado;
    public FlickType flickType;
    public bool lockToClosest = true;
    public float maxForce = 15;
    private Vector3 finalPosition, startpos, endpos, oldpos;
    private float length, startTime, mouseMove1, force;
    private bool SW, finished = true;
    private bool initialized, mobileGesture;
    private Camera gestureCamera;

    public bool isSwiping1 { get { return SW || length != 0; } }
    public bool IsPointerRotating { get { return hayClick && areaCorrecta; } }
    public float NativeFlickForce { get { return force; } }

    void Start() { InitializeNativeRotation(); }
    public void InitializeNativeRotation()
    {
        if (initialized) return;
        initialized = true;
        finished = inicio = permitirClick = true;
        rotationInicial = EsferaCamera == null ? Vector3.zero : EsferaCamera.eulerAngles;
        rotationFinal = Vector3.zero;
        degree = 0;
        layer_mask = LayerMask.GetMask("entrada");
    }
    bool CanRotate()
    {
        return EsferaCamera != null && !(MoveBloque.MoviendoBloque && MoveBloque.modoSolucion == 0);
    }
    void Update()
    {
        InitializeNativeRotation();
        if (!CanRotate()) return;
        if (Input.touchCount > 0) HandleMobileSwipe();
        else HandleMouseSwipe();
    }
    bool PointerOverUI(Vector2 pointer, int pointerId)
    {
        if (EventSystem.current == null) return false;
        var hits = new System.Collections.Generic.List<RaycastResult>();
        var data = new PointerEventData(EventSystem.current) { position = pointer, pointerId = pointerId };
        EventSystem.current.RaycastAll(data, hits);
        foreach (RaycastResult hit in hits)
            if (hit.module is UnityEngine.UI.GraphicRaycaster) return true;
        return false;
    }
    public bool BeginRotation(Vector2 pointer, bool mobile = false, Camera camera = null, int pointerId = -1)
    {
        InitializeNativeRotation();
        areaCorrecta = false;
        hayClick = false;
        if (!CanRotate() || PointerOverUI(pointer, pointerId)) return false;
        gestureCamera = camera != null ? camera : Camera.main;
        if (gestureCamera == null || !gestureCamera.pixelRect.Contains(pointer)) return false;
        RecoveryPiece picked; RaycastHit pickedHit;
        RecoveredFlowGame game = RecoveredFlowGame.Instance;
        if (game != null && game.IsLevelActive && !game.IsSolutionPreview &&
            RecoveredNativeInteraction.TryPick(gestureCamera, pointer, game, out picked, out pickedHit)) return false;
        RaycastHit areaHit;
        getAreaRotation = ReturnClickedObject1(pointer, gestureCamera, out areaHit);
        if (getAreaRotation == null) return false;
        areaCorrecta = true;
        mobileGesture = mobile;
        finished = !mobile;
        startTime = Time.time;
        finalPosition = Vector3.zero;
        length = 0;
        SW = false;
        inicio = true;
        startpos = oldpos = new Vector3(pointer.x, 0, pointer.y);
        rotationInicial = EsferaCamera.eulerAngles;
        hayClick = true;
        return true;
    }
    public void ContinueRotation(Vector2 pointer, float mouseAxis = 0)
    {
        if (!CanRotate() || !hayClick || !areaCorrecta) return;
        Vector3 position = new Vector3(pointer.x, 0, pointer.y);
        if (mobileGesture)
        {
            SW = true;
            if (handleSwipes && position.x != oldpos.x)
            {
                Vector3 displacement = position - oldpos;
                float signedDistance = displacement.magnitude * Time.deltaTime * (displacement.x < 0 ? 1 : -1);
                degree = multiplicador * signedDistance * .2f * -.4f;
                EsferaCamera.eulerAngles = new Vector3(0, EsferaCamera.eulerAngles.y + degree, 0);
            }
            oldpos = position;
        }
        else
        {
            mouseMove1 = mouseAxis;
            if (handleSwipes && mouseMove1 != 0)
            {
                degree = mouseMove1 * multiplicador;
                EsferaCamera.eulerAngles = new Vector3(0, EsferaCamera.eulerAngles.y + degree, 0);
            }
        }
    }
    public void EndRotation(Vector2 pointer, bool cancelled = false)
    {
        if (!areaCorrecta || !hayClick) return;
        hayClick = false;
        if (cancelled || (mobileGesture && !SW)) { SW = false; finished = true; if (cancelled) areaCorrecta = false; return; }
        endpos = new Vector3(pointer.x, 0, pointer.y);
        finalPosition = endpos - startpos;
        float sign = finalPosition.x < 0 ? -1 : 1;
        // 1055551C=.35 mobile;10555B2C=.5 desktop, opposite signed length.
        length = finalPosition.magnitude * Time.deltaTime * sign * (mobileGesture ? .35f : -.5f);
        float elapsed = Time.time - startTime;
        float measured = length / elapsed;
        force = Mathf.Clamp(measured, -maxForce, maxForce);
        bool flick = handleFlicks && Mathf.Abs(force) > requiredForceForFlick && flickType != FlickType.Inertia;
        if (flick)
        {
            if ((mobileGesture && length < 0) || (!mobileGesture && length > 0)) GirarIzquierda();
            else GirarDerecha();
        }
        finished = true;
    }
    public void TickRotation(float deltaTime)
    {
        if (!CanRotate() || hayClick || !areaCorrecta || (mobileGesture && !SW)) return;
        EsferaCamera.eulerAngles = new Vector3(0,
            Mathf.LerpAngle(EsferaCamera.eulerAngles.y, rotationFinal.y, smoothSpeed * deltaTime), 0);
    }
    void HandleMobileSwipe()
    {
        if (permitirClick && Input.touchCount == 1)
        {
            Touch touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began) BeginRotation(touch.position, true, Camera.main, touch.fingerId);
            else if (touch.phase == TouchPhase.Moved) ContinueRotation(touch.position);
            else if (touch.phase == TouchPhase.Stationary) { SW = false; finished = true; }
            else if (touch.phase == TouchPhase.Canceled) EndRotation(touch.position, true);
            else if (touch.phase == TouchPhase.Ended) EndRotation(touch.position);
        }
        TickRotation(Time.deltaTime);
    }
    void HandleMouseSwipe()
    {
        if (Input.GetMouseButtonDown(0)) BeginRotation(Input.mousePosition);
        if (Input.GetMouseButtonUp(0)) EndRotation(Input.mousePosition);
        if (Input.GetMouseButton(0)) ContinueRotation(Input.mousePosition, Input.GetAxis("Mouse X"));
        TickRotation(Time.deltaTime);
    }
    public void GirarDerecha()
    {
        InitializeNativeRotation();
        if (!CanRotate()) return;
        rotationFinal.y += 90;
        girado = true;
        areaCorrecta = true;
        SW = true;
    }
    public void GirarIzquierda()
    {
        InitializeNativeRotation();
        if (!CanRotate()) return;
        rotationFinal.y -= 90;
        girado = true;
        areaCorrecta = true;
        SW = true;
    }
    public void SynchronizeExternalRotation(bool markRotated = false)
    {
        InitializeNativeRotation();
        if (EsferaCamera == null) return;
        rotationInicial = rotationFinal = EsferaCamera.eulerAngles;
        hayClick = false; SW = false; areaCorrecta = false; length = 0; finished = true;
        if (markRotated) girado = true;
    }
    GameObject ReturnClickedObject1(out RaycastHit hit)
    {
        return ReturnClickedObject1(Input.mousePosition, Camera.main, out hit);
    }
    GameObject ReturnClickedObject1(Vector2 pointer, Camera camera, out RaycastHit hit)
    {
        hit = default(RaycastHit);
        if (camera == null || layer_mask == 0) return null;
        Ray ray = camera.ScreenPointToRay(pointer);
        return Physics.Raycast(ray.origin, ray.direction * 10, out hit, 1000, layer_mask) &&
            hit.collider != null && hit.collider.gameObject.layer == LayerMask.NameToLayer("entrada")
            ? hit.collider.gameObject : null;
    }
}
