using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovimientoEsfera : MonoBehaviour
{
    public bool moverEsfera = false;
    public float velocidad = 0.5f;

    // Update is called once per frame
    void Update()
    {
        if (moverEsfera)
        {
            transform.position += (new Vector3(0f, 0f, 1f)) * velocidad * Time.deltaTime;

            if (transform.position.z > 30)
            {
                transform.position = new Vector3(0f, 2f, -6f);
            }
        }
    }
}
