using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

[System.Serializable]
public class Node
{
    public Vector3 pos;
    public Vector3 vel;
    public Vector3 force;

    public float mass;
    public bool isFixed;
    public Transform fixer;
    public Vector3 diferenciaFixer;

    public Node(Vector3 posicion, float massNode)
    {
        pos = posicion;
        vel = Vector3.zero;
        force = Vector3.zero;
        mass = massNode;
        isFixed = false;
        fixer = null;
        diferenciaFixer = Vector3.zero;
    }

    public void ComputeForces(Vector3 gravity, float damping)
    {
        force += mass * gravity;

        // amortiguamiento
        float dampingFactor = damping * mass;
        Vector3 amortiguamiento = -dampingFactor * vel; // multiplicamos por la masa para hacerlo proporcional 
        force += amortiguamiento;
    }
}

