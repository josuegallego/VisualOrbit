using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Anillo giratorio de un conducto (Etapa 6). Va en Ring_Root, junto al XR Grab Interactable.
/// Gira SOLO alrededor del eje del tubo. El eje se fija en Start() usando solo la rotacion
/// del padre (sin escala). La pose del editor es la pose RESUELTA; al iniciar se desordena.
///
/// Eje de giro (Axis Source):
///  - TubeMesh (recomendado): la direccion MAS LARGA de la malla del tubo. No depende de como
///    esten rotados Tube_Root, el conducto o el anillo, solo de donde esta el tubo de verdad.
///  - RingMesh: la dimension mas pequena de la malla del anillo (solo vale si el anillo es mas
///    ancho que grueso).
///  - Manual: Axis Reference + Axis Choice.
///
/// CORRECCION: si XRI desparenta el anillo al agarrarlo, el script lo devuelve a su padre
/// original (misma posicion, escala y rotacion local) para que el eje no se desvie.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class TubeRingManipulator : MonoBehaviour
{
    public enum AxisChoice { Z, Y, X }
    public enum AxisSource { TubeMesh, RingMesh, Manual }

    [Header("Eje de giro")]
    [Tooltip("TubeMesh: eje = direccion mas larga de la malla del tubo (recomendado).")]
    public AxisSource axisSource = AxisSource.TubeMesh;
    [Tooltip("Tube_Root DE ESTE TUBO (no el de otro). Su malla se usa en modo TubeMesh.")]
    public Transform axisReference;
    [Tooltip("Solo en modo Manual.")]
    public AxisChoice axisChoice = AxisChoice.Z;
    public bool invertAxis = false;

    [Header("Orden de resolucion")]
    public RingSequence sequence;

    [Header("Alineacion")]
    [Range(1f, 90f)] public float toleranceDegrees = 25f;
    [Min(0.05f)] public float holdSeconds = 0.5f;
    [Min(1)] public int symmetryCount = 1;
    public float snapAssistDegrees = 40f;
    public float snapSpeedDegPerSec = 180f;
    public bool lockWhenSolved = true;

    [Header("Estabilidad del agarre")]
    [Tooltip("Tras agarrar, el punto de agarre puede desplazarse un instante: durante este tiempo solo se toma como referencia.")]
    public float grabSettleSeconds = 0.15f;
    [Tooltip("Si en un solo fotograma el giro supera estos grados, se considera un salto del punto de agarre y se ignora.")]
    public float maxDegreesPerFrame = 35f;

    [Header("Desordenar al iniciar")]
    public bool scrambleAtStart = true;
    public float scrambleMin = 60f;
    public float scrambleMax = 170f;

    [Header("Al resolver (opcional)")]
    public GlucoseFlowController flowToActivate;

    [Header("Depuracion")]
    public bool logValues = true;

    public event Action<TubeRingManipulator> Solved;

    public bool IsSolved { get; private set; }
    public float ErrorDegrees { get; private set; }

    XRBaseInteractable interactable;
    Renderer tubeRenderer;
    Renderer ringRenderer;
    Quaternion solvedLocalRotation;
    Vector3 axisLocal = Vector3.forward;   // eje en el marco de rotacion del padre
    float offset;
    Vector3 prevVec;
    bool hasPrev;
    bool wasSelecting;
    bool blockedGrab;
    bool snapping;
    float snapTarget;
    float alignedTime;
    float grabStartTime;
    bool axisFixed;

    // Padre original y pose local, para recuperarlos si XRI desparenta el anillo
    Transform homeParent;
    Vector3 homeLocalPos;
    Vector3 homeLocalScale;

    float Step => 360f / Mathf.Max(1, symmetryCount);
    bool IsMyTurn => sequence == null || sequence.CanSolve(this);

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        solvedLocalRotation = transform.localRotation;

        // Guardar el padre y la pose local originales
        homeParent = transform.parent;
        homeLocalPos = transform.localPosition;
        homeLocalScale = transform.localScale;

        // El anillo lo mueve SOLO este script. Si el XR Grab Interactable sigue el movimiento
        // de la mano, el anillo salta hacia la mano al agarrarlo.
        if (interactable is XRGrabInteractable grab)
        {
            if (logValues && (grab.trackPosition || grab.trackRotation))
                Debug.LogWarning($"[TubeRing] '{name}': el XR Grab Interactable tenia Track Position={grab.trackPosition} " +
                                 $"y Track Rotation={grab.trackRotation}. Los desactivo por codigo.", this);

            grab.trackPosition = false;
            grab.trackRotation = false;
        }

        if (axisReference == null && axisSource != AxisSource.RingMesh)
            Debug.LogWarning("[TubeRing] 'Axis Reference' esta vacio: asigna el Tube_Root de este tubo.", this);
    }

    void Start()
    {
        FixAxis();
        axisFixed = true;
        if (logValues) LogAxisReport();
        if (scrambleAtStart) Scramble();
    }

    void Update()
    {
        // Si XRI desparento el anillo, lo devolvemos antes de hacer cualquier calculo
        KeepParent();

        if (IsSolved) return;

        var selecting = interactable.firstInteractorSelecting;

        if (selecting != null)
        {
            snapping = false;

            if (!IsMyTurn)
            {
                if (!wasSelecting && sequence != null) sequence.PlayError(this);
                hasPrev = false;
                blockedGrab = true;
                wasSelecting = true;
                alignedTime = 0f;
                return;
            }

            if (!wasSelecting && logValues)
            {
                float drift = Vector3.Angle(AxisWorld(), RawAxisWorld());
                Debug.Log($"[TubeRing] '{name}' AGARRADO | desvio del eje = {drift:F1} grados | " +
                          $"rotacion local = {transform.localEulerAngles} | posicion local = {transform.localPosition}", this);
            }

            blockedGrab = false;
            RotateWithHand(selecting);
        }
        else
        {
            hasPrev = false;
            if (wasSelecting && !blockedGrab) OnReleased();
            blockedGrab = false;
            if (snapping) SnapStep();
        }
        wasSelecting = selecting != null;

        UpdateAlignment();
    }

    // XRI puede volver a desparentar el anillo despues de Update: lo corregimos tambien aqui
    void LateUpdate()
    {
        KeepParent();
    }

    public void SetToleranceDegrees(float degrees)
    {
        toleranceDegrees = Mathf.Clamp(degrees, 1f, 90f);
    }

    // ------------------------------------------------------------ Padre

    // Si el anillo dejo de ser hijo de su padre original (lo hace XRI al agarrar),
    // lo devuelve con su posicion y escala locales y reaplica la rotacion
    void KeepParent()
    {
        if (!axisFixed) return;
        if (homeParent == null || transform.parent == homeParent) return;

        transform.SetParent(homeParent, false);
        transform.localPosition = homeLocalPos;
        transform.localScale = homeLocalScale;
        ApplyRotation();
    }

    // ------------------------------------------------------------ Eje

    Renderer TubeRenderer()
    {
        if (tubeRenderer == null && axisReference != null)
            tubeRenderer = axisReference.GetComponentInChildren<Renderer>();
        return tubeRenderer;
    }

    Renderer RingRenderer()
    {
        if (ringRenderer == null) ringRenderer = GetComponentInChildren<Renderer>();
        return ringRenderer;
    }

    // Direccion mas larga (o mas corta) de la malla de un renderer, en coordenadas de mundo
    static bool TryMeshAxis(Renderer r, bool longest, out Vector3 axisWorld)
    {
        axisWorld = Vector3.forward;
        if (r == null) return false;

        var filter = r.GetComponent<MeshFilter>();
        if (filter == null || filter.sharedMesh == null) return false;

        Transform t = r.transform;
        Vector3 s = t.lossyScale;
        Vector3 size = Vector3.Scale(filter.sharedMesh.bounds.size,
                                     new Vector3(Mathf.Abs(s.x), Mathf.Abs(s.y), Mathf.Abs(s.z)));

        int idx = 0;
        if (longest)
        {
            if (size.y > size[idx]) idx = 1;
            if (size.z > size[idx]) idx = 2;
        }
        else
        {
            if (size.y < size[idx]) idx = 1;
            if (size.z < size[idx]) idx = 2;
        }

        Vector3 local = idx == 0 ? Vector3.right : idx == 1 ? Vector3.up : Vector3.forward;
        axisWorld = t.TransformDirection(local).normalized;
        return true;
    }

    // Eje de giro en mundo, segun Axis Source
    Vector3 RawAxisWorld()
    {
        Vector3 a = Vector3.forward;
        bool ok = false;

        if (axisSource == AxisSource.TubeMesh) ok = TryMeshAxis(TubeRenderer(), true, out a);
        else if (axisSource == AxisSource.RingMesh) ok = TryMeshAxis(RingRenderer(), false, out a);

        if (!ok)
        {
            Transform t = axisReference != null ? axisReference
                        : (transform.parent != null ? transform.parent : transform);

            a = axisChoice == AxisChoice.Y ? t.up
              : axisChoice == AxisChoice.X ? t.right
              : t.forward;
        }

        return invertAxis ? -a : a;
    }

    // Fija el eje expresado solo con la rotacion del padre (sin escala)
    void FixAxis()
    {
        Quaternion parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        axisLocal = (Quaternion.Inverse(parentRot) * RawAxisWorld()).normalized;
    }

    // Eje en mundo derivado del fijado
    Vector3 AxisWorld()
    {
        Quaternion parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
        return parentRot * axisLocal;
    }

    static float AxisAngle(Vector3 a, Vector3 b)
    {
        float x = Vector3.Angle(a, b);
        return Mathf.Min(x, 180f - x);   // un eje no tiene sentido: 0 y 180 son el mismo
    }

    // Informe al iniciar: con que direcciones coincide el eje usado
    void LogAxisReport()
    {
        Vector3 used = AxisWorld();
        string withRoot = axisReference != null ? $"{AxisAngle(used, axisReference.forward):F0}" : "-";

        string withTube = TryMeshAxis(TubeRenderer(), true, out Vector3 tubeA) ? $"{AxisAngle(used, tubeA):F0}" : "-";
        string withRing = TryMeshAxis(RingRenderer(), false, out Vector3 ringA) ? $"{AxisAngle(used, ringA):F0}" : "-";

        Debug.Log($"[TubeRing] '{name}' eje fijado ({axisSource}) | difiere (grados) de: " +
                  $"Tube_Root.forward = {withRoot} | malla del tubo = {withTube} | malla del anillo = {withRing}", this);
    }

    // ------------------------------------------------------------ Giro

    void RotateWithHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor selecting)
    {
        Transform attach = selecting.GetAttachTransform(interactable);
        Vector3 axis = AxisWorld();
        Vector3 v = Vector3.ProjectOnPlane(attach.position - transform.position, axis);

        if (v.sqrMagnitude < 0.0004f) return;

        if (!hasPrev)
        {
            prevVec = v;
            hasPrev = true;
            grabStartTime = Time.time;
            return;
        }

        // Justo despues de agarrar el punto de agarre puede moverse: solo se usa como referencia
        if (Time.time - grabStartTime < grabSettleSeconds)
        {
            prevVec = v;
            return;
        }

        float delta = Vector3.SignedAngle(prevVec, v, axis);
        prevVec = v;

        // Un giro enorme en un solo fotograma es un salto del punto de agarre, no la mano
        if (Mathf.Abs(delta) > maxDegreesPerFrame)
        {
            if (logValues) Debug.Log($"[TubeRing] salto de {delta:F0} grados ignorado (el punto de agarre se movio de golpe)", this);
            return;
        }

        offset += delta;
        ApplyRotation();
    }

    void ApplyRotation()
    {
        if (float.IsNaN(offset) || float.IsInfinity(offset)) offset = 0f;
        if (axisLocal.sqrMagnitude < 0.0001f) return;

        transform.localRotation = Quaternion.AngleAxis(offset, axisLocal) * solvedLocalRotation;
    }

    // ------------------------------------------------------------ Ajuste al soltar

    void OnReleased()
    {
        float error = Mathf.Abs(SignedDelta(offset));
        if (logValues) Debug.Log($"[TubeRing] soltado | error = {error:F1} grados (tolerancia {toleranceDegrees:F0}) | " +
                                 $"rotacion local = {transform.localEulerAngles} | posicion local = {transform.localPosition}", this);

        if (error <= snapAssistDegrees)
        {
            snapTarget = offset - SignedDelta(offset);
            snapping = true;
        }
    }

    void SnapStep()
    {
        offset = Mathf.MoveTowards(offset, snapTarget, snapSpeedDegPerSec * Time.deltaTime);
        ApplyRotation();
        if (Mathf.Approximately(offset, snapTarget)) snapping = false;
    }

    // ------------------------------------------------------------ Alineacion

    float SignedDelta(float angle)
    {
        return Mathf.Repeat(angle + Step * 0.5f, Step) - Step * 0.5f;
    }

    void UpdateAlignment()
    {
        ErrorDegrees = Mathf.Abs(SignedDelta(offset));

        if (!IsMyTurn)
        {
            alignedTime = 0f;
            return;
        }

        if (ErrorDegrees <= toleranceDegrees)
        {
            alignedTime += Time.deltaTime;
            if (alignedTime >= holdSeconds) Solve();
        }
        else
        {
            alignedTime = 0f;
        }
    }

    void Solve()
    {
        if (IsSolved) return;
        IsSolved = true;

        offset -= SignedDelta(offset);
        ApplyRotation();

        if (lockWhenSolved) interactable.enabled = false;

        if (logValues) Debug.Log("[TubeRing] RESUELTO", this);

        Solved?.Invoke(this);
        if (flowToActivate != null) flowToActivate.SetFlow(true);
    }

    // ------------------------------------------------------------ Desorden inicial

    void Scramble()
    {
        for (int i = 0; i < 30; i++)
        {
            float candidate = UnityEngine.Random.Range(scrambleMin, scrambleMax)
                              * (UnityEngine.Random.value < 0.5f ? -1f : 1f);

            if (Mathf.Abs(SignedDelta(candidate)) > toleranceDegrees * 1.5f)
            {
                offset = candidate;
                break;
            }
        }

        if (Mathf.Approximately(offset, 0f))
            Debug.LogWarning("[TubeRing] No se pudo desordenar el anillo: revisa Scramble Min/Max, Tolerance y Symmetry Count.", this);

        ApplyRotation();
    }

    // ------------------------------------------------------------ Gizmo

    // Al seleccionar el anillo en la Scene view:
    //   cian    = eje que usa el script para girar (debe atravesar el agujero del anillo)
    //   amarillo = direccion mas larga de la malla del tubo
    //   magenta = dimension mas pequena de la malla del anillo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 used = Application.isPlaying ? AxisWorld() : RawAxisWorld();
        Gizmos.DrawLine(transform.position - used * 1.5f, transform.position + used * 1.5f);

        if (TryMeshAxis(TubeRenderer(), true, out Vector3 tubeA))
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position - tubeA * 1.2f, transform.position + tubeA * 1.2f);
        }

        if (TryMeshAxis(RingRenderer(), false, out Vector3 ringA))
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawLine(transform.position - ringA * 0.9f, transform.position + ringA * 0.9f);
        }
    }
}