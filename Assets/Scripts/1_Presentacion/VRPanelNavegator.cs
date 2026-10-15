using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Hands;

/// <summary>
/// Navegador de paneles dentro de un mismo Canvas para VR.
///
/// Cada panel tiene SUS PROPIOS botones (Continuar, Atrás, Ir a X, Finalizar)
/// que llaman a los métodos públicos de este componente desde su onClick
/// en el Inspector. No hay un botón único compartido.
///
/// Opcionalmente, cerrar el puño con cualquier mano (XR Hands) avanza al
/// siguiente panel (útil como gesto de "siguiente" rápido).
/// </summary>
public class VRPanelNavigator : MonoBehaviour
{
    [Header("Paneles (en orden)")]
    [SerializeField] private List<GameObject> panels = new List<GameObject>();
    [SerializeField] private int startIndex = 0;

    [Header("Transición")]
    [SerializeField] private float fadeDuration = 0.25f;
    [Tooltip("Tiempo mínimo entre cambios para evitar dobles activaciones.")]
    [SerializeField] private float minTimeBetweenAdvances = 0.6f;

    [Header("Gesto de puño (opcional, XR Hands)")]
    [Tooltip("Si está activo, cerrar el puño avanza al siguiente panel.")]
    [SerializeField] private bool enableFistGesture = false;
    [Tooltip("Distancia (m) entre punta del dedo y palma para considerar el dedo cerrado.")]
    [SerializeField] private float fingerCurlDistance = 0.06f;
    [Tooltip("Dedos cerrados necesarios (índice, medio, anular, meñique).")]
    [Range(1, 4)]
    [SerializeField] private int minCurledFingers = 4;
    [Tooltip("Segundos que debe mantenerse el puño para activarse.")]
    [SerializeField] private float fistHoldTime = 0.3f;

    [Header("Eventos")]
    public UnityEvent<int> onPanelChanged;   // índice del nuevo panel
    public UnityEvent onSequenceFinished;    // al llamar a Finish() o pasar del último

    // ---------------------------------------------------------------- Estado interno

    private class HandState
    {
        public float holdTimer;
        public bool armed = true;
        public void Reset() { holdTimer = 0f; armed = true; }
    }

    private static readonly XRHandJointID[] FingerTips =
    {
        XRHandJointID.IndexTip,
        XRHandJointID.MiddleTip,
        XRHandJointID.RingTip,
        XRHandJointID.LittleTip
    };

    private readonly HandState leftState = new HandState();
    private readonly HandState rightState = new HandState();
    private readonly List<XRHandSubsystem> handSubsystems = new List<XRHandSubsystem>();

    private XRHandSubsystem handSubsystem;
    private int currentIndex = -1;
    private bool isTransitioning;
    private float lastAdvanceTime = -999f;

    public int CurrentIndex => currentIndex;
    public int PanelCount => panels.Count;

    // ---------------------------------------------------------------- Ciclo de vida

    private void Start()
    {
        if (panels.Count == 0)
        {
            Debug.LogWarning("[VRPanelNavigator] No hay paneles asignados.", this);
            return;
        }

        foreach (var p in panels)
        {
            if (p != null && p.GetComponent<CanvasGroup>() == null)
                p.AddComponent<CanvasGroup>();
        }

        ShowInstant(Mathf.Clamp(startIndex, 0, panels.Count - 1));
    }

    private void Update()
    {
        if (!enableFistGesture || panels.Count == 0) return;
        if (!EnsureHandSubsystem()) return;

        UpdateHand(handSubsystem.leftHand, leftState);
        UpdateHand(handSubsystem.rightHand, rightState);
    }

    // ---------------------------------------------------------------- API pública

    /// <summary>Avanza al siguiente panel. Si ya es el último, dispara onSequenceFinished.</summary>
    public void Next()
    {
        if (!CanAdvance()) return;

        lastAdvanceTime = Time.unscaledTime;

        if (currentIndex >= panels.Count - 1)
        {
            onSequenceFinished?.Invoke();
            return;
        }

        StartCoroutine(SwitchTo(currentIndex + 1));
    }

