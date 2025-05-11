using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tetraedro
{
    public Node a, b, c, d;
    public float volumenTetra;
    public Tetraedro(float volumen, Node a, Node b, Node c, Node d)
    {
        this.volumenTetra = volumen;
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}
