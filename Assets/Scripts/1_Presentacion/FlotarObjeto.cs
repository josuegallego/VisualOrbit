
using UnityEngine;

public class FlotarObjeto : MonoBehaviour
{
    [Header("Flotación")]
    [SerializeField] private float altura = 0.25f;
    [SerializeField] private float velocidad = 1.5f;

    [Header("Rotación")]
    [SerializeField] private bool rotar = true;
    [SerializeField] private float velocidadRotacion = 20f;

    private Vector3 posicionInicial;

    private void Start()
    {
        posicionInicial = transform.localPosition;
    }

    private void Update()
    {
        // Movimiento suave hacia arriba y abajo
        float movimiento = Mathf.Sin(Time.time * velocidad) * altura;

        transform.localPosition = posicionInicial
            + Vector3.up * movimiento;

        // Rotación lenta sobre el eje vertical
        if (rotar)
        {
            transform.Rotate(
                Vector3.up,
                velocidadRotacion * Time.deltaTime,
                Space.Self
            );
        }
    }
}
