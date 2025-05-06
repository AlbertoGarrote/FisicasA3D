using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Net.NetworkInformation;
using System;
using System.Globalization;
using System.Reflection;

/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
public class SolidDeformation : MonoBehaviour
{
    /// <summary>
    /// Default constructor. Zero all. 
    /// </summary>
    public SolidDeformation()
    {
        this.Paused = true;
        this.TimeStep = 0.01f;
        this.Gravity = new Vector3(0.0f, -9.81f, 0.0f);
        this.IntegrationMethod = Integration.Symplectic;
    }

    /// <summary>
    /// Integration method.
    /// </summary>
    public enum Integration
    {
        Explicit = 0,
        Symplectic = 1,
    };

    #region InEditorVariables

    public bool Paused;
    public float TimeStep;
    public int subSteps = 10;
    public Vector3 Gravity;
    public Integration IntegrationMethod;
    public float massNodes;

    [SerializeField] public List<Node> nodes;
    [SerializeField] public List<Spring> springsTraccion;
    //[SerializeField] public List<Spring> springsFlexion;

    //public Mesh mesh;
    public Vector3[] vertices;
    public float stiffnessSpringTraccion;
    //public float stiffnessSpringFlexion;

    public List<GameObject> fixers;

    public float dampingMuelle = 0.1f;
    public float dampingNodo = 0.1f;

    public Vector3 viento = Vector3.zero;
    public float friccionViento = 0.5f;

    public float k_penalty = 100;
    public GameObject esferaPenalty;

    public TextAsset fileName;

    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        //mesh = GetComponent<MeshFilter>().mesh;
        InicializarNodos();
        InicializarMuelles();
        EncontrarFixers();
        FijarNodosFixer();
    }



    public void Update()
    {
        if (Input.GetKeyUp(KeyCode.P))
            this.Paused = !this.Paused;
    }

    public void FixedUpdate()
    {
        if (this.Paused)
            return; // Not simulating

        float subTimeStep = TimeStep / subSteps;

        //for (int i = 0; i < subSteps; i++)
        //{
            // Select integration method
            switch (this.IntegrationMethod)
            {
                case Integration.Explicit: this.stepExplicit(); break;
                case Integration.Symplectic: this.stepSymplectic(); break;
                default:
                    throw new System.Exception("[ERROR] Should never happen!");
            }
        //}
        // actualizamos la mesh despu s de hacer todos los supSteps
        ActualizarMesh();

    }

    #endregion

    /// <summary>
    /// Performs a simulation step in 1D using Explicit integration.
    /// </summary>
    private void stepExplicit()
    {
    }

    /// <summary>
    /// Performs a simulation step in 1D using Symplectic integration.
    /// </summary>
    private void stepSymplectic()
    {
        // fuerzas nodos
        foreach (Node node in nodes)
        {
            node.force = Vector3.zero;
            node.ComputeForces(Gravity);
        }

        // aplicar fuerzas de los muelles
        foreach (Spring springT in springsTraccion)
        {
            springT.ComputeForces(dampingMuelle);
        }

        // integrar Euler Simplectico
        foreach (Node node in nodes)
        {
            if (node.isFixed && node.fixer != null) // movimiento con el fixer
            {
                node.pos = node.fixer.position + node.diferenciaFixer;
                node.vel = Vector3.zero;
            }
            if (!node.isFixed)
            {
                node.vel += TimeStep / node.mass * node.force;
                node.pos += TimeStep * node.vel;
            }
        }

        foreach (Spring springT in springsTraccion)
        {
            springT.UpdateLength();
        }

        //ActualizarMesh();
    }

    void ActualizarMesh()
    {
        //for (int i = 0; i < nodes.Count; i++)
        //{
        //    vertices[i] = transform.InverseTransformPoint(nodes[i].pos);
        //}

    }

    void InicializarNodos()
    {
        nodes = new List<Node>();

        //for (int i = 0; i < vertices.Length; i++)
        //{
        //    Vector3 worldPos = transform.TransformPoint(vertices[i]);
        //    nodes.Add(new Node(worldPos, massNodes));
        //}

        string[] textString = fileName.text.Split(new string[] { " ", "\n", "\r" }, System.StringSplitOptions.RemoveEmptyEntries);
        CultureInfo locale = new CultureInfo("en-US");

        int numNodes = int.Parse(textString[0]);

        //nodes.Add(new Node(new Vector3(0, 0, 0), massNodes)); // 0
        //nodes.Add(new Node(new Vector3(5, 0, 0), massNodes)); // 1
        //nodes.Add(new Node(new Vector3(2.5f, 5, 0), massNodes)); // 2
        //nodes.Add(new Node(new Vector3(2.5f, 2.5f, 5), massNodes)); // 3

        int index = 1;

        for (int i = 0; i < numNodes; i++)
        {
            float x = float.Parse(textString[index++], locale);
            float y = float.Parse(textString[index++], locale);
            float z = float.Parse(textString[index++], locale);

            Vector3 position = new Vector3(x, y, z);
            Node node = new Node(position, massNodes);
            nodes.Add(node);
        }
    }

    void InicializarMuelles()
    {
        AgregarMuelleTraccion(0, 1);
        AgregarMuelleTraccion(0, 2);
        AgregarMuelleTraccion(0, 3);
        AgregarMuelleTraccion(1, 2);
        AgregarMuelleTraccion(1, 3);
        AgregarMuelleTraccion(2, 3);
    }

    // MUELLES DE TRACCION
    void AgregarMuelleTraccion(int i, int j)
    {
        springsTraccion.Add(new Spring(nodes[i], nodes[j], stiffnessSpringTraccion));
    }

    //FIXER
    void EncontrarFixers()
    {
        GameObject[] fixersObj = GameObject.FindGameObjectsWithTag("fixer");
        fixers = new List<GameObject>(fixersObj);
    }

    void FijarNodosFixer()
    {
        foreach (var fixer in fixers)
        {
            Collider colliderFixed = fixer.GetComponent<Collider>();

            foreach (Node node in nodes)
            {
                if (colliderFixed.bounds.Contains(node.pos))
                {
                    node.isFixed = true;
                    node.fixer = fixer.transform;
                    node.diferenciaFixer = node.pos - fixer.transform.position;
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (nodes == null || springsTraccion == null) return;

        Gizmos.color = Color.blue;
        foreach (Node node in nodes)
        {
            Gizmos.DrawSphere(node.pos, 0.05f);
        }

        Gizmos.color = Color.red;
        foreach (Spring spring in springsTraccion)
        {
            Gizmos.DrawLine(spring.nodeA.pos, spring.nodeB.pos);
        }
    }

}


