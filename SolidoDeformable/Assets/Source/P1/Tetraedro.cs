using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tetraedro
{
    public Node a, b, c, d;
    public Tetraedro(Node a, Node b, Node c, Node d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}
