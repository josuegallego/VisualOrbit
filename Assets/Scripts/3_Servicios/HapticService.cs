using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Servicio de vibracion de los controles (HU 3.3.3).
///   HapticService.Success();  -> dos toques cortos y suaves, que suben ("tic-tic"): acierto.
///   HapticService.Error();    -> un zumbido largo y fuerte: error.
/// Son muy distintos a proposito para reconocerlos sin mirar. Se mandan a los dos controles.
/// Los valores se pueden cambiar por codigo (campos estaticos) si hace falta afinarlos en las gafas.
/// </summary>
public static class HapticService
{
    // Acierto: dos toques cortos, el segundo un poco mas fuerte
    public static float successAmp1 = 0.35f, successDur1 = 0.07f;
    public static float successAmp2 = 0.65f, successDur2 = 0.10f;
    public static float successGap = 0.07f;

    // Error: un zumbido largo y fuerte
    public static float errorAmp = 1.0f, errorDur = 0.45f;

    static HapticRunner runner;
    static readonly List<UnityEngine.XR.InputDevice> devices = new List<UnityEngine.XR.InputDevice>();

    public static void Success()
    {
        Run(Pattern(
            new Step(successAmp1, successDur1, successGap),
            new Step(successAmp2, successDur2, 0f)));
    }

    public static void Error()
    {
        Run(Pattern(new Step(errorAmp, errorDur, 0f)));
    }

    // ------------------------------------------------------------ interno

    struct Step
    {
        public float amp, dur, gapAfter;
        public Step(float a, float d, float g) { amp = a; dur = d; gapAfter = g; }
    }

    static Step[] Pattern(params Step[] steps) => steps;

    static void Run(Step[] steps)
    {
        if (runner == null)
        {
            var go = new GameObject("HapticService_Runner");
            go.hideFlags = HideFlags.HideInHierarchy;
            Object.DontDestroyOnLoad(go);
            runner = go.AddComponent<HapticRunner>();
        }
        runner.Play(steps);
    }

    static void Pulse(float amp, float dur)
    {
        devices.Clear();
        UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
            UnityEngine.XR.InputDeviceCharacteristics.Controller, devices);

        foreach (var d in devices)
            if (d.TryGetHapticCapabilities(out var cap) && cap.supportsImpulse)
                d.SendHapticImpulse(0, Mathf.Clamp01(amp), dur);
    }

    class HapticRunner : MonoBehaviour
    {
        Coroutine current;

        public void Play(Step[] steps)
        {
            if (current != null) StopCoroutine(current);
            current = StartCoroutine(Sequence(steps));
        }

        IEnumerator Sequence(Step[] steps)
        {
            foreach (var s in steps)
            {
                Pulse(s.amp, s.dur);
                yield return new WaitForSecondsRealtime(s.dur + s.gapAfter);
            }
            current = null;
        }
    }
}
