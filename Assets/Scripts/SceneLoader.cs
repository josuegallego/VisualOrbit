using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Carga una escena por nombre o por índice.
/// 
/// USO EN EL INSPECTOR:
///   1. Añade este componente al objeto que tenga el botón (o a cualquier objeto de la escena).
///   2. En el Button, en "On Click ()", pulsa "+".
///   3. Arrastra el objeto que tiene este script.
///   4. En el desplegable, elige:
///        - SceneLoader.LoadSceneByName  → escribe el nombre en el campo de texto.
///        - SceneLoader.LoadSceneByIndex → escribe el número en el campo de texto.
///        - SceneLoader.ReloadCurrentScene → sin parámetros.
///        - SceneLoader.LoadNextScene    → sin parámetros.
/// </summary>
public class SceneLoader : MonoBehaviour
{
    [Header("Opcional: precargar una escena al iniciar")]
    [Tooltip("Nombre de la escena que se cargará automáticamente al Start. Déjalo vacío si no quieres.")]
    [SerializeField] private string autoLoadOnStart = "";

    [Header("Opcional: mostrar logs")]
    [SerializeField] private bool debugLogs = true;

    private void Start()
    {
        if (!string.IsNullOrEmpty(autoLoadOnStart))
        {
            LoadSceneByName(autoLoadOnStart);
        }
    }

    /// <summary>Carga una escena por su nombre (debe estar en Build Settings).</summary>
    public void LoadSceneByName(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("[SceneLoader] El nombre de la escena está vacío.");
            return;
        }

        if (debugLogs)
            Debug.Log($"[SceneLoader] Cargando escena por nombre: '{sceneName}'");

        SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    /// <summary>Carga una escena por su índice en Build Settings.</summary>
    public void LoadSceneByIndex(int sceneIndex)
    {
        if (sceneIndex < 0 || sceneIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogError($"[SceneLoader] Índice de escena inválido: {sceneIndex}. " +
                           $"Escenas en Build Settings: {SceneManager.sceneCountInBuildSettings}");
            return;
        }

        if (debugLogs)
            Debug.Log($"[SceneLoader] Cargando escena por índice: {sceneIndex}");

        SceneManager.LoadScene(sceneIndex, LoadSceneMode.Single);
    }

    /// <summary>Recarga la escena actual.</summary>
    public void ReloadCurrentScene()
    {
        var current = SceneManager.GetActiveScene();
        if (debugLogs)
            Debug.Log($"[SceneLoader] Recargando escena: '{current.name}'");

        SceneManager.LoadScene(current.buildIndex, LoadSceneMode.Single);
    }

    /// <summary>Carga la siguiente escena en el orden de Build Settings.</summary>
    public void LoadNextScene()
    {
        int next = SceneManager.GetActiveScene().buildIndex + 1;

        if (next >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("[SceneLoader] No hay siguiente escena. Volviendo a la primera.");
            next = 0;
        }

        if (debugLogs)
            Debug.Log($"[SceneLoader] Cargando siguiente escena (índice {next}).");

        SceneManager.LoadScene(next, LoadSceneMode.Single);
    }

    /// <summary>Sale de la aplicación (útil para "Salir" en VR).</summary>
    public void QuitApplication()
    {
        if (debugLogs)
            Debug.Log("[SceneLoader] Saliendo de la aplicación...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}