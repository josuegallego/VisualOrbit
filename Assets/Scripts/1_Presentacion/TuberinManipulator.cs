using System;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Anillo giratorio de un conducto (Etapa 6). Va en Ring_Root, junto al XR Grab Interactable.
///
/// - Mientras el anillo esta agarrado, gira SOLO alrededor del eje del tubo,
///   siguiendo el movimiento de la mano alrededor de ese eje (como girar una rueda).
/// - La pose del anillo en el EDITOR es la pose RESUELTA. Al iniciar se desordena.
/// - Se considera resuelto cuando el error angular se mantiene dentro de la tolerancia
///   durante 'holdSeconds' (evita activaciones accidentales). Se resuelve una sola vez.
/// - Al soltar cerca de una posicion valida, el anillo se acerca solo (ayuda de ajuste).
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class TubeRingManipulator : MonoBehaviour
{
    [Header("Eje del tubo")]
    [Tooltip("Tube_Root: su eje Z debe ser el eje largo del tubo.")]
    public Transform axisReference;

    [Header("Alineacion")]
    [Tooltip("Error maximo (grados) para contar como alineado. Mas adelante lo fijara la dificultad.")]
    [Range(1f, 90f)] public float toleranceDegrees = 25f;
    [Tooltip("Segundos que debe mantenerse alineado para resolver.")]
    [Min(0.05f)] public float holdSeconds = 0.5f;
    [Tooltip("Veces que el dibujo del anillo se repite en 360 grados. 1 = una sola posicion valida.")]
    [Min(1)] public int symmetryCount = 1;
    [Tooltip("Al soltar, si esta a menos de estos grados de una posicion valida, el anillo se acerca solo.")]
    public float snapAssistDegrees = 40f;
    public float snapSpeedDegPerSec = 180f;
    [Tooltip("Al resolver, desactiva la interaccion con este anillo.")]
    public bool lockWhenSolved = true;

    [Header("Desordenar al iniciar")]
    public bool scrambleAtStart = true;
    public float scrambleMin = 60f;
    public float scrambleMax = 170f;

    [Header("Al resolver (opcional)")]
    [Tooltip("Flujo de glucosa que se enciende al resolver este anillo.")]
    public GlucoseFlowController flowToActivate;

    [Header("Depuracion")]
    public bool logValues = true;

    /// <summary>Se dispara UNA vez, cuando el anillo queda resuelto.</summary>
    public event Action<TubeRingManipulator> Solved;

    public bool IsSolved { get; private set; }
    public float ErrorDegrees { get; private set; }

    XRBaseInteractable interactable;
    Quaternion solvedLocalRotation;
    float offset;          // grados girados respecto a la pose resuelta
    Vector3 prevVec;
    bool hasPrev;
    bool wasSelecting;
    bool snapping;
    float snapTarget;
    float alignedTime;

    float Step => 360f / Mathf.Max(1, symmetryCount);

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        solvedLocalRotation = transform.localRotation;

        if (axisReference == null)
            Debug.LogWarning("[TubeRing] 'Axis Reference' esta vacio: asigna Tube_Root.", this);
    }

    void Start()
    {
        if (scrambleAtStart) Scramble();
    }

    void Update()
    {
        if (IsSolved) return;

        var selecting = interactable.firstInteractorSelecting;

        if (selecting != null)
        {
            snapping = false;
            RotateWithHand(selecting);
        }
        else
        {
            hasPrev = false;
            if (wasSelecting) OnReleased();
            if (snapping) SnapStep();
        }
        wasSelecting = selecting != null;

        UpdateAlignment();
    }

    /// <summary>Para que la dificultad (Etapa 7) pueda fijar la tolerancia.</summary>
    public void SetToleranceDegrees(float degrees)
    {
        toleranceDegrees = Mathf.Clamp(degrees, 1f, 90f);
    }

    // ------------------------------------------------------------ Giro

    void RotateWithHand(UnityEngine.XR.Interaction.Toolkit.Interactors.IXRSelectInteractor selecting)
    {
        Transform attach = selecting.GetAttachTransform(interactable);
        Vector3 axis = AxisWorld();
        Vector3 v = Vector3.ProjectOnPlane(attach.position - transform.position, axis);

        if (v.sqrMagnitude < 0.0004f) return;          // mano casi sobre el eje (< 2 cm)

        if (!hasPrev)
        {
            prevVec = v;
            hasPrev = true;
            return;
        }

        float delta = Vector3.SignedAngle(prevVec, v, axis);
        prevVec = v;
        offset += delta;
        ApplyRotation();
    }

    void ApplyRotation()
    {
        Transform parent = transform.parent;
        Vector3 axisWorld = AxisWorld();
        Vector3 axisInParent = parent != null ? parent.InverseTransformDirection(axisWorld) : axisWorld;
        transform.localRotation = Quaternion.AngleAxis(offset, axisInParent) * solvedLocalRotation;
    }

    Vector3 AxisWorld()
    {
        if (axisReference != null) return axisReference.forward;
        return transform.parent != null ? transform.parent.forward : Vector3.forward;
    }

    // ------------------------------------------------------------ Ajuste al soltar

    void OnReleased()
    {
        float error = Mathf.Abs(SignedDelta(offset));
        if (logValues) Debug.Log($"[TubeRing] soltado | error = {error:F1} grados (tolerancia {toleranceDegrees:F0})", this);

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

    // Diferencia con la posicion valida mas cercana, en el rango (-Step/2, Step/2]
    float SignedDelta(float angle)
    {
        return Mathf.Repeat(angle + Step * 0.5f, Step) - Step * 0.5f;
    }

    void UpdateAlignment()
    {
        ErrorDegrees = Mathf.Abs(SignedDelta(offset));

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

        offset -= SignedDelta(offset);   // queda exacto en la posicion valida
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

            // Que quede claramente fuera de la tolerancia
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
}