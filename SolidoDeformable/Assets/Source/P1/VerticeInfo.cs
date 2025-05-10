
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]

public class VerticeInfo
{
    public int verticeID;
    public Tetraedro tetraContenedor;
    public float[] pesos;
    public VerticeInfo(int _verticeID, Tetraedro tetra, float[] pesosT)
    {
        this.verticeID = _verticeID;
        this.tetraContenedor = tetra;
        this.pesos = pesosT;
    }
}
