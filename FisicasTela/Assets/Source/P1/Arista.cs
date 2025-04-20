using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Arista
{
    public int a, b, c; 

    public Arista(int v1, int v2, int v3)
    {
        if (v1 < v2)// ordenar extremos para evitar duplicados
        {
            a = v1;
            b = v2;
        }
        else
        {
            a = v2;
            b = v1;
        }

        this.c = v3;
    }

    public bool mismaArista(Arista a2)
    {
        return this.a == a2.a && this.b == a2.b;
    }
}
