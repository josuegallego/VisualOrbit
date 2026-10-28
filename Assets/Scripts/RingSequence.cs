using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Define una o varias RUTAS de resolucion para los anillos (Etapa 6).
/// Cada ruta es una lista ordenada: un anillo solo puede resolverse cuando todos los
/// anteriores de SU ruta ya estan resueltos. Las rutas son independientes entre si.
///
/// - Un anillo en una sola ruta: sigue el orden de esa ruta.
/// - Un anillo en varias rutas: puede resolverse si ALGUNA de sus rutas se lo permite.
/// - Un anillo que no esta en ninguna ruta: sin restriccion y sin sonidos.
///
/// Audio:
/// - Fuera de orden -> error.
/// - Anillo resuelto en su turno -> acierto.
/// - Ruta completada -> sonido de la ruta (o el general si no tiene).
/// - Todas las rutas completadas -> sonido final (opcional).
/// </summary>
public class RingSequence : MonoBehaviour
{
    [Serializable]
    public class Route
    {
        public string name = "Ruta";
        [Tooltip("El primero se resuelve primero.")]
        public TubeRingManipulator[] order;
        [Tooltip("Opcional. Suena al completar esta ruta. Vacio = usa 'Route Complete Clip'.")]
        public AudioClip completeClip;

        public bool Contains(TubeRingManipulator ring) => Array.IndexOf(order, ring) >= 0;

        public bool IsComplete()
        {
            foreach (var r in order)
                if (r != null && !r.IsSolved) return false;
            return true;
        }

        public TubeRingManipulator NextPending()
        {
            foreach (var r in order)
                if (r != null && !r.IsSolved) return r;
            return null;
        }

        // Todos los anteriores a 'ring' dentro de esta ruta estan resueltos
        public bool Allows(TubeRingManipulator ring)
        {
            for (int i = 0; i < order.Length; i++)
            {
                if (order[i] == ring) return true;
                if (order[i] != null && !order[i].IsSolved) return false;
            }
            return false;
        }
    }

    [Header("Rutas")]
    public Route[] routes;

    [Header("Audio")]
    public AudioSource audioSource;
    [Tooltip("Suena al intentar girar un anillo fuera de orden.")]
    public AudioClip errorClip;
    [Tooltip("Suena cada vez que se resuelve un anillo en su turno.")]
    public AudioClip successClip;
    [Tooltip("Opcional. Suena al completar una ruta (si la ruta no tiene el suyo).")]
    public AudioClip routeCompleteClip;
    [Tooltip("Opcional. Suena al completar TODAS las rutas.")]
    public AudioClip allCompleteClip;
    [Range(0f, 1f)] public float volume = 1f;
    [Tooltip("Segundos minimos entre dos sonidos de error.")]
    public float errorCooldown = 1f;

    [Header("Depuracion")]
    public bool logValues = true;

    /// <summary>Se dispara cuando una ruta queda completa.</summary>
    public event Action<Route> RouteCompleted;
    /// <summary>Se dispara cuando todas las rutas estan completas.</summary>
    public event Action AllRoutesCompleted;
    /// <summary>Se dispara cuando el jugador intenta agarrar un anillo fuera de orden.</summary>
    public event Action<TubeRingManipulator> OutOfOrderAttempt;

    readonly List<TubeRingManipulator> allRings = new List<TubeRingManipulator>();
    float lastErrorTime = -999f;

    void Awake()
    {
        // Lista de anillos sin repetir (un anillo puede estar en varias rutas)
        allRings.Clear();
        foreach (var route in routes)
            foreach (var ring in route.order)
                if (ring != null && !allRings.Contains(ring))
                    allRings.Add(ring);

        foreach (var ring in allRings) ring.sequence = this;

        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            Debug.LogWarning("[RingSequence] Falta 'Audio Source': no sonara ningun audio.", this);
    }

    void OnEnable()
    {
        foreach (var ring in allRings) ring.Solved += OnRingSolved;
    }

    void OnDisable()
    {
        foreach (var ring in allRings) ring.Solved -= OnRingSolved;
    }

    void OnRingSolved(TubeRingManipulator ring)
    {
        // Rutas que contienen este anillo y que acaban de quedar completas
        Route finishedRoute = null;
        foreach (var route in routes)
        {
            if (route.Contains(ring) && route.IsComplete())
            {
                finishedRoute = route;
                RouteCompleted?.Invoke(route);
                if (logValues) Debug.Log($"[RingSequence] Ruta '{route.name}' COMPLETA", this);
                break;
            }
        }

        bool allDone = AllComplete();
        if (logValues) Debug.Log($"[RingSequence] '{ring.name}' resuelto en orden." + (allDone ? " TODAS LAS RUTAS COMPLETAS" : ""), this);

        AudioClip clip = successClip;
        if (finishedRoute != null)
            clip = finishedRoute.completeClip != null ? finishedRoute.completeClip
                 : routeCompleteClip != null ? routeCompleteClip : successClip;
        if (allDone && allCompleteClip != null) clip = allCompleteClip;

        Play(clip);
        if (allDone) AllRoutesCompleted?.Invoke();
    }

    /// <summary>True si todas las rutas estan completas.</summary>
    public bool AllComplete()
    {
        foreach (var route in routes)
            if (!route.IsComplete()) return false;
        return true;
    }

    /// <summary>True si este anillo ya puede resolverse segun alguna de sus rutas.</summary>
    public bool CanSolve(TubeRingManipulator ring)
    {
        bool inAnyRoute = false;
        foreach (var route in routes)
        {
            if (!route.Contains(ring)) continue;
            inAnyRoute = true;
            if (route.Allows(ring)) return true;
        }
        return !inAnyRoute; // sin ruta: sin restriccion
    }

    public void PlayError(TubeRingManipulator ring)
    {
        if (Time.time - lastErrorTime < errorCooldown) return;
        lastErrorTime = Time.time;
        OutOfOrderAttempt?.Invoke(ring);

        if (logValues)
        {
            foreach (var route in routes)
            {
                if (!route.Contains(ring)) continue;
                var next = route.NextPending();
                Debug.Log($"[RingSequence] '{ring.name}' fuera de orden en ruta '{route.name}'. " +
                          $"Toca primero: '{(next != null ? next.name : "-")}'", this);
            }
        }

        Play(errorClip);
    }

    void Play(AudioClip clip)
    {
        if (audioSource == null)
        {
            Debug.LogWarning("[RingSequence] No hay Audio Source asignado.", this);
            return;
        }
        if (clip == null)
        {
            Debug.LogWarning("[RingSequence] El clip que toca reproducir esta VACIO. Asigna 'Success Clip' (o el clip de la ruta).", this);
            return;
        }

        Debug.Log($"[RingSequence] Reproduciendo '{clip.name}' | mute={audioSource.mute} | " +
                  $"spatialBlend={audioSource.spatialBlend:F1} | volumen={volume:F1}", this);
        audioSource.PlayOneShot(clip, volume);
    }
}