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
    [SerializeField] public List<Arista> aristas;
    [SerializeField] public List<Face> faces;
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
    public TextAsset nodeFile, eleFile, faceFile;
    private Mesh meshAsset;

    public float densidadMasa;

    #endregion

    #region OtherVariables
    #endregion

    #region MonoBehaviour

    public void Start()
    {
        //mesh = GetComponent<MeshFilter>().mesh;
        InicializarNodos();
        InicializarFaces();
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

        for (int i = 0; i < subSteps; i++)
        {
            // Select integration method
            switch (this.IntegrationMethod)
            {
                case Integration.Explicit: this.stepExplicit(); break;
                case Integration.Symplectic: this.stepSymplectic(); break;
                default:
                    throw new System.Exception("[ERROR] Should never happen!");
            }
        }
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
            node.ComputeForces(Gravity, dampingNodo);
        }

        // aplicar fuerzas de los muelles
        foreach (Spring springT in springsTraccion)
        {
            springT.ComputeForces(dampingMuelle);
        }

        // Feurza de Penalty
        AplicarFuerzaPenalty(esferaPenalty, k_penalty);

        // Fuerza Viento
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

        //ActualizarMesh();
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

    void InicializarFaces()
    {
        string[] faceLines = faceFile.text.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        faces = new List<Face>();

        int numFaces = int.Parse(faceLines[0].Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries)[0]);

        for (int i = 1; i <= numFaces; i++)
        {
            string line = faceLines[i].Trim();
            if (string.IsNullOrWhiteSpace(line)) continue;

            string[] parts = line.Split(new char[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            int a = int.Parse(parts[1]) - 1; 
            int b = int.Parse(parts[2]) - 1;
            int c = int.Parse(parts[3]) - 1;

            Face face = new Face(a, b, c);
            faces.Add(face);
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

            float volumenTetra = CalcularVolumenTetra(nodes[idx0].pos, nodes[idx1].pos, nodes[idx2].pos, nodes[idx3].pos);

            Tetraedro t = new Tetraedro(volumenTetra, nodes[idx0], nodes[idx1], nodes[idx2], nodes[idx3]);
            tetraedros.Add(t);
        }

        CalcularMasaNodos(densidadMasa);
    }

    float CalcularVolumenTetra(Vector3 pos0, Vector3 pos1, Vector3 pos2, Vector3 pos3)
    {
        float volumen = Mathf.Abs(Vector3.Dot((pos1 - pos0), Vector3.Cross((pos2 - pos0), (pos3 - pos0)))) / 6f;
        return volumen;
    }

    void CalcularMasaNodos(float densidadMasa)
    {
        foreach (Tetraedro tetra in tetraedros)
        {
            Vector3 p0 = tetra.a.pos;
            Vector3 p1 = tetra.b.pos;
            Vector3 p2 = tetra.c.pos;
            Vector3 p3 = tetra.d.pos;

            float masaTetra = densidadMasa * tetra.volumenTetra;
            float masaPorNodo = masaTetra / 4f;

            tetra.a.mass += masaPorNodo;
            tetra.b.mass += masaPorNodo;
            tetra.c.mass += masaPorNodo;
            tetra.d.mass += masaPorNodo;
        }
    }

    void InicializarMuelles()
    {
        springsTraccion = new List<Spring>();
        aristas = new List<Arista>();

        foreach (Tetraedro tetra in tetraedros)
        {
            aristas.Add(new Arista(tetra.a, tetra.b, 0));
            aristas.Add(new Arista(tetra.a, tetra.c, 0));
            aristas.Add(new Arista(tetra.a, tetra.d, 0));
            aristas.Add(new Arista(tetra.b, tetra.c, 0));
            aristas.Add(new Arista(tetra.b, tetra.d, 0));
            aristas.Add(new Arista(tetra.c, tetra.d, 0));
        }

        // ordenar las aristas para ver cuales se repiten
        OrdenarAristas(aristas);

        foreach (Tetraedro tetra in tetraedros)
        {
            SumarVolumenArista(tetra.a, tetra.b, tetra.volumenTetra / 6);
            SumarVolumenArista(tetra.a, tetra.c, tetra.volumenTetra / 6);
            SumarVolumenArista(tetra.a, tetra.d, tetra.volumenTetra / 6);
            SumarVolumenArista(tetra.b, tetra.c, tetra.volumenTetra / 6);
            SumarVolumenArista(tetra.b, tetra.d, tetra.volumenTetra / 6);
            SumarVolumenArista(tetra.c, tetra.d, tetra.volumenTetra / 6);
        }

        EliminarAristas();

    }

    void SumarVolumenArista(Node nodoA, Node nodoB, float volumen)
    {
        Arista aux = new Arista(nodoA, nodoB, 0);

        foreach (Arista arista in aristas)
        {
            if (arista.mismaArista(aux))
            {
                arista.aristaVolumen += volumen;
            }
        }
    }

    void EliminarAristas()
    {
        int index = 0;
        while (index < aristas.Count)
        {
            Arista aristaActual = aristas[index];
            int count = 1;

            // ver si se repite la arista
            while (index + count < aristas.Count && aristaActual.mismaArista(aristas[index + count]))
            {
                count++;
            }

            AgregarMuelleTraccion(aristaActual.nodoA, aristaActual.nodoB, aristaActual.aristaVolumen);

            index += count;
        }
    }

    // MUELLES DE TRACCION
    void AgregarMuelleTraccion(Node i, Node j, float volumen)
    {
        springsTraccion.Add(new Spring(i, j, stiffnessSpringTraccion, volumen));
    }

    void OrdenarAristas(List<Arista> aristas)
    {
        // funcion lambda para ordenar aristas
        aristas.Sort((e1, e2) =>
        {
            int comparador = CompararVectores(e1.nodoA.pos, e2.nodoA.pos);
            if (comparador != 0)
                return comparador;

            return CompararVectores(e1.nodoB.pos, e2.nodoB.pos);
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
        Vector3 n0, n1, n2, n3;

        // triángulo opuesto a p0
        n0 = Vector3.Cross(p2 - p1, p3 - p1);
        w[0] = Vector3.Dot(n0, p - p1) / Vector3.Dot(n0, p0 - p1);

        // triángulo opuesto a p0
        n1 = Vector3.Cross(p3 - p2, p0 - p2);
        w[1] = Vector3.Dot(n1, p - p2) / Vector3.Dot(n1, p1 - p2);

        // triángulo opuesto a p0
        n2 = Vector3.Cross(p1 - p0, p3 - p0);
        w[2] = Vector3.Dot(n2, p - p0) / Vector3.Dot(n2, p2 - p0);

        // triángulo opuesto a p0
        n3 = Vector3.Cross(p2 - p0, p1 - p0);
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
    private int CompararVectores(Vector3 p1, Vector3 p2)
    {
        int compX = p1.x.CompareTo(p2.x);
        if (compX != 0) return compX;

        int compY = p1.y.CompareTo(p2.y);
        if (compY != 0) return compY;

        return p1.z.CompareTo(p2.z);
    }

    // CONTACTO CON ESFERA (PENALTY)
    void AplicarFuerzaPenalty(GameObject esfera, float k_penalty)
    {
        SphereCollider colliderEsfera = esfera.GetComponent<SphereCollider>();
        Vector3 centro = colliderEsfera.bounds.center;
        float radio = colliderEsfera.bounds.extents.x * 1.1f;

        foreach (Node nodo in nodes)
        {
            Vector3 direcionFuerza = nodo.pos - centro;
            float distancia = direcionFuerza.magnitude;

            if (distancia < radio) // está dentro de la esfera
            {
                float penetracion = radio - distancia;
                Vector3 normalSalidaFuerza = direcionFuerza.normalized;

                Vector3 fuerzaPenalty = k_penalty * penetracion * normalSalidaFuerza;
                nodo.force += fuerzaPenalty;
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

        // Area del triangulo
        Vector3 edge1 = nodes[v2].pos - nodes[v1].pos;
        Vector3 edge2 = nodes[v3].pos - nodes[v1].pos;
        float areaTriangulo = 0.5f * Vector3.Cross(edge1, edge2).magnitude;

        // formula viento
        Vector3 fuerzaViento = -friccion * areaTriangulo * componenteNormalVelocidad * normal;

        return fuerzaViento;
    }

    public void AplicarFuerzaViento(Vector3 viento, float friccion)
    {
        foreach(Face face in faces)
        {
            int v1 = face.a;
            int v2 = face.b;
            int v3 = face.c;

            Vector3 fuerzaViento = FuerzaViento(v1, v2, v3, viento, friccion);

            nodes[v1].force += fuerzaViento / 3f;
            nodes[v2].force += fuerzaViento / 3f;
            nodes[v3].force += fuerzaViento / 3f;
        }
    }

}



