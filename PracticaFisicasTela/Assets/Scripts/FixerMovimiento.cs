using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FixerMovimiento : MonoBehaviour
{
    public bool moverFixer = false;
    public float velocidad = 1f;

    public float distanciaMaxima = 3f;
    private Vector3 posicionInicial;


    private void Start()
    {
        posicionInicial = transform.position;
    }
    void Update()
    {
        if (moverFixer)
        {
            float movimiento = Mathf.Sin(Time.time * velocidad) * distanciaMaxima;
            transform.position = new Vector3(posicionInicial.x + movimiento, posicionInicial.y, posicionInicial.z);
        }
    }
}
