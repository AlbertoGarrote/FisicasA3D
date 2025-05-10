using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Net.NetworkInformation;
using System;
using System.Globalization;
using System.Reflection;
using System.Linq;
using UnityEngine.UIElements;

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
    [SerializeField] public List<VerticeInfo> verticesContenidos;
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

    // solido deformable
    public TextAsset nodeFile, eleFile;
    private Mesh meshAsset;

    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        //mesh = GetComponent<MeshFilter>().mesh;
        InicializarNodos();
        InicializarTetraedros();
        InicializarBlend();
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
        //ActualizarMesh();

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

        ActualizarMesh();
    }

    void ActualizarMesh()
    {

        Mesh visualMesh = this.GetComponentInChildren<MeshFilter>().mesh;
        Vector3[] updatedVertices = new Vector3[visualMesh.vertexCount];
        Transform meshTransform = GetComponentInChildren<MeshFilter>().transform;

        foreach (VerticeInfo verticeCont in verticesContenidos)
        {
            Tetraedro t = verticeCont.tetraContenedor;
            float[] w = verticeCont.pesos;

            Vector3 newPos = w[0] * t.a.pos + w[1] * t.b.pos + w[2] * t.c.pos + w[3] * t.d.pos;
            updatedVertices[verticeCont.verticeID] = meshTransform.transform.InverseTransformPoint(newPos);
        }

        visualMesh.vertices = updatedVertices;
        visualMesh.RecalculateNormals();

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

            Tetraedro t = new Tetraedro(i - 1, nodes[idx0], nodes[idx1], nodes[idx2], nodes[idx3]);
            tetraedros.Add(t);
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

    void InicializarBlend()
    {
        meshAsset = this.GetComponentInChildren<MeshFilter>().mesh;
        vertices = meshAsset.vertices;
        Transform transformMesh = GetComponentInChildren<MeshFilter>().transform;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 verticeWorl = transformMesh.TransformPoint(vertices[i]);

            foreach (Tetraedro tetra in tetraedros)
            {
                float[] w = CalcularPesos(verticeWorl, tetra.a.pos, tetra.b.pos, tetra.c.pos, tetra.d.pos);
                if (DentroTetraedro(w))
                {
                    verticesContenidos.Add(new VerticeInfo(i, tetra, w));
                    break;
                }
            }
        }
    }

    public static float[] CalcularPesos(Vector3 p, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3)
    {
        float[] w = new float[4];

        // Triángulo opuesto a p0 => (p1, p2, p3)
        Vector3 n0 = Vector3.Cross(p2 - p1, p3 - p1);
        w[0] = Vector3.Dot(n0, p - p1) / Vector3.Dot(n0, p0 - p1);

        // Triángulo opuesto a p1 => (p0, p2, p3)
        Vector3 n1 = Vector3.Cross(p3 - p2, p0 - p2);
        w[1] = Vector3.Dot(n1, p - p2) / Vector3.Dot(n1, p1 - p2);

        // Triángulo opuesto a p2 => (p0, p1, p3)
        Vector3 n2 = Vector3.Cross(p1 - p0, p3 - p0);
        w[2] = Vector3.Dot(n2, p - p0) / Vector3.Dot(n2, p2 - p0);

        // Triángulo opuesto a p3 => (p0, p2, p1)
        Vector3 n3 = Vector3.Cross(p2 - p0, p1 - p0);
        w[3] = Vector3.Dot(n3, p - p0) / Vector3.Dot(n3, p3 - p0);

        return w;
    }

    bool DentroTetraedro(float[] w)
    {
        bool dentro = true;
        for (int i = 0; i < w.Length; i++)
        {
            if (w[i] < -0.0005f)
            {
                dentro = false; break;
            }
        }
        return dentro;
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



}



