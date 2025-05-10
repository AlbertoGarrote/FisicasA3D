using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Tetraedro
{
    public Node a, b, c, d;
    public int tetraID;
    public Tetraedro(int id, Node a, Node b, Node c, Node d)
    {
        this.tetraID = id;
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}
