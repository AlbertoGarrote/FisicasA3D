using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[System.Serializable]
public class Arista
{
    public Node nodoA, nodoB;
    public float aristaVolumen;

    public Arista(Node v1, Node v2, float aristaVolumen)
    {
        if (CompararVectores(v1.pos, v2.pos) < 0)// ordenar extremos para evitar duplicados
        {
            nodoA = v1;
            nodoB = v2;
        }
        else
        {
            nodoA = v2;
            nodoB = v1;
        }

        this.aristaVolumen = aristaVolumen;
    }

    public bool mismaArista(Arista a2)
    {
        return nodoA.pos == a2.nodoA.pos && nodoB.pos == a2.nodoB.pos;
    }

    private int CompararVectores(Vector3 p1, Vector3 p2) 
    {
        int compX = p1.x.CompareTo(p2.x);
        if (compX != 0) return compX;

        int compY = p1.y.CompareTo(p2.y);
        if (compY != 0) return compY;

        return p1.z.CompareTo(p2.z);
    }
}
