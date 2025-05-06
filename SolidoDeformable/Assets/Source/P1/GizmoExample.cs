using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoExample : MonoBehaviour {

    public Vector3 pos1, pos2;

	void Start () {

        pos1 = new Vector3(0.0f, 0.0f, 0.0f);
        pos2 = pos1 + Vector3.up;
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(pos1, 0.2f);
        Gizmos.DrawSphere(pos2, 0.2f);
        Gizmos.color = Color.red;
        Gizmos.DrawLine(pos1, pos2);
        //pos2 += 0.01f * Vector3.up;
    }

}
