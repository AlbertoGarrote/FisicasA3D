using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Spring
{

    public Node nodeA, nodeB;

    public float Length0;
    public float Length;

    public float stiffness, volumen;

    // Use this for initialization
    public Spring(Node a, Node b, float stiffness, float _volumen)
    {
        this.nodeA = a;
        this.nodeB = b;
        Length = Vector3.Distance(nodeA.pos, nodeB.pos);
        this.stiffness = stiffness;
        Length0 = Length;
        this.volumen = _volumen;
    }

    public void UpdateLength()
    {
        Length = (nodeA.pos - nodeB.pos).magnitude;
    }

    public void ComputeForces(float dampingFactor)
    {
        Vector3 u = nodeA.pos - nodeB.pos;
        u.Normalize();
        Vector3 force = -(volumen / Mathf.Pow(Length0, 2)) * stiffness * (Length - Length0) * u;

        // amortiguamineto de muelle
        float dampingSpring = dampingFactor * (stiffness * 0.005f); // proporcional a la rigidez
        Vector3 velocidadRelativa = nodeA.vel - nodeB.vel;
        float dampingMag = Vector3.Dot(velocidadRelativa, u); // producto escalar direccion del amortiguamiento
        Vector3 dampingForce = -dampingSpring * dampingMag * u;

        nodeA.force += force + dampingForce;
        nodeB.force -= force + dampingForce;

    }
}

