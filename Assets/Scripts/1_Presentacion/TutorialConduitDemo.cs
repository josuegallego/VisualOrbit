using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Demo automatica con el conducto REAL (anillo, flujo de glucosa y neurona), en bucle:
///   1) ANIMACION (el anillo NO se puede tocar): la mano fantasma se acerca -> agarra -> gira el
///      anillo -> encaja (lo resuelve el propio TubeRingManipulator) -> fluye la glucosa ->
///      se enciende la neurona.
///   2) TURNO DEL JUGADOR (el anillo SI se puede tocar): el anillo se desordena de nuevo y el
///      jugador puede probarlo. Si lo agarra, la demo le cede el control. Si nadie lo toca en
///      'yourTurnSeconds', vuelve la animacion.
///
/// Ponlo en el objeto raiz de un conducto de DEMOSTRACION (duplicado, FUERA de NeuralNetwork).
/// Requiere en TubeRingManipulator: Offset, AxisDirection, DemoSetOffset() y DemoReset().
/// </summary>
public class TutorialConduitDemo : MonoBehaviour
{
    [Header("Conducto de demostracion")]
    public TubeRingManipulator ring;
    [Tooltip("GlucoseFlow del conducto. Con 'Flow On Start' DESACTIVADO.")]
    public GlucoseFlowController flow;

    [Header("Mano fantasma")]
    [Tooltip("Opcional: tu propio modelo de mano. Vacio = esfera amarilla.")]
    public Transform ghostHand;
    [Tooltip("Tamano en metros de la dimension mayor de TU modelo de mano (se ajusta solo). " +
             "Una mano real mide unos 0.2 m: prueba entre 0.25 y 0.4. 0 = no tocar su escala. " +
             "Cambialo con el juego PARADO.")]
    public float ghostHandSize = 0.3f;
    public float ghostSize = 0.045f;
    public Color ghostColor = new Color(1f, 0.85f, 0.2f);
    [Tooltip("0 = automatico (segun el tamano del anillo).")]
    public float handRadius = 0f;

    [Header("Tiempos")]
    [Tooltip("Desorden inicial del anillo (grados). Debe ser mayor que la tolerancia.")]
    public float startOffset = 130f;
    public float idleSeconds = 1.5f;
    public float approachSeconds = 1f;
    public float grabSeconds = 0.7f;
    public float rotateSeconds = 2.5f;
    public float celebrateSeconds = 5f;
    [Tooltip("Turno del jugador: si nadie agarra el anillo en este tiempo, vuelve la animacion.")]
    public float yourTurnSeconds = 8f;
    [Tooltip("Si el jugador agarro el anillo y no lo resuelve, la demo reinicia tras estos segundos.")]
    public float userIdleResetSeconds = 10f;
    [Tooltip("Si el jugador lo resolvio, la demo reinicia tras estos segundos.")]
    public float userSolvedResetSeconds = 4f;

    [Header("Textos (opcional)")]
    public TMP_Text caption;
    public string txtApproach = "Acerca la mano o el rayo al anillo";
    public string txtGrab = "Mantén el gatillo para agarrarlo";
    public string txtRotate = "Gíralo alrededor del tubo";
    public string txtSolved = "¡Encaja! La glucosa empieza a fluir";
    public string txtNeuron = "Y la neurona se enciende";
    public string txtYourTurn = "¡Ahora tú! Agarra el anillo y gíralo";

    enum State { Idle, Approach, Grab, Rotate, WaitSolved, Celebrate, YourTurn, User }

    State state = State.Idle;
    float stateTime;
    float userTimer;
    bool inited;

    XRBaseInteractable ringInteractable;
    Renderer ringRenderer;
    GameObject ghost;
    bool ghostFitted;
    string lastCaption;

    IEnumerator Start()
    {
        if (ring == null)
        {
            Debug.LogError("[TutorialDemo] Falta 'Ring'.", this);
            yield break;
        }

        ringInteractable = ring.GetComponent<XRBaseInteractable>();
        ringRenderer = ring.GetComponentInChildren<Renderer>();
        if (flow != null) flow.SetFlow(false);

        yield return null;   // deja que el anillo termine su Start (y su desorden aleatorio)

        ResetDemo();
        inited = true;
    }

    void OnDisable()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    // ------------------------------------------------------------ Bucle

    void Update()
    {
        if (!inited || ring == null) return;

        stateTime += Time.deltaTime;

        // El jugador agarro el anillo (solo es posible en su turno): la demo le cede el control
        if (state != State.User && ringInteractable != null && ringInteractable.firstInteractorSelecting != null)
        {
            Go(State.User);
            userTimer = 0f;
            HideGhost();
        }

        switch (state)
        {
            case State.Idle:
                SetCaption(txtApproach);
                if (stateTime >= idleSeconds) Go(State.Approach);
                break;

            case State.Approach:
                {
                    SetCaption(txtApproach);
                    Vector3 target = HandPos(ring.Offset);
                    Vector3 outward = (target - Center()).normalized;
                    float k = Mathf.SmoothStep(0f, 1f, stateTime / Mathf.Max(0.1f, approachSeconds));
                    ShowGhost(Vector3.Lerp(target + outward * 0.3f, target, k), 1f);
                    if (stateTime >= approachSeconds) Go(State.Grab);
                    break;
                }

            case State.Grab:
                SetCaption(txtGrab);
                ShowGhost(HandPos(ring.Offset), 0.8f);   // se "aprieta"
                if (stateTime >= grabSeconds) Go(State.Rotate);
                break;

            case State.Rotate:
                {
                    SetCaption(txtRotate);
                    float k = Mathf.SmoothStep(0f, 1f, stateTime / Mathf.Max(0.1f, rotateSeconds));
                    float off = Mathf.Lerp(startOffset, 0f, k);
                    ring.DemoSetOffset(off);
                    ShowGhost(HandPos(off), 0.8f);
                    if (stateTime >= rotateSeconds) Go(State.WaitSolved);
                    break;
                }

            case State.WaitSolved:
                // El propio TubeRingManipulator lo resuelve tras 'holdSeconds' alineado
                ShowGhost(HandPos(0f), 1f);
                if (ring.IsSolved || stateTime > 3f) Go(State.Celebrate);
                break;

            case State.Celebrate:
                HideGhost();
                SetCaption(stateTime < 2.2f ? txtSolved : txtNeuron);
                if (stateTime >= celebrateSeconds)
                {
                    ResetDemo();          // vuelve a desordenar el anillo y apaga flujo y neurona
                    Go(State.YourTurn);   // ahora el jugador puede probarlo
                }
                break;

            case State.YourTurn:
                SetCaption(txtYourTurn);
                if (stateTime >= yourTurnSeconds) ResetDemo();   // nadie lo toco: vuelve la animacion
                break;

            case State.User:
                UpdateUser();
                break;
        }
    }

