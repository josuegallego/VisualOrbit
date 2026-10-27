using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Cuenta cuantas neuronas estan encendidas sobre el total y avisa cuando cambia.
/// Base para la iluminacion progresiva y, mas adelante, para el fin del minijuego.
///
/// Ponlo en un objeto vacio FUERA de NeuralNetwork (por ejemplo "EnvironmentController").
/// </summary>
public class NeuronProgress : MonoBehaviour
{
    [Header("Neuronas")]
    [Tooltip("Si esta activo, busca todas las NeuronController de la escena (tambien las inactivas).")]
    public bool autoFind = true;
    [Tooltip("Lista manual. Se usa si 'Auto Find' esta desactivado.")]
    public List<NeuronController> neurons = new List<NeuronController>();

    [Header("Depuracion")]
    public bool logEvents = true;

    public int Total => neurons.Count;
    public int LitCount { get; private set; }
    public float Progress01 => Total == 0 ? 0f : (float)LitCount / Total;
    public bool IsComplete => Total > 0 && LitCount >= Total;

    /// <summary>Se dispara cuando cambia el numero de neuronas encendidas. Parametro: progreso 0-1.</summary>
    public event Action<float> ProgressChanged;

    void Awake()
    {
        if (autoFind)
        {
            neurons.Clear();
            neurons.AddRange(FindObjectsByType<NeuronController>(FindObjectsInactive.Include, FindObjectsSortMode.None));
        }

        foreach (var n in neurons)
            if (n != null) n.LitChanged += OnNeuronLitChanged;

        if (logEvents) Debug.Log($"[NeuronProgress] {Total} neuronas registradas.", this);
        Recount();
    }

    void OnDestroy()
    {
        foreach (var n in neurons)
            if (n != null) n.LitChanged -= OnNeuronLitChanged;
    }

    void OnNeuronLitChanged(bool lit)
    {
        Recount();
    }

    void Recount()
    {
        int count = 0;
        foreach (var n in neurons)
            if (n != null && n.IsLit) count++;

        if (count == LitCount) return;

        LitCount = count;
        if (logEvents) Debug.Log($"[NeuronProgress] {LitCount}/{Total} neuronas encendidas (progreso {Progress01:F2})", this);
        ProgressChanged?.Invoke(Progress01);
    }
}