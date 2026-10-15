
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HiloLuz3D : MonoBehaviour
{
    [Header("Forma 3D")]
    [SerializeField] private int cantidadPuntos = 80;
    [SerializeField] private float longitud = 3f;
    [SerializeField] private float curvaX = 0.15f;
    [SerializeField] private float curvaY = 0.3f;
    [SerializeField] private float curvaZ = 0.25f;
    [SerializeField] private float ondulaciones = 2f;

    [Header("Movimiento de la curva")]
    [SerializeField] private bool animarCurva = true;
    [SerializeField] private float velocidadCurva = 0.7f;

    [Header("Pulso visual del hilo")]
    [SerializeField] private bool activarPulso = true;
    [SerializeField] private float velocidadPulso = 0.5f;
    [SerializeField, Range(0.01f, 0.3f)]
    private float anchoPulso = 0.06f;

    [Header("Emision del hilo")]
    [SerializeField]
    private Color colorLuz =
        new Color(0.1f, 0.7f, 1f);

    [SerializeField] private float brilloMinimo = 0.5f;
    [SerializeField] private float brilloMaximo = 3f;
    [SerializeField] private float velocidadBrillo = 2f;

    [Header("Luz real viajera")]
    [SerializeField] private bool crearLuzViajera = true;
    [SerializeField] private float intensidadLuz = 3f;
    [SerializeField] private float alcanceLuz = 1.5f;
    [SerializeField] private float velocidadLuz = 0.5f;
    [SerializeField] private Color colorLuzViajera = Color.cyan;
    [SerializeField] private float intensidadMinimaLuz = 0.1f;
    [SerializeField] private float velocidadPulsoLuz = 6f;

    private LineRenderer linea;
    private Material materialInstancia;
    private Vector3[] puntos;

    private Light luzViajera;
    private Transform puntoLuz;

    private void Awake()
    {
        linea = GetComponent<LineRenderer>();

        cantidadPuntos = Mathf.Max(2, cantidadPuntos);
        linea.positionCount = cantidadPuntos;
        linea.useWorldSpace = false;

        puntos = new Vector3[cantidadPuntos];

        // Crear una instancia independiente del material.
        if (linea.sharedMaterial != null)
        {
            materialInstancia = new Material(linea.sharedMaterial);
            linea.material = materialInstancia;
        }

        linea.startColor = colorLuz;
        linea.endColor = colorLuz;

        // Crear la luz real que recorrerá el hilo.
        if (crearLuzViajera)
        {
            GameObject objetoLuz = new GameObject("LuzViajera");

            puntoLuz = objetoLuz.transform;
            puntoLuz.SetParent(transform, false);

            luzViajera = objetoLuz.AddComponent<Light>();
            luzViajera.type = LightType.Point;
            luzViajera.color = colorLuzViajera;
            luzViajera.intensity = intensidadLuz;
            luzViajera.range = alcanceLuz;
            luzViajera.shadows = LightShadows.None;
        }
    }

    private void Update()
    {
        DibujarCurva();
        ActualizarEmision();
        ActualizarPulso();
        ActualizarLuzViajera();
    }

    private void DibujarCurva()
    {
        float tiempo = animarCurva
            ? Time.time * velocidadCurva
            : 0f;

        for (int i = 0; i < cantidadPuntos; i++)
        {
            float t = i / (float)(cantidadPuntos - 1);
            float onda = t * ondulaciones * Mathf.PI * 2f;

            float envolvente = Mathf.Sin(t * Mathf.PI);

            float x = t * longitud
                + Mathf.Sin(onda + tiempo) * curvaX * envolvente;

            float y = Mathf.Sin(onda + tiempo * 1.2f)
                * curvaY * envolvente;

            float z = Mathf.Cos(onda + tiempo * 0.8f)
                * curvaZ * envolvente;

            puntos[i] = new Vector3(x, y, z);
            linea.SetPosition(i, puntos[i]);
        }
    }

    private void ActualizarEmision()
    {
        float t = (Mathf.Sin(
            Time.time * velocidadBrillo * Mathf.PI * 2f
        ) + 1f) * 0.5f;

        float brillo = Mathf.Lerp(
            brilloMinimo,
            brilloMaximo,
            t
        );

        Color emision = colorLuz * brillo;

        if (materialInstancia != null &&
            materialInstancia.HasProperty("_EmissionColor"))
        {
            materialInstancia.EnableKeyword("_EMISSION");
            materialInstancia.SetColor("_EmissionColor", emision);
        }

        // Pulso visible de la línea.
        float brilloVisible = Mathf.Lerp(0.25f, 1f, t);
        linea.startColor = colorLuz * brilloVisible;
        linea.endColor = colorLuz * brilloVisible;
    }

    private void ActualizarPulso()
    {
        if (!activarPulso)
        {
            linea.colorGradient = CrearGradienteUniforme();
            return;
        }

        float posicion = Mathf.Repeat(
            Time.time * velocidadPulso,
            1f
        );

        float inicio = Mathf.Clamp01(posicion - anchoPulso);
        float final = Mathf.Clamp01(posicion + anchoPulso);

        Gradient gradiente = new Gradient();

        GradientColorKey[] colores =
        {
            new GradientColorKey(colorLuz, 0f),
            new GradientColorKey(Color.white, inicio),
            new GradientColorKey(Color.white, posicion),
            new GradientColorKey(colorLuz, final),
            new GradientColorKey(colorLuz, 1f)
        };

        GradientAlphaKey[] transparencias =
        {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        };

        gradiente.SetKeys(colores, transparencias);
        linea.colorGradient = gradiente;
    }

    private Gradient CrearGradienteUniforme()
    {
        Gradient gradiente = new Gradient();

        gradiente.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(colorLuz, 0f),
                new GradientColorKey(colorLuz, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        return gradiente;
    }

    private void ActualizarLuzViajera()
    {
        if (luzViajera == null || puntoLuz == null || puntos == null)
            return;

        // Avanzar por los puntos de la curva.
        float t = Mathf.Repeat(Time.time * velocidadLuz, 1f);

        float indice = t * (cantidadPuntos - 1);
        int anterior = Mathf.FloorToInt(indice);
        int siguiente = Mathf.Min(anterior + 1, cantidadPuntos - 1);

        float interpolacion = indice - anterior;

        Vector3 posicion = Vector3.Lerp(
            puntos[anterior],
            puntos[siguiente],
            interpolacion
        );

        puntoLuz.localPosition = posicion;

        // La luz se enciende y se atenúa periódicamente.
        float pulso = (Mathf.Sin(
            Time.time * velocidadPulsoLuz * Mathf.PI * 2f
        ) + 1f) * 0.5f;

        luzViajera.intensity = Mathf.Lerp(
            intensidadMinimaLuz,
            intensidadLuz,
            pulso
        );

        luzViajera.color = colorLuzViajera;
        luzViajera.range = alcanceLuz;
    }

    private void OnDestroy()
    {
        if (materialInstancia != null)
        {
            Destroy(materialInstancia);
        }
    }
}