    /// <summary>Regresa al panel anterior.</summary>
    public void Previous()
    {
        if (!CanAdvance() || currentIndex <= 0) return;

        lastAdvanceTime = Time.unscaledTime;
        StartCoroutine(SwitchTo(currentIndex - 1));
    }

    /// <summary>Salta directamente a un panel (0-based).</summary>
    public void GoTo(int index)
    {
        if (panels.Count == 0 || isTransitioning) return;
        index = Mathf.Clamp(index, 0, panels.Count - 1);
        if (index == currentIndex) return;
        if (Time.unscaledTime - lastAdvanceTime < minTimeBetweenAdvances) return;

        lastAdvanceTime = Time.unscaledTime;
        StartCoroutine(SwitchTo(index));
    }

    /// <summary>Dispara onSequenceFinished sin cambiar de panel.</summary>
    public void Finish()
    {
        onSequenceFinished?.Invoke();
    }

    // ---------------------------------------------------------------- Interno

    private bool CanAdvance()
    {
        if (panels.Count == 0 || isTransitioning) return false;
        if (Time.unscaledTime - lastAdvanceTime < minTimeBetweenAdvances) return false;
        return true;
    }

    private void ShowInstant(int index)
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == null) continue;
            bool active = i == index;
            panels[i].SetActive(active);
            var cg = panels[i].GetComponent<CanvasGroup>();
            if (cg != null) cg.alpha = active ? 1f : 0f;
        }

        currentIndex = index;
        onPanelChanged?.Invoke(currentIndex);
    }

    private IEnumerator SwitchTo(int newIndex)
    {
        isTransitioning = true;

        GameObject current = panels[currentIndex];
        GameObject next = panels[newIndex];

        // Fade out del panel actual
        if (current != null)
        {
            var cgOut = current.GetComponent<CanvasGroup>();
            yield return Fade(cgOut, 1f, 0f);
            current.SetActive(false);
        }

        // Fade in del nuevo panel
        if (next != null)
        {
            var cgIn = next.GetComponent<CanvasGroup>();
            cgIn.alpha = 0f;
            next.SetActive(true);
            yield return Fade(cgIn, 0f, 1f);
        }

        currentIndex = newIndex;
        isTransitioning = false;

        onPanelChanged?.Invoke(currentIndex);
    }

    private IEnumerator Fade(CanvasGroup group, float from, float to)
    {
        if (group == null) yield break;

        if (fadeDuration <= 0f)
        {
            group.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;
            group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
            yield return null;
        }

        group.alpha = to;
    }

    // ---------------------------------------------------------------- Gesto de puño

    private bool EnsureHandSubsystem()
    {
        if (handSubsystem != null && handSubsystem.running) return true;

        handSubsystems.Clear();
        SubsystemManager.GetSubsystems(handSubsystems);

        foreach (var s in handSubsystems)
        {
            if (s.running)
            {
                handSubsystem = s;
                return true;
            }
        }

        handSubsystem = null;
        return false;
    }

    private void UpdateHand(XRHand hand, HandState state)
    {
        if (!hand.isTracked)
        {
            state.Reset();
            return;
        }

        if (!IsFist(hand))
        {
            state.holdTimer = 0f;
            state.armed = true;
            return;
        }

        if (!state.armed) return;

        state.holdTimer += Time.unscaledDeltaTime;

        if (state.holdTimer >= fistHoldTime)
        {
            state.armed = false;
            state.holdTimer = 0f;
            Next();
        }
    }

    private bool IsFist(XRHand hand)
    {
        if (!hand.GetJoint(XRHandJointID.Palm).TryGetPose(out Pose palm))
            return false;

        int curled = 0;

        foreach (var id in FingerTips)
        {
            if (hand.GetJoint(id).TryGetPose(out Pose tip) &&
                Vector3.Distance(tip.position, palm.position) < fingerCurlDistance)
            {
                curled++;
            }
        }

        return curled >= minCurledFingers;
    }
}