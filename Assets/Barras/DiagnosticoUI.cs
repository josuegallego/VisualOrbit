using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Diagnóstico automático de la escena: revisa las causas más comunes de
/// que los botones/sliders de UI no respondan (falta de EventSystem,
/// Canvas World Space sin Event Camera, Raycaster incorrecto para XR,
/// botones mal configurados, falta el GestorBarras, etc.) y las imprime
/// en la consola con banderas ✅ / ⚠️ / ❌.
///
/// CÓMO USARLO:
/// 1. Crea un GameObject vacío (ej. "Diagnostico") y agrégale este script.
/// 2. Click derecho sobre el componente en el Inspector → "Ejecutar Diagnóstico".
///    (También corre automáticamente al entrar en Play, en Start()).
/// 3. Lee los resultados en la Console.
/// </summary>
public class DiagnosticoUI : MonoBehaviour
{
    [Tooltip("Nombres de los botones de opción a revisar (deben existir en la escena)")]
    public string[] nombresBotones = { "OP1", "OP2" };

    void Start()
    {
        EjecutarDiagnostico();
    }

    [ContextMenu("Ejecutar Diagnóstico")]
    public void EjecutarDiagnostico()
    {
        Debug.Log("========== DIAGNÓSTICO UI/XR ==========");

        RevisarEventSystem();
        RevisarCanvases();
        RevisarBotones();
        RevisarGestorBarras();
        RevisarSliders();

        Debug.Log("========== FIN DEL DIAGNÓSTICO ==========");
    }

    private void RevisarEventSystem()
    {
        EventSystem eventSystem = FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            Debug.LogError("❌ No hay ningún EventSystem en la escena. Sin esto, NINGÚN botón de UI puede responder. Solución: click derecho en Hierarchy → UI → Event System.");
            return;
        }

        Debug.Log($"✅ EventSystem encontrado: '{eventSystem.gameObject.name}'");

        BaseInputModule modulo = eventSystem.GetComponent<BaseInputModule>();
        if (modulo == null)
        {
            Debug.LogError("❌ El EventSystem no tiene ningún Input Module. Agrégale 'Input System UI Input Module' o 'Standalone Input Module'.");
        }
        else
        {
            Debug.Log($"ℹ️ Input Module activo: '{modulo.GetType().Name}'. Si es un módulo de XR (ej. XRUIInputModule), los clicks de mouse normales NO van a funcionar sin un XR Device Simulator o un controlador real.");
        }
    }

    private void RevisarCanvases()
    {
        Canvas[] canvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        if (canvases.Length == 0)
        {
            Debug.LogError("❌ No se encontró ningún Canvas en la escena.");
            return;
        }

        foreach (Canvas canvas in canvases)
        {
            if (!canvas.gameObject.activeInHierarchy) continue;

            Debug.Log($"--- Canvas '{canvas.gameObject.name}' (RenderMode: {canvas.renderMode}) ---");

            if (canvas.renderMode == RenderMode.WorldSpace && canvas.worldCamera == null)
            {
                Debug.LogError($"❌ El Canvas '{canvas.gameObject.name}' está en World Space pero NO tiene 'Event Camera' asignada. Los clicks nunca se van a detectar. Asígnale tu cámara (Main Camera o la cámara del XR Rig).");
            }
            else if (canvas.renderMode == RenderMode.WorldSpace)
            {
                Debug.Log($"✅ Event Camera asignada: '{canvas.worldCamera.name}'");
            }

            GraphicRaycaster raycaster = canvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                Debug.LogError($"❌ El Canvas '{canvas.gameObject.name}' no tiene ningún GraphicRaycaster (ni normal ni TrackedDevice). Sin esto, ningún click ni rayo va a detectar los botones.");
            }
            else
            {
                Debug.Log($"✅ Raycaster presente: '{raycaster.GetType().Name}' " +
                    (raycaster.GetType().Name == "TrackedDeviceGraphicRaycaster"
                        ? "(pensado para rayos de controlador XR, no para mouse plano)"
                        : "(pensado para mouse/touch estándar)"));
            }
        }
    }

    private void RevisarBotones()
    {
        foreach (string nombre in nombresBotones)
        {
            GameObject obj = GameObject.Find(nombre);
            if (obj == null)
            {
                Debug.LogWarning($"⚠️ No se encontró ningún GameObject llamado '{nombre}' en la escena (¿está desactivado o se llama distinto?).");
                continue;
            }

            Button boton = obj.GetComponent<Button>();
            if (boton == null)
            {
                Debug.LogError($"❌ '{nombre}' no tiene un componente Button. Tiene: {string.Join(", ", GetNombresComponentes(obj))}");
                continue;
            }

            if (!boton.interactable)
            {
                Debug.LogWarning($"⚠️ El botón '{nombre}' tiene 'Interactable' desmarcado.");
            }

            Graphic grafico = obj.GetComponent<Graphic>();
            if (grafico == null || !grafico.raycastTarget)
            {
                Debug.LogWarning($"⚠️ El botón '{nombre}' no tiene un Graphic (Image/Text) con 'Raycast Target' activo, así que no puede recibir clicks/rayos.");
            }

            if (boton.onClick.GetPersistentEventCount() == 0)
            {
                Debug.Log($"ℹ️ '{nombre}' no tiene listeners asignados en el Inspector (OnClick), pero eso es normal si OpcionSimulada los agrega por código en Start().");
            }

            Debug.Log($"✅ Botón '{nombre}' configurado correctamente (Button + Interactable + Raycast Target).");
        }
    }

    private void RevisarGestorBarras()
    {
        var gestor = FindFirstObjectByType<GestorBarras>();
        if (gestor == null)
        {
            Debug.LogError("❌ No hay ningún GestorBarras en la escena. OpcionSimulada no va a poder aplicar cambios a las barras.");
        }
        else
        {
            Debug.Log($"✅ GestorBarras encontrado en '{gestor.gameObject.name}' con {gestor.barras.Count} barra(s) registradas.");
            if (gestor.barras.Count == 0)
            {
                Debug.LogWarning("⚠️ La lista 'barras' del GestorBarras está vacía. Arrastra tus BarraAlimentaria ahí.");
            }
        }
    }

    private void RevisarSliders()
    {
        var barras = FindObjectsByType<BarraAlimentaria>(FindObjectsSortMode.None);
        if (barras.Length == 0)
        {
            Debug.LogWarning("⚠️ No se encontró ningún componente BarraAlimentaria en la escena.");
            return;
        }

        foreach (var barra in barras)
        {
            if (string.IsNullOrEmpty(barra.idBarra))
            {
                Debug.LogError($"❌ La barra en '{barra.gameObject.name}' no tiene 'idBarra' asignado.");
            }

            if (barra.sliderUI == null && barra.imagenFillUI == null)
            {
                Debug.LogError($"❌ La barra '{barra.idBarra}' no tiene ni Slider UI ni Imagen Fill UI asignados: no va a mostrar nada visualmente.");
            }
            else
            {
                Debug.Log($"✅ Barra '{barra.idBarra}' conectada a UI correctamente.");
            }
        }
    }

    private string[] GetNombresComponentes(GameObject obj)
    {
        Component[] componentes = obj.GetComponents<Component>();
        string[] nombres = new string[componentes.Length];
        for (int i = 0; i < componentes.Length; i++)
            nombres[i] = componentes[i].GetType().Name;
        return nombres;
    }
}