    void UpdateUser()
    {
        bool selecting = ringInteractable != null && ringInteractable.firstInteractorSelecting != null;

        if (ring.IsSolved)
        {
            SetCaption(txtSolved);
            userTimer += Time.deltaTime;
            if (userTimer >= userSolvedResetSeconds) ResetDemo();
            return;
        }

        if (selecting)
        {
            userTimer = 0f;
            SetCaption(txtRotate);
        }
        else
        {
            userTimer += Time.deltaTime;
            SetCaption(txtYourTurn);
            if (userTimer >= userIdleResetSeconds) ResetDemo();
        }
    }

    void Go(State next)
    {
        state = next;
        stateTime = 0f;

        // El anillo solo se puede tocar en el turno del jugador. Durante la animacion no.
        SetRingInteractable(next == State.YourTurn || next == State.User);

        if (next == State.Celebrate && flow != null && !flow.IsFlowing)
            flow.SetFlow(true);   // por si el anillo no tenia el flujo conectado
    }

    // Bloquea o permite que las manos reales agarren el anillo del tutorial
    void SetRingInteractable(bool on)
    {
        if (ringInteractable != null) ringInteractable.enabled = on;
    }

    void ResetDemo()
    {
        ring.DemoReset(startOffset);
        if (flow != null) flow.SetFlow(false);   // la neurona se apaga sola
        HideGhost();
        Go(State.Idle);
    }

    // ------------------------------------------------------------ Mano fantasma

    Vector3 Center()
    {
        return ringRenderer != null ? ringRenderer.bounds.center : ring.transform.position;
    }

    float Radius()
    {
        if (handRadius > 0f) return handRadius;
        if (ringRenderer == null) return 0.1f;

        Vector3 axis = ring.AxisDirection;
        Vector3 e = ringRenderer.bounds.extents;
        Vector3 a = new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z));
        return Mathf.Max(0.05f, Mathf.Max(e.x * (1f - a.x), e.y * (1f - a.y), e.z * (1f - a.z)));
    }

    // Punto del borde del anillo que gira junto con el (offset = giro actual del anillo)
    Vector3 HandPos(float offset)
    {
        Vector3 axis = ring.AxisDirection;
        Vector3 u = Vector3.Cross(axis, Vector3.up);
        if (u.sqrMagnitude < 0.01f) u = Vector3.Cross(axis, Vector3.right);
        u.Normalize();

        return Center() + Quaternion.AngleAxis(offset, axis) * u * Radius();
    }

    void ShowGhost(Vector3 position, float scale)
    {
        if (ghost == null)
        {
            if (ghostHand != null)
            {
                // Si es un prefab del Project (no un objeto de la escena), se crea una copia
                ghost = ghostHand.gameObject.scene.IsValid()
                    ? ghostHand.gameObject
                    : Instantiate(ghostHand.gameObject, transform);
                // La mano es solo visual: sin colliders ni fisica, para que no agarre el anillo de verdad
                foreach (var c in ghost.GetComponentsInChildren<Collider>(true)) c.enabled = false;
                foreach (var rb in ghost.GetComponentsInChildren<Rigidbody>(true)) rb.isKinematic = true;
            }
            else
            {
                ghost = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                ghost.name = "Tutorial_GhostHand";
                ghost.transform.SetParent(transform, true);
                var col = ghost.GetComponent<Collider>();
                if (col != null) Destroy(col);
                var m = new Material(Shader.Find("Sprites/Default"));
                m.color = ghostColor;
                ghost.GetComponent<Renderer>().material = m;
            }
        }

        ghost.SetActive(true);

        // Una sola vez: ajusta el modelo al tamano pedido (los FBX suelen venir con escalas enormes)
        if (ghostHand != null && !ghostFitted)
        {
            ghostFitted = true;
            FitToSize(ghost, ghostHandSize);
            Debug.Log($"[TutorialDemo] mano fantasma lista: '{ghost.name}' | escala final = {ghost.transform.lossyScale}", this);
        }

        ghost.transform.position = position;
        if (ghostHand == null) ghost.transform.localScale = Vector3.one * ghostSize * scale;
    }

    static void FitToSize(GameObject go, float size)
    {
        if (size <= 0f) return;

        var rends = go.GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0) return;

        Bounds b = rends[0].bounds;
        for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);

        float max = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (max < 0.0001f) return;

        go.transform.localScale *= size / max;
    }

    void HideGhost()
    {
        if (ghost != null) ghost.SetActive(false);
    }

    void SetCaption(string text)
    {
        if (caption == null || text == lastCaption) return;
        lastCaption = text;
        caption.text = text;
    }
}