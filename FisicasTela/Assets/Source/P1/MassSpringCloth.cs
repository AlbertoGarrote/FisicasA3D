using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using System.Net.NetworkInformation;
using System;

/// <summary>
/// Basic physics manager capable of simulating a given ISimulable
/// implementation using diverse integration methods: explicit,
/// implicit, Verlet and semi-implicit.
/// </summary>
public class MassSpringCloth : MonoBehaviour
{
    /// <summary>
    /// Default constructor. Zero all. 
    /// </summary>
    public MassSpringCloth()
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
    public Vector3 Gravity;
    public Integration IntegrationMethod;
    public float massNodes;

    [SerializeField] public List<Node> nodes;
    [SerializeField] public List<Spring> springsTraccion;
    [SerializeField] public List<Spring> springsFlexion;

    public Mesh mesh;
    public Vector3[] vertices;
    public float stiffnessSpringTraccion;
    public float stiffnessSpringFlexion;

    public List<GameObject> fixers;

    public float dampingMuelle = 0.1f;
    public float dampingNodo = 0.1f;

    public Vector3 viento = new Vector3(1, 0, 0);
    public float friccionViento = 0.5f;

    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
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

        // Select integration method
        switch (this.IntegrationMethod)
        {
            case Integration.Explicit: this.stepExplicit(); break;
            case Integration.Symplectic: this.stepSymplectic(); break;
            default:
                throw new System.Exception("[ERROR] Should never happen!");
        }

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
            node.ComputeForces(Gravity, dampingNodo);
        }

        // aplicar fuerzas de los muelles
        foreach (Spring springT in springsTraccion)
        {
            springT.ComputeForces(dampingMuelle);
        }
        foreach (Spring springF in springsFlexion)
        {
            springF.ComputeForces(dampingMuelle);
        }

        // aplica las fuerzas del viento
        AplicarFuerzaViento(viento, friccionViento);

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
        foreach (Spring springF in springsFlexion)
        {
            springF.UpdateLength();
        }

        ActualizarMesh();
    }

    void ActualizarMesh()
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            vertices[i] = transform.InverseTransformPoint(nodes[i].pos);
        }

        mesh.vertices = vertices;
        mesh.RecalculateNormals();
    }

    void InicializarNodos()
    {
        vertices = mesh.vertices;
        nodes = new List<Node>();

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldPos = transform.TransformPoint(vertices[i]);
            nodes.Add(new Node(worldPos, massNodes));
        }
    }

    void InicializarMuelles()
    {
        springsTraccion = new List<Spring>();
        springsFlexion = new List<Spring>();
        int[] triangles = mesh.triangles;
        List<Arista> aristas = new List<Arista>();

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int i0 = triangles[i];
            int i1 = triangles[i + 1];
            int i2 = triangles[i + 2];

            aristas.Add(new Arista(i0, i1, i2));
            aristas.Add(new Arista(i1, i2, i0));
            aristas.Add(new Arista(i2, i0, i1));
        }

        // ordenar las aristas para ver cuales se repiten
        OrdenarAristas(aristas);

        int index = 0;
        while (index < aristas.Count)
        {
            Arista aristaActual = aristas[index];
            int count = 1;

            // ver cuantas veces se repite la arista
            while (index + count < aristas.Count && aristaActual.mismaArista(aristas[index + count]))
            {
                count++;
            }

            AgregarMuelleTraccion(aristaActual.a, aristaActual.b);

            // si se repite la arista se crea un muelle de flexion
            if (count == 2)
            {
                Arista e1 = aristas[index];
                Arista e2 = aristas[index + 1];

                AgregarMuelleFlexion(e1.c, e2.c);
            }
            index += count;
        }
    }

    // MUELLES DE TRACCION
    void AgregarMuelleTraccion(int i, int j)
    {
        springsTraccion.Add(new Spring(nodes[i], nodes[j], stiffnessSpringTraccion));
    }

    // MUELLES DE FLEXION
    void AgregarMuelleFlexion(int i, int j)
    {
        springsFlexion.Add(new Spring(nodes[i], nodes[j], stiffnessSpringFlexion));
    }


    void OrdenarAristas(List<Arista> aristas)
    {
        // funcion lambda para ordenar aristas
        aristas.Sort((e1, e2) =>
        {
            int comparador = e1.a.CompareTo(e2.a);
            if (comparador != 0)
                return comparador;

            return e1.b.CompareTo(e2.b);
        });
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

    // VIENTO

    public Vector3 MediaVelocidadTri(int v1, int v2, int v3)
    {
        Vector3 velocidadTriangulo = (nodes[v1].vel + nodes[v2].vel + nodes[v3].vel) / 3.0f;
        return velocidadTriangulo;
    }

    public Vector3 NormalTri(int v1, int v2, int v3)
    {
        Vector3 artisa1 = nodes[v2].pos - nodes[v1].pos;
        Vector3 arista2 = nodes[v3].pos - nodes[v1].pos;
        return Vector3.Cross(artisa1, arista2).normalized;
    }

    public Vector3 FuerzaViento(int v1, int v2, int v3, Vector3 viento, float friccion)
    {
        Vector3 normal = NormalTri(v1, v2, v3);
        Vector3 velocidadTriangulo = MediaVelocidadTri(v1, v2, v3);

        Vector3 velocidadRelativa = viento - velocidadTriangulo;
        float componenteNormalVelocidad = Vector3.Dot(velocidadRelativa, normal);

        // área del triangulo
        Vector3 edge1 = nodes[v2].pos - nodes[v1].pos;
        Vector3 edge2 = nodes[v3].pos - nodes[v1].pos;
        float areaTriangulo = 0.5f * Vector3.Cross(edge1, edge2).magnitude;

        // formula viento
        Vector3 fuerzaViento = -friccion * areaTriangulo * componenteNormalVelocidad * normal;

        return fuerzaViento;
    }

    public void AplicarFuerzaViento(Vector3 viento, float friccion)
    {
        int[] triangles = mesh.triangles;

        for (int i = 0; i < triangles.Length; i += 3)
        {
            int v1 = triangles[i];
            int v2 = triangles[i + 1];
            int v3 = triangles[i + 2];

            Vector3 fuerzaViento = FuerzaViento(v1, v2, v3, viento, friccion);

            nodes[v1].force += fuerzaViento / 3f;
            nodes[v2].force += fuerzaViento / 3f;
            nodes[v3].force += fuerzaViento / 3f;
        }
    }
}

