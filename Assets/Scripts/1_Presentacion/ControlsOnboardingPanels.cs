using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

/// <summary>
/// Onboarding de controles con PANELES-IMAGEN (uno por boton) y animaciones.
///
/// Por cada paso:
///   - El panel entra con un "pop" (escala + fundido).
///   - Respira suavemente y emite ondas de luz del color del boton (halo que se expande).
///   - Espera a que el jugador presione de verdad ese boton.
///   - Al acertar: destello verde, golpecito de escala, vibracion, y el panel sale.
/// Al final aparece el boton Continuar (y opcionalmente un panel "Listo").
///
/// Si no detecta el boton (simulador): Enter cuenta como pulsacion y, pasados X segundos, avanza solo.
/// Pasos: GATILLO, AGARRE, BOTON Y (la Y abre la configuracion y aqui se cierra sola) y JOYSTICK IZQUIERDO (mover).
///
/// USO: ver el final del archivo.
/// </summary>
public class ControlsOnboardingPanels : MonoBehaviour
{
    public enum Boton { Gatillo, Agarre, BotonY, JoystickIzquierdo }

    [System.Serializable]
    public class Paso
    {
        public Boton boton = Boton.Gatillo;
        [Tooltip("Objeto Image del panel de este paso (con el sprite panel_onboarding_n).")]
        public Image panel;
        public Color color = new Color(0.92f, 1f, 0f);
    }

    [Header("Pasos (en orden)")]
    public Paso[] pasos = new Paso[]
    {
        new Paso { boton = Boton.Gatillo, color = new Color(0.92f, 1f, 0f) },
        new Paso { boton = Boton.Agarre,  color = new Color(0f, 0.94f, 1f) },
        new Paso { boton = Boton.BotonY,  color = new Color(0.15f, 0.82f, 0.41f) },
        new Paso { boton = Boton.JoystickIzquierdo, color = new Color(0.75f, 0.43f, 1f) },
    };

    [Header("Bienvenida (antes del primer paso)")]
    [Tooltip("Opcional: panel 'Conoce tus controles' que aparece primero y se va solo.")]
    public Image panelBienvenida;
    [Tooltip("Segundos que se muestra la bienvenida antes de pasar al primer boton.")]
    public float bienvenidaSegundos = 5f;

    [Header("Al terminar")]
    [Tooltip("Opcional: panel 'Listo' que aparece al final.")]
    public Image panelFinal;
    public Color colorFinal = new Color(0.2f, 1f, 0.9f);
    [Tooltip("Boton Continuar. Se oculta al inicio y aparece al terminar.")]
    public GameObject continuarObj;

    [Header("Objetos viejos a ocultar (textos e iconos del onboarding anterior)")]
    public GameObject[] ocultarAlIniciar;
    [Tooltip("Si se deja vacio se busca y se apaga el ControlVRAnimation viejo.")]
    public ControlVRAnimation animacionAnterior;

    [Header("Animacion")]
    public float duracionEntrada = 0.45f;
    public float duracionSalida = 0.3f;
    [Range(0f, 0.1f)] public float respiracion = 0.025f;
    public float velocidadRespiracion = 2.2f;
    [Tooltip("Ondas de luz que se expanden desde el panel.")]
    public bool ondasDeLuz = true;
    public float duracionOnda = 1.4f;
    [Range(0f, 0.3f)] public float expansionOnda = 0.12f;
    [Range(0f, 1f)] public float intensidadOnda = 0.55f;

    [Header("Tiempos")]
    public float esperaInicial = 0.8f;
    public float pausaExito = 1.0f;
    [Tooltip("Si no detecta el boton, avanza solo pasados estos segundos (0 = nunca).")]
    public float segundosParaAvanzarSolo = 25f;
    [Range(0.1f, 1f)] public float umbralAnalogico = 0.6f;

