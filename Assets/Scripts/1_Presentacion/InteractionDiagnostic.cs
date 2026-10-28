using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Script TEMPORAL de diagnostico (XRI 3.x). Ponlo en Ring_Root.
/// - Al iniciar revisa la configuracion y escribe en la consola que falta.
/// - Cambia el material del anillo en hover (cian) y al agarrar (amarillo).
/// - Muestra el estado en un texto opcional y en la consola.
/// Se puede borrar cuando el agarre funcione.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public class InteractionDiagnostic : MonoBehaviour
{
    [Header("Visual")]
    [Tooltip("Renderer del anillo (Ring_Mesh). Si queda vacio busca uno en los hijos.")]
    public Renderer targetRenderer;
    [Tooltip("Opcional. Si queda vacio se crea uno cian (URP Unlit).")]
    public Material hoverMaterial;
    [Tooltip("Opcional. Si queda vacio se crea uno amarillo (URP Unlit).")]
    public Material selectMaterial;

    [Header("Opcional: texto de estado (util con las gafas)")]
    public TMP_Text debugLabel;

    [Header("Depuracion")]
    [Tooltip("Escribe cada segundo la distancia de cada interactor al anillo.")]
    public bool logDistances = true;

    XRBaseInteractable interactable;
    Material[] originalMaterials;
    Material[] hoverArray, selectArray;
    XRBaseInteractor[] interactors;
    bool[] pressed;
    int hoverCount;
    bool selected;
    TubeRingManipulator ring;
    bool lastAllowed = true;
    float nextLog;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        ring = GetComponent<TubeRingManipulator>();

        if (targetRenderer == null) targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer != null)
        {
            originalMaterials = targetRenderer.sharedMaterials;
            if (hoverMaterial == null) hoverMaterial = MakeUnlit(new Color(0f, 0.9f, 1f));
            if (selectMaterial == null) selectMaterial = MakeUnlit(new Color(1f, 0.8f, 0.1f));
            hoverArray = Fill(hoverMaterial, originalMaterials.Length);
            selectArray = Fill(selectMaterial, originalMaterials.Length);
        }
        else
        {
            Debug.LogWarning("[Diag] No hay Renderer: no se vera el cambio de material.", this);
        }
    }

    void OnEnable()
    {
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
        hoverCount = 0; selected = false;
        Refresh();
    }

    void Start()
    {
        RunDiagnostic();
        Refresh();
    }

    // ---------------------------------------------------------- Eventos

    void OnHoverEntered(HoverEnterEventArgs a)
    {
        hoverCount++;
        Debug.Log($"[Diag] HOVER ENTRA: '{a.interactorObject.transform.name}'", this);
        Refresh();
    }

    void OnHoverExited(HoverExitEventArgs a)
    {
        hoverCount = Mathf.Max(0, hoverCount - 1);
        Debug.Log($"[Diag] HOVER SALE: '{a.interactorObject.transform.name}'", this);
        Refresh();
    }

    void OnSelectEntered(SelectEnterEventArgs a)
    {
        selected = true;
        Debug.Log($"[Diag] AGARRADO por '{a.interactorObject.transform.name}'", this);
        Refresh();
    }

    void OnSelectExited(SelectExitEventArgs a)
    {
        selected = false;
        Debug.Log($"[Diag] SOLTADO por '{a.interactorObject.transform.name}'", this);
        Refresh();
    }

    // ---------------------------------------------------------- Visual

    void Refresh()
    {
        if (targetRenderer != null && originalMaterials != null)
        {
            Material[] mats = originalMaterials;
            if (selected && selectArray != null && selectMaterial != null) mats = selectArray;
            else if (hoverCount > 0 && HoverAllowed() && hoverArray != null && hoverMaterial != null) mats = hoverArray;
            targetRenderer.sharedMaterials = mats;
        }

        if (debugLabel != null)
            debugLabel.text = $"Hover: {hoverCount}\nAgarrado: {selected}";
    }

    static Material[] Fill(Material m, int count)
    {
        var arr = new Material[count];
        for (int i = 0; i < count; i++) arr[i] = m;
        return arr;
    }

    static Material MakeUnlit(Color c)
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (sh == null)
        {
            Debug.LogWarning("[Diag] No se encontro el shader URP Unlit. Asigna un material propio.");
            return null;
        }
        var m = new Material(sh);
        m.SetColor("_BaseColor", c);
        return m;
    }

    // ---------------------------------------------------------- Diagnostico

    [ContextMenu("Ejecutar diagnostico")]
    public void RunDiagnostic()
    {
        Debug.Log("[Diag] ===== DIAGNOSTICO DE '" + name + "' =====", this);

        var manager = interactable.interactionManager;
        if (manager == null)
            Debug.LogError("[Diag] X El Interaction Manager esta vacio. Arrastra el objeto XRInteractionManager al campo.", this);
        else
            Debug.Log($"[Diag] OK Interaction Manager: '{manager.name}' | registrado: {manager.IsRegistered((IXRInteractable)interactable)}", this);

        var rb = GetComponent<Rigidbody>();
        if (rb == null) Debug.LogError("[Diag] X Falta el Rigidbody en este objeto.", this);
        else Debug.Log($"[Diag] OK Rigidbody | isKinematic: {rb.isKinematic} | useGravity: {rb.useGravity}", this);

        var cols = interactable.colliders;
        if (cols.Count == 0)
            Debug.LogError("[Diag] X La lista 'Colliders' del interactable esta VACIA: nada puede detectarse.", this);
        foreach (var c in cols)
        {
            string extra = "";
            if (c is MeshCollider mc) extra = $" | convex: {mc.convex} | malla: {(mc.sharedMesh != null ? mc.sharedMesh.name : "NINGUNA")}";
            Debug.Log($"[Diag] Collider '{c.name}' ({c.GetType().Name}) | activo: {c.enabled} | trigger: {c.isTrigger}{extra}", this);
        }

        Debug.Log($"[Diag] Capa del objeto: '{LayerMask.LayerToName(gameObject.layer)}' | Interaction Layer Mask (valor): {interactable.interactionLayers.value}", this);

        interactors = FindObjectsByType<XRBaseInteractor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        pressed = new bool[interactors.Length];
        Debug.Log($"[Diag] Interactores en la escena: {interactors.Length}", this);
        foreach (var i in interactors)
        {
            bool shares = (i.interactionLayers.value & interactable.interactionLayers.value) != 0;
            Debug.Log($"[Diag]   - '{i.name}' ({i.GetType().Name}) | activo: {i.isActiveAndEnabled} | comparte capa: {shares}", i);
        }
    }

    // El resaltado de hover solo se muestra si es el turno de ese anillo
    bool HoverAllowed()
    {
        if (ring == null) return true;
        if (ring.IsSolved) return false;
        return ring.sequence == null || ring.sequence.CanSolve(ring);
    }

    void Update()
    {
        bool allowed = HoverAllowed();
        if (allowed != lastAllowed)
        {
            lastAllowed = allowed;
            Refresh();
        }

        if (interactors == null) return;

        // Avisa cuando el input de seleccion (pellizco o gatillo) se activa o se suelta
        for (int k = 0; k < interactors.Length; k++)
        {
            if (interactors[k] is XRBaseInputInteractor inp && inp.isActiveAndEnabled)
            {
                bool now = inp.selectInput.ReadIsPerformed();
                if (now != pressed[k])
                {
                    pressed[k] = now;
                    Debug.Log($"[Diag] INPUT DE SELECCION en '{inp.name}': {(now ? "PRESIONADO" : "SOLTADO")} (valor {inp.selectInput.ReadValue():F2})", this);
                }
            }
        }

        if (!logDistances || targetRenderer == null || Time.time < nextLog) return;
        nextLog = Time.time + 1f;

        foreach (var i in interactors)
        {
            if (i == null || !i.isActiveAndEnabled) continue;
            float d = Vector3.Distance(i.transform.position, targetRenderer.bounds.ClosestPoint(i.transform.position));
            Debug.Log($"[Diag] Distancia '{i.name}' -> anillo: {d:F2} m", this);
        }
    }
}