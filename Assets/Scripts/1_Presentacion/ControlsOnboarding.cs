using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.XR;

/// <summary>
/// Onboarding de los controles, UN boton a la vez y con practica real.
///
/// Para cada paso:
///   1. Se resalta SOLO ese boton en el modelo del control (brilla y late con su color).
///   2. Se muestra su nombre grande, una frase corta y la pista "Presionalo ahora".
///   3. Espera a que el jugador lo presione de verdad. Al hacerlo: vibracion suave, "Muy bien" y pasa al siguiente.
///   4. Al terminar los pasos aparece el boton Continuar.
///
/// Pasos por defecto: GATILLO (elegir / pulsar botones), AGARRE (tomar objetos) y BOTON Y (configuracion).
///
/// Si no detecta el boton (por ejemplo en el simulador del PC):
///   - Enter en el teclado cuenta como "lo presione".
///   - Pasados 'segundosParaAvanzarSolo' avanza solo, para no bloquear a nadie.
///
/// USO: ver instrucciones al final de este archivo.
/// </summary>
public class ControlsOnboarding : MonoBehaviour
{
    public enum Boton { Gatillo, Agarre, BotonY }

    [System.Serializable]
    public class Paso
    {
        public Boton boton = Boton.Gatillo;
        public string nombre = "GATILLO";
        [TextArea(1, 3)] public string instruccion = "Sirve para elegir y pulsar botones.";
        public string pista = "Presiónalo ahora";
        public string exito = "¡Muy bien!";
        public Color color = new Color(0.92f, 1f, 0f);
        [Tooltip("Parte del modelo del control que brilla. Si lo dejas vacio se toma de ControlVRAnimation.")]
        public Renderer resaltar;
    }

    [Header("Pasos")]
    public Paso[] pasos = new Paso[]
    {
        new Paso { boton = Boton.Gatillo, nombre = "GATILLO", color = new Color(0.92f, 1f, 0f),
                   instruccion = "Sirve para elegir opciones y pulsar botones." },
        new Paso { boton = Boton.Agarre, nombre = "AGARRE", color = new Color(0f, 1f, 1f),
                   instruccion = "Mantenlo apretado para tomar objetos.", pista = "Apriétalo ahora" },
        new Paso { boton = Boton.BotonY, nombre = "BOTÓN Y", color = new Color(0.15f, 0.82f, 0.41f),
                   instruccion = "Abre la configuración: audio, efectos y más." },
    };

    [Header("Textos (arrastra TMP existentes o nuevos)")]
    [Tooltip("Nombre grande del boton, con su color.")]
    public TMP_Text nombreText;
    [Tooltip("Frase corta que explica para que sirve.")]
    public TMP_Text instruccionText;
    [Tooltip("Pista de accion: 'Presionalo ahora' / '¡Muy bien!'.")]
    public TMP_Text pistaText;
    [Tooltip("Opcional: puntos de progreso (● ○ ○).")]
    public TMP_Text progresoText;
    [Tooltip("Opcional: grupo que contiene los textos, para hacer fundido entre pasos.")]
    public CanvasGroup grupoTextos;

    [Header("Al terminar")]
    [Tooltip("Boton Continuar. Se oculta al inicio y aparece al terminar todos los pasos.")]
    public GameObject continuarObj;
    public string tituloFinal = "¡LISTO!";
    [TextArea(1, 3)] public string instruccionFinal = "Ya sabes cómo moverte. Presiona CONTINUAR con el gatillo.";
    public Color colorFinal = new Color(0.2f, 1f, 0.9f);

    [Header("Objetos del onboarding viejo que se ocultan (textos sueltos, iconos...)")]
    public GameObject[] ocultarAlIniciar;

    [Header("Resaltado del control")]
    [Tooltip("Si se deja vacio se busca ControlVRAnimation en la escena para tomar los renderers y se apaga su bucle.")]
    public ControlVRAnimation animacionAnterior;
    public float escalaMaxima = 1.3f;
    public float velocidadPulso = 3f;

    [Header("Tiempos")]
    public float esperaInicial = 1.0f;
    public float pausaExito = 1.3f;
    public float fundido = 0.25f;
    [Tooltip("Si no detecta el boton, avanza solo pasados estos segundos (0 = nunca).")]
    public float segundosParaAvanzarSolo = 25f;
    [Range(0.1f, 1f)] public float umbralAnalogico = 0.6f;

    [Header("Extras")]
    public bool vibrarAlAcertar = true;
    public AudioClip sonidoAcierto;
    [Range(0f, 1f)] public float volumenAcierto = 0.8f;
    public bool logEvents = true;

    readonly List<InputDevice> dispositivos = new List<InputDevice>();
    readonly Dictionary<Renderer, Vector3> escalasOriginales = new Dictionary<Renderer, Vector3>();
    Coroutine pulso;
    AudioSource audioFuente;

    // ------------------------------------------------------------------ ciclo de vida

    void Awake()
    {
        if (animacionAnterior == null) animacionAnterior = FindFirstObjectByType<ControlVRAnimation>();
    }