    [Header("Feedback de acierto / error (viñetas verde y roja + vibracion)")]
    [Tooltip("El RingFeedback (el mismo de los minijuegos: viñeta verde/roja y vibracion). " +
             "Copialo de ArmaMolecula con Copy Component > Paste Component As New. Si queda vacio se busca en la escena.")]
    public RingFeedback vineta;
    [Tooltip("Si el jugador presiona OTRO boton, muestra la viñeta roja, vibra y sacude el panel.")]
    public bool avisarBotonIncorrecto = true;
    [Tooltip("El joystick izquierdo tambien cuenta como error en los pasos que no son del joystick (por defecto no, porque se mueve sin querer).")]
    public bool joystickCuentaComoError = false;
    [Tooltip("Mover el joystick DERECHO (esta bloqueado) tambien muestra la viñeta roja, vibra y sacude el panel.")]
    public bool joystickDerechoCuentaComoError = true;
    [Tooltip("Segundos minimos entre un error y otro.")]
    public float pausaEntreErrores = 1f;
    [Tooltip("Duracion de la sacudida del panel al equivocarse (0 = sin sacudida).")]
    public float sacudidaPanel = 0.35f;
    [Tooltip("Intensidad de la sacudida (unidades del Canvas).")]
    public float sacudidaFuerza = 14f;
    [Tooltip("Mientras el tutorial no llegue al paso del boton Y, el menu de configuracion (ControllerMenuToggle) queda desactivado, para que presionar Y por error no abra la configuracion.")]
    public bool bloquearMenuHastaPasoY = true;

    [Header("Extras")]
    public bool vibrarAlAcertar = true;
    public AudioClip sonidoAcierto;
    [Range(0f, 1f)] public float volumenAcierto = 0.8f;
    public bool logEvents = true;

    readonly List<InputDevice> dispositivos = new List<InputDevice>();
    AudioSource audioFuente;
    readonly Dictionary<RectTransform, Vector3> escalasBase = new Dictionary<RectTransform, Vector3>();

    // Escala que el panel tiene en la escena (no se asume 1: en World Space suele ser ~0.001)
    Vector3 Base(RectTransform rt)
    {
        if (!escalasBase.TryGetValue(rt, out var v)) { v = rt.localScale; escalasBase[rt] = v; }
        return v;
    }

    // ------------------------------------------------------------------ ciclo de vida

    ControllerMenuToggle menuToggle;

    void Awake()
    {
        if (animacionAnterior == null) animacionAnterior = FindFirstObjectByType<ControlVRAnimation>();
        if (vineta == null) vineta = FindFirstObjectByType<RingFeedback>();
        menuToggle = FindFirstObjectByType<ControllerMenuToggle>();
    }

    void OnDisable()
    {
        if (menuToggle != null) menuToggle.enabled = true;   // no dejamos la configuracion bloqueada
    }

    void Bien()
    {
        if (vineta != null) vineta.FlashBien();
        if (vibrarAlAcertar && (vineta == null || !vineta.useHaptics)) HapticService.Success();
    }

    void Mal()
    {
        if (vineta != null) vineta.FlashMal();
        else HapticService.Error();
    }

    void Start()
    {
        if (animacionAnterior != null) animacionAnterior.enabled = false;
        if (ocultarAlIniciar != null)
            foreach (var go in ocultarAlIniciar) if (go != null) go.SetActive(false);
        foreach (var p in pasos) if (p.panel != null) { Base(p.panel.rectTransform); Grupo(p.panel); p.panel.gameObject.SetActive(false); }
        if (panelFinal != null) { Base(panelFinal.rectTransform); Grupo(panelFinal); panelFinal.gameObject.SetActive(false); }
        if (panelBienvenida != null) { Base(panelBienvenida.rectTransform); Grupo(panelBienvenida); panelBienvenida.gameObject.SetActive(false); }
        if (continuarObj != null) continuarObj.SetActive(false);
        if (bloquearMenuHastaPasoY && menuToggle != null) menuToggle.enabled = false;
        StartCoroutine(Secuencia());
    }

    // ------------------------------------------------------------------ secuencia

