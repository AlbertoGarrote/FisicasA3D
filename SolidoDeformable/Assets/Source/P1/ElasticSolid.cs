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
public class ElasticSolid : MonoBehaviour
{
    /// <summary>
    /// Default constructor. Zero all. 
    /// </summary>
    public ElasticSolid()
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
    [SerializeField] public List<Tetraedro> tetraedros;
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

    public TextAsset nodeFile, eleFile;

    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        //mesh = GetComponent<MeshFilter>().mesh;
        InicializarNodos();
        InicializarTetraedros();
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
        string[] lines = nodeFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        nodes = new List<Node>();

        CultureInfo locale = new CultureInfo("en-US");
        int numNodes = int.Parse(lines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0]);

        for (int i = 1; i <= numNodes; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);

            float x = float.Parse(parts[1], locale);
            float y = float.Parse(parts[2], locale);
            float z = float.Parse(parts[3], locale);

            Vector3 position = new Vector3(x, y, z);
            Node node = new Node(position, massNodes);
            nodes.Add(node);


        }
    }

    void InicializarMuelles()
    {
        //AgregarMuelleTraccion(0, 1);
        //AgregarMuelleTraccion(0, 2);
        //AgregarMuelleTraccion(0, 3);
        //AgregarMuelleTraccion(1, 2);
        //AgregarMuelleTraccion(1, 3);
        //AgregarMuelleTraccion(2, 3);
        foreach (Tetraedro tetra in tetraedros)
        {
            AgregarMuelleTraccion(tetra.a, tetra.b);
            AgregarMuelleTraccion(tetra.a, tetra.c);
            AgregarMuelleTraccion(tetra.a, tetra.d);
            AgregarMuelleTraccion(tetra.b, tetra.c);
            AgregarMuelleTraccion(tetra.b, tetra.d);
            AgregarMuelleTraccion(tetra.c, tetra.d);
        }

        
    }

    // MUELLES DE TRACCION
    void AgregarMuelleTraccion(Node i, Node j)
    {
        springsTraccion.Add(new Spring(i, j, stiffnessSpringTraccion));
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
        if (nodes == null || springsTraccion == null || tetraedros == null) return;

        Gizmos.color = Color.blue;

        foreach (Tetraedro tetra in tetraedros)
        {
            Gizmos.DrawSphere(tetra.a.pos, 0.10f);
            Gizmos.DrawSphere(tetra.b.pos, 0.10f);
            Gizmos.DrawSphere(tetra.c.pos, 0.10f);
            Gizmos.DrawSphere(tetra.d.pos, 0.10f);
        }
        Gizmos.color = Color.red;
        foreach (Spring spring in springsTraccion)
        {
            Gizmos.DrawLine(spring.nodeA.pos, spring.nodeB.pos);
        }
    }


    void InicializarTetraedros()
    {
        tetraedros = new List<Tetraedro>();

        string[] lines = eleFile.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);

        int numTetra = int.Parse(lines[0].Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries)[0]);

        for (int i = 1; i <= numTetra; i++)
        {
            string[] parts = lines[i].Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);

            int idx0 = int.Parse(parts[1]) - 1;
            int idx1 = int.Parse(parts[2]) - 1;
            int idx2 = int.Parse(parts[3]) - 1;
            int idx3 = int.Parse(parts[4]) - 1;

            Tetraedro t = new Tetraedro(nodes[idx0], nodes[idx1], nodes[idx2], nodes[idx3]);
            tetraedros.Add(t);
        }
    }
}