    void Start()
    {
        // Rellena los renderers que falten usando el script anterior y lo apaga (si no, los dos se pisarian).
        if (animacionAnterior != null)
        {
            foreach (var p in pasos)
            {
                if (p.resaltar != null) continue;
                switch (p.boton)
                {
                    case Boton.Gatillo: p.resaltar = animacionAnterior.triggerRenderer; break;
                    case Boton.Agarre: p.resaltar = animacionAnterior.bumperRenderer; break;
                    case Boton.BotonY: p.resaltar = animacionAnterior.botonRenderer; break;
                }
            }
            animacionAnterior.enabled = false;
        }

        foreach (var p in pasos) PrepararMaterial(p.resaltar);

        if (ocultarAlIniciar != null)
            foreach (var go in ocultarAlIniciar) if (go != null) go.SetActive(false);
        if (continuarObj != null) continuarObj.SetActive(false);

        StartCoroutine(Secuencia());
    }

    void OnDisable()
    {
        StopAllCoroutines();
        pulso = null;
        foreach (var p in pasos) Apagar(p.resaltar);
    }

    // ------------------------------------------------------------------ secuencia

    IEnumerator Secuencia()
    {
        SetTextos("", "", "", "");
        yield return Fade(0f, 0f);
        yield return new WaitForSeconds(esperaInicial);

        for (int i = 0; i < pasos.Length; i++)
        {
            var p = pasos[i];

            yield return Fade(grupoTextos != null ? grupoTextos.alpha : 0f, 0f);
            SetTextos(p.nombre, p.instruccion, p.pista, Progreso(i), p.color);
            Resaltar(p);
            yield return Fade(0f, 1f);
            if (logEvents) Debug.Log($"[ControlsOnboarding] Paso {i + 1}/{pasos.Length}: {p.nombre}", this);

            // Exige una pulsacion NUEVA: si ya lo tenia apretado al empezar, no cuenta.
            bool antes = Presionado(p.boton);
            float t = 0f;
            while (true)
            {
                bool ahora = Presionado(p.boton) || TeclaSimular();
                if (ahora && !antes) break;
                antes = ahora;

                t += Time.deltaTime;
                if (segundosParaAvanzarSolo > 0f && t >= segundosParaAvanzarSolo)
                {
                    if (logEvents) Debug.Log("[ControlsOnboarding] Avanza solo (no se detecto el boton).", this);
                    break;
                }
                yield return null;
            }

            // Acierto
            DetenerResaltado(p);
            SetTexto(pistaText, p.exito, new Color(0.3f, 1f, 0.5f));
            if (vibrarAlAcertar) HapticService.Soft();
            if (sonidoAcierto != null) Sonar(sonidoAcierto, volumenAcierto);

            if (p.boton == Boton.BotonY && SettingsPanelManager.Instance != null)
            {
                // La Y ya abrio la configuracion (ControllerMenuToggle): la dejamos ver un momento y la cerramos.
                yield return new WaitForSeconds(pausaExito + 0.8f);
                SettingsPanelManager.Instance.ClosePanel();
            }
            else
            {
                yield return new WaitForSeconds(pausaExito);
            }
        }

        // Final
        yield return Fade(grupoTextos != null ? grupoTextos.alpha : 1f, 0f);
        SetTextos(tituloFinal, instruccionFinal, "", Progreso(pasos.Length), colorFinal);
        yield return Fade(0f, 1f);
        if (continuarObj != null) continuarObj.SetActive(true);
    }

    // ------------------------------------------------------------------ deteccion de botones

    bool Presionado(Boton b)
    {
        switch (b)
        {
            case Boton.Gatillo: return LeerBoton(InputDeviceCharacteristics.Left, CommonUsages.triggerButton, CommonUsages.trigger)
                                    || LeerBoton(InputDeviceCharacteristics.Right, CommonUsages.triggerButton, CommonUsages.trigger);
            case Boton.Agarre: return LeerBoton(InputDeviceCharacteristics.Left, CommonUsages.gripButton, CommonUsages.grip)
                                   || LeerBoton(InputDeviceCharacteristics.Right, CommonUsages.gripButton, CommonUsages.grip);
            case Boton.BotonY: return LeerBotonesY();
        }
        return false;
    }