    IEnumerator Secuencia()
    {
        yield return new WaitForSeconds(esperaInicial);

        if (panelBienvenida != null)
        {
            yield return Entrada(panelBienvenida);
            float tb = 0f;
            var rtb = panelBienvenida.rectTransform;
            while (tb < bienvenidaSegundos)
            {
                tb += Time.deltaTime;
                rtb.localScale = Base(rtb) * (1f + respiracion * Mathf.Sin(tb * velocidadRespiracion));
                yield return null;
            }
            rtb.localScale = Base(rtb);
            yield return Salida(panelBienvenida);
            panelBienvenida.gameObject.SetActive(false);
        }

        for (int i = 0; i < pasos.Length; i++)
        {
            var p = pasos[i];
            if (p.panel == null) continue;
            if (logEvents) Debug.Log($"[ControlsOnboardingPanels] Paso {i + 1}/{pasos.Length}: {p.boton}", this);

            Image halo = ondasDeLuz ? CrearHalo(p.panel, p.color) : null;
            Image flash = CrearFlash(p.panel);
            yield return Entrada(p.panel);

            // En el paso del boton Y se habilita el menu de configuracion (el tutorial abre y cierra el panel)
            if (menuToggle != null && bloquearMenuHastaPasoY) menuToggle.enabled = (p.boton == Boton.BotonY);

            bool antes = Presionado(p.boton);
            float t = 0f, tAnim = 0f;
            var rt = p.panel.rectTransform;

            // Estado previo de los demas botones, para detectar un boton NUEVO que no es el pedido
            var todos = (Boton[])System.Enum.GetValues(typeof(Boton));
            var previos = new Dictionary<Boton, bool>();
            foreach (var b in todos) previos[b] = Presionado(b);
            float proximoError = 0f, sacudida = 0f;
            bool derPrevio = LeerJoystickDerecho();
            Vector2 posBase = rt.anchoredPosition;

            while (true)
            {
                bool ahora = Presionado(p.boton) || TeclaSimular();
                if (ahora && !antes) break;
                antes = ahora;

                if (avisarBotonIncorrecto && joystickDerechoCuentaComoError)
                {
                    bool der = LeerJoystickDerecho();
                    if (der && !derPrevio && Time.time >= proximoError)
                    {
                        proximoError = Time.time + pausaEntreErrores;
                        sacudida = sacudidaPanel;
                        Mal();
                        if (logEvents) Debug.Log("[ControlsOnboardingPanels] Joystick derecho (bloqueado).", this);
                    }
                    derPrevio = der;
                }

                if (avisarBotonIncorrecto)
                {
                    foreach (var b in todos)
                    {
                        if (b == p.boton) continue;
                        bool n = Presionado(b);
                        bool eraAntes = previos[b];
                        previos[b] = n;
                        if (!n || eraAntes) continue;
                        if (b == Boton.JoystickIzquierdo && !joystickCuentaComoError) continue;
                        if (Time.time < proximoError) continue;

                        proximoError = Time.time + pausaEntreErrores;
                        sacudida = sacudidaPanel;
                        Mal();
                        if (logEvents) Debug.Log($"[ControlsOnboardingPanels] Boton incorrecto: {b} (se pedia {p.boton}).", this);
                    }
                }

                if (sacudida > 0f)
                {
                    sacudida -= Time.deltaTime;
                    float k = Mathf.Clamp01(sacudida / Mathf.Max(0.01f, sacudidaPanel));
                    rt.anchoredPosition = posBase + Vector2.right * (Mathf.Sin(Time.time * 70f) * sacudidaFuerza * k);
                    if (sacudida <= 0f) rt.anchoredPosition = posBase;
                }

                t += Time.deltaTime;
                tAnim += Time.deltaTime;
                if (segundosParaAvanzarSolo > 0f && t >= segundosParaAvanzarSolo)
                {
                    if (logEvents) Debug.Log("[ControlsOnboardingPanels] Avanza solo (no se detecto el boton).", this);
                    break;
                }
                rt.localScale = Base(rt) * (1f + respiracion * Mathf.Sin(tAnim * velocidadRespiracion));
                AnimarHalo(halo, tAnim);
                yield return null;
            }

            // Acierto
            rt.anchoredPosition = posBase;
            if (halo != null) Destroy(halo.gameObject);
            Bien();
            if (menuToggle != null && bloquearMenuHastaPasoY) menuToggle.enabled = false;
            if (sonidoAcierto != null) Sonar(sonidoAcierto, volumenAcierto);
            yield return Acierto(p.panel, flash);

            if (p.boton == Boton.BotonY && SettingsPanelManager.Instance != null)
            {
                yield return new WaitForSeconds(0.8f);
                SettingsPanelManager.Instance.ClosePanel();
            }
            yield return Salida(p.panel);
            Destroy(flash.gameObject);
            p.panel.gameObject.SetActive(false);
        }

        // Final: se devuelve el menu de configuracion
        if (menuToggle != null) menuToggle.enabled = true;
        if (panelFinal != null)
        {
            Image halo = ondasDeLuz ? CrearHalo(panelFinal, colorFinal) : null;
            yield return Entrada(panelFinal);
            if (continuarObj != null) continuarObj.SetActive(true);
            float tAnim = 0f;
            while (true)
            {
                tAnim += Time.deltaTime;
                panelFinal.rectTransform.localScale = Base(panelFinal.rectTransform) * (1f + respiracion * Mathf.Sin(tAnim * velocidadRespiracion));
                AnimarHalo(halo, tAnim);
                yield return null;
            }
        }
        else if (continuarObj != null) continuarObj.SetActive(true);
    }

