using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Spring
{

    public Node nodeA, nodeB;

    public float Length0;
    public float Length;

    public float stiffness;

    // Use this for initialization
    public Spring(Node a, Node b, float stiffness)
    {
        this.nodeA = a;
        this.nodeB = b;
        Length = Vector3.Distance(nodeA.pos, nodeB.pos);
        this.stiffness = stiffness;
        Length0 = Length;
    }

    // Update is called once per frame
    //void Update () {
    //       transform.localScale = new Vector3(transform.localScale.x, Length / 2.0f, transform.localScale.z);
    //       transform.position = 0.5f * (nodeA.pos + nodeB.pos);

    //       Vector3 u = nodeA.pos - nodeB.pos;
    //       u.Normalize();
    //       transform.rotation = Quaternion.FromToRotation(Vector3.up, u);
    //   }

    public void UpdateLength()
    {
        Length = (nodeA.pos - nodeB.pos).magnitude;
    }

    public void ComputeForces(float dampingFactor)
    {
        Vector3 u = nodeA.pos - nodeB.pos;
        u.Normalize();
        Vector3 force = -stiffness * (Length - Length0) * u;

        // amortiguamineto de muelle
        //float dampingSpring = dampingFactor * (stiffness * 0.005f); // proporcional a la rigidez
        //Vector3 velocidadRelativa = nodeA.vel - nodeB.vel;
        //float dampingMag = Vector3.Dot(velocidadRelativa, u); // producto escalar direccion del amortiguamiento
        //Vector3 dampingForce = -dampingSpring * dampingMag * u;

        //nodeA.force += force + dampingForce;
        //nodeB.force -= force + dampingForce;

        nodeA.force += force;
        nodeB.force -= force;
    }
}