    bool LeerBoton(InputDeviceCharacteristics mano, InputFeatureUsage<bool> boton, InputFeatureUsage<float> analogico)
    {
        dispositivos.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | mano, dispositivos);
        foreach (var d in dispositivos)
        {
            if (d.TryGetFeatureValue(boton, out bool b) && b) return true;
            if (d.TryGetFeatureValue(analogico, out float f) && f >= umbralAnalogico) return true;
        }
        return false;
    }

    bool LeerBotonesY()
    {
        dispositivos.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, dispositivos);
        foreach (var d in dispositivos)
        {
            if (d.TryGetFeatureValue(CommonUsages.secondaryButton, out bool y) && y) return true;
            if (d.TryGetFeatureValue(CommonUsages.menuButton, out bool m) && m) return true;
        }
        return false;
    }

    static bool TeclaSimular()
    {
#if ENABLE_INPUT_SYSTEM
        var kb = UnityEngine.InputSystem.Keyboard.current;
        return kb != null && (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame);
#else
        return false;
#endif
    }

    // ------------------------------------------------------------------ textos

    string Progreso(int hecho)
    {
        string s = "";
        for (int i = 0; i < pasos.Length; i++)
            s += (i < hecho ? "<color=#4DFF88>●</color>" : (i == hecho ? "<color=#FFFFFF>●</color>" : "<color=#556677>○</color>")) + (i < pasos.Length - 1 ? "  " : "");
        return s;
    }

    void SetTextos(string nombre, string instruccion, string pista, string progreso, Color? color = null)
    {
        Color c = color ?? Color.white;
        SetTexto(nombreText, nombre, c);
        SetTexto(instruccionText, instruccion, Color.white);
        SetTexto(pistaText, pista, new Color(0.75f, 0.9f, 1f));
        if (progresoText != null) progresoText.text = progreso;
    }

    static void SetTexto(TMP_Text t, string s, Color c)
    {
        if (t == null) return;
        t.text = s;
        t.color = c;
    }

    IEnumerator Fade(float desde, float hasta)
    {
        if (grupoTextos == null) yield break;
        if (fundido <= 0f) { grupoTextos.alpha = hasta; yield break; }
        float t = 0f;
        while (t < fundido)
        {
            t += Time.deltaTime;
            grupoTextos.alpha = Mathf.Lerp(desde, hasta, t / fundido);
            yield return null;
        }
        grupoTextos.alpha = hasta;
    }

    // ------------------------------------------------------------------ resaltado del boton

    void Resaltar(Paso p)
    {
        foreach (var o in pasos) if (o != p) Apagar(o.resaltar);
        if (p.resaltar == null) return;
        if (pulso != null) StopCoroutine(pulso);
        pulso = StartCoroutine(Latido(p.resaltar, p.color));
    }

    void DetenerResaltado(Paso p)
    {
        if (pulso != null) { StopCoroutine(pulso); pulso = null; }
        Apagar(p.resaltar);
    }

    IEnumerator Latido(Renderer r, Color color)
    {
        Vector3 baseScale = EscalaBase(r);
        Material mat = r.material;
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * 2f);
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * velocidadPulso;
            r.transform.localScale = Vector3.Lerp(baseScale, baseScale * escalaMaxima, Mathf.PingPong(t, 1f));
            yield return null;
        }
    }

    Vector3 EscalaBase(Renderer r)
    {
        if (!escalasOriginales.TryGetValue(r, out var s)) { s = r.transform.localScale; escalasOriginales[r] = s; }
        return s;
    }

    void Apagar(Renderer r)
    {
        if (r == null) return;
        if (escalasOriginales.TryGetValue(r, out var s)) r.transform.localScale = s;
        if (r.material.HasProperty("_EmissionColor")) r.material.SetColor("_EmissionColor", Color.black);
    }

    void PrepararMaterial(Renderer r)
    {
        if (r == null) return;
        EscalaBase(r);
        Material mat = r.material;
        mat.EnableKeyword("_EMISSION");
        mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", Color.black);
    }

    void Sonar(AudioClip clip, float vol)
    {
        if (audioFuente == null)
        {
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false;
            audioFuente.spatialBlend = 0f;
        }
        audioFuente.PlayOneShot(clip, vol);
    }
}

/* ============================ COMO MONTARLO EN LA ESCENA TutorialControl ============================
 *
 * 1. Crea un objeto vacio "ControlsOnboarding" y arrastrale este script.
 *
 * 2. Deja SOLO estos textos en el Canvas (el resto se oculta solo con 'Ocultar Al Iniciar'):
 *      - Titulo: "Configuración previa" (se queda como esta)
 *      - Nombre (grande, centrado)        -> 'Nombre Text'
 *      - Instruccion (una linea)          -> 'Instruccion Text'
 *      - Pista (pequeno, debajo)          -> 'Pista Text'
 *      - Puntos de progreso (opcional)    -> 'Progreso Text'
 *    Puedes reutilizar los TMP que ya hay: "Text (TMP) (6)", "(2)", "(9)", "(3)", "(10)".
 *    Si los metes todos dentro de un objeto con CanvasGroup, arrastralo a 'Grupo Textos' (fundido entre pasos).
 *
 * 3. 'Continuar Obj' = el boton CONTINUAR. 'Ocultar Al Iniciar' = los textos e iconos viejos que ya no se usan
 *    (Texto "En esta experiencia...", "Cuando estes listo...", Imagenconfi, BotonY-Photoroom, etc.).
 *
 * 4. 'Animacion Anterior' se rellena sola con el ControlVRAnimation de la escena: se toman sus renderers
 *    (gatillo / agarre / boton Y) y se apaga su bucle para que no se pisen.
 *
 * 5. Dale Play: en el simulador, Enter cuenta como "lo presione".
 * ================================================================================================== */