    // ------------------------------------------------------------------ animaciones

    IEnumerator Entrada(Image panel)
    {
        panel.gameObject.SetActive(true);
        var g = Grupo(panel);
        var rt = panel.rectTransform;
        float t = 0f;
        while (t < duracionEntrada)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionEntrada);
            g.alpha = Mathf.SmoothStep(0f, 1f, k);
            rt.localScale = Base(rt) * Mathf.LerpUnclamped(0.8f, 1f, EaseOutBack(k));
            yield return null;
        }
        g.alpha = 1f; rt.localScale = Base(rt);
    }

    IEnumerator Salida(Image panel)
    {
        var g = Grupo(panel);
        var rt = panel.rectTransform;
        Vector3 s0 = rt.localScale; float t = 0f;
        while (t < duracionSalida)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duracionSalida);
            g.alpha = 1f - k;
            rt.localScale = s0 * Mathf.Lerp(1f, 1.08f, k);
            yield return null;
        }
        g.alpha = 0f;
    }

    // golpecito de escala + destello verde sobre el panel
    IEnumerator Acierto(Image panel, Image flash)
    {
        var rt = panel.rectTransform;
        Color verde = new Color(0.3f, 1f, 0.5f);
        float dur = Mathf.Max(0.35f, pausaExito * 0.5f), t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            rt.localScale = Base(rt) * (1f + 0.10f * Mathf.Sin(k * Mathf.PI));
            flash.color = new Color(verde.r, verde.g, verde.b, 0.55f * (1f - k));
            yield return null;
        }
        flash.color = new Color(verde.r, verde.g, verde.b, 0f);
        yield return new WaitForSeconds(Mathf.Max(0f, pausaExito - dur));
    }

    // halo: copia del panel detras, del color del boton, que se expande y se desvanece en bucle
    Image CrearHalo(Image panel, Color color)
    {
        var go = new GameObject("Halo", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(panel.transform.parent, false);
        CopiarRect(panel.rectTransform, rt);
        rt.localScale = Base(panel.rectTransform);
        rt.SetSiblingIndex(panel.transform.GetSiblingIndex());
        var im = go.GetComponent<Image>();
        im.sprite = panel.sprite;
        im.raycastTarget = false;
        im.color = new Color(color.r, color.g, color.b, 0f);
        return im;
    }

    // capa blanca/verde que cubre el panel para el destello de acierto
    Image CrearFlash(Image panel)
    {
        var go = new GameObject("Flash", typeof(RectTransform), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.SetParent(panel.transform, false);
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        var im = go.GetComponent<Image>();
        im.sprite = panel.sprite;
        im.raycastTarget = false;
        im.color = new Color(1, 1, 1, 0);
        return im;
    }

    void AnimarHalo(Image halo, float t)
    {
        if (halo == null) return;
        var rt = halo.rectTransform;
        var panelRt = (RectTransform)halo.transform.parent.GetChild(halo.transform.GetSiblingIndex() + 1);
        // Dos ondas desfasadas
        float k = (t / duracionOnda) % 1f;
        rt.localScale = Base(panelRt) * (1f + expansionOnda * k);
        var c = halo.color; c.a = intensidadOnda * (1f - k) * (1f - k);
        halo.color = c;
    }

    static void CopiarRect(RectTransform a, RectTransform b)
    {
        b.anchorMin = a.anchorMin; b.anchorMax = a.anchorMax; b.pivot = a.pivot;
        b.anchoredPosition3D = a.anchoredPosition3D; b.sizeDelta = a.sizeDelta;
        b.localRotation = a.localRotation;
    }

    static CanvasGroup Grupo(Image panel)
    {
        var g = panel.GetComponent<CanvasGroup>();
        if (g == null) g = panel.gameObject.AddComponent<CanvasGroup>();
        g.interactable = false; g.blocksRaycasts = false;
        return g;
    }

    static float EaseOutBack(float x)
    {
        const float c1 = 1.70158f, c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(x - 1f, 3f) + c1 * Mathf.Pow(x - 1f, 2f);
    }

    // ------------------------------------------------------------------ deteccion de botones

    bool Presionado(Boton b)
    {
        switch (b)
        {
            case Boton.Gatillo: return Leer(InputDeviceCharacteristics.Left, CommonUsages.triggerButton, CommonUsages.trigger)
                                    || Leer(InputDeviceCharacteristics.Right, CommonUsages.triggerButton, CommonUsages.trigger);
            case Boton.Agarre: return Leer(InputDeviceCharacteristics.Left, CommonUsages.gripButton, CommonUsages.grip)
                                   || Leer(InputDeviceCharacteristics.Right, CommonUsages.gripButton, CommonUsages.grip);
            case Boton.BotonY: return LeerY();
            case Boton.JoystickIzquierdo: return LeerJoystickIzquierdo();
        }
        return false;
    }

    bool Leer(InputDeviceCharacteristics mano, InputFeatureUsage<bool> boton, InputFeatureUsage<float> analogico)
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

    // Joystick izquierdo movido lo suficiente (se detecta con el visor puesto; en PC, Enter)
    bool LeerJoystickIzquierdo()
    {
        dispositivos.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left, dispositivos);
        foreach (var d in dispositivos)
            if (d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v) && v.magnitude >= umbralAnalogico) return true;
        return false;
    }

    bool LeerJoystickDerecho()
    {
        dispositivos.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right, dispositivos);
        foreach (var d in dispositivos)
            if (d.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 v) && v.magnitude >= umbralAnalogico) return true;
        return false;
    }

    bool LeerY()
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

    void Sonar(AudioClip clip, float vol)
    {
        if (audioFuente == null)
        {
            audioFuente = gameObject.AddComponent<AudioSource>();
            audioFuente.playOnAwake = false; audioFuente.spatialBlend = 0f;
        }
        audioFuente.PlayOneShot(clip, vol);
    }
}

/* ======================= USO EN UNITY =======================
 1. Importa los 3 PNG: Texture Type = Sprite (2D and UI) > Apply.
 2. En el Canvas de TutorialControl crea 3 objetos UI > Image (Panel_Gatillo, Panel_Agarre, Panel_BotonY),
    cada uno con su sprite. Mismo Rect Transform en los tres (p. ej. 1600x900 centrado, escala en el Canvas
    para que quepa). Deja "Preserve Aspect" activado.
 3. Objeto vacio "OnboardingPanels" > Add Component > ControlsOnboardingPanels.
    - Pasos: arrastra cada panel en su paso (Gatillo, Agarre, BotonY).
    - Continuar Obj: tu boton Continuar.
    - Ocultar Al Iniciar: los textos/iconos viejos del compañero.
 4. Si tenias ControlsOnboarding (el de textos) en la escena, quitalo: este lo reemplaza.
 5. Play. En PC: Enter = "presione el boton".
*/
