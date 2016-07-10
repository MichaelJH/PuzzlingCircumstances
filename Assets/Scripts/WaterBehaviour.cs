using UnityEngine;
using System.Collections;

public class WaterBehaviour : MonoBehaviour {
    private float[] xpositions;
    private float[] ypositions;
    private float[] velocities;
    private float[] accelerations;

    private LineRenderer bodyOfWater;

    private GameObject[] meshobjects;
    private Mesh[] meshes;

    private GameObject[] colliders;

    // constants
    const float springconstant = 0.01f;
    const float damping = 0.03f;
    const float spread = 0.05f;
    const float z = -.1f;

    // dimensions
    float baseheight;
    float left;
    float bottom;

    public Material mat;
    public GameObject watermesh;

    // set bounds
    public float Left;
    public float Width;
    public float Top;
    public float Bottom;


	// Use this for initialization
	void Start () {
        SpawnWater(Left, Width, Top, Bottom);
	}

    public void SpawnWater(float newLeft, float newWidth, float newTop, float newBottom) {
        int edgecount = Mathf.RoundToInt(newWidth) * 4;
        int nodecount = edgecount + 1;

        bodyOfWater = gameObject.AddComponent<LineRenderer>();
        bodyOfWater.material = mat;
        bodyOfWater.material.renderQueue = 1000;
        bodyOfWater.SetVertexCount(nodecount);
        bodyOfWater.SetWidth(0.1f, 0.1f);
        bodyOfWater.sortingLayerName = "Sconce";

        xpositions = new float[nodecount];
        ypositions = new float[nodecount];
        velocities = new float[nodecount];
        accelerations = new float[nodecount];

        meshobjects = new GameObject[edgecount];
        meshes = new Mesh[edgecount];
        colliders = new GameObject[edgecount];

        baseheight = newTop;
        bottom = newBottom;
        left = newLeft;

        for (int i = 0; i < nodecount; i++) {
            ypositions[i] = newTop;
            xpositions[i] = newLeft + newWidth * i / edgecount;
            accelerations[i] = 0;
            velocities[i] = 0;
            bodyOfWater.SetPosition(i, new Vector3(xpositions[i], ypositions[i], z));
        }

        for (int i = 0; i < edgecount; i++) {
            meshes[i] = new Mesh();

            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            vertices[2] = new Vector3(xpositions[i], bottom, z);
            vertices[3] = new Vector3(xpositions[i + 1], bottom, z);

            Vector2[] UVs = new Vector2[4];
            UVs[0] = new Vector2(0, 1);
            UVs[1] = new Vector2(1, 1);
            UVs[2] = new Vector2(0, 0);
            UVs[3] = new Vector2(1, 0);

            int[] tris = new int[6] { 0, 1, 3, 3, 2, 0 };

            meshes[i].vertices = vertices;
            meshes[i].uv = UVs;
            meshes[i].triangles = tris;

            meshobjects[i] = Instantiate(watermesh, Vector3.zero, Quaternion.identity) as GameObject;
            meshobjects[i].GetComponent<MeshFilter>().mesh = meshes[i];
            meshobjects[i].transform.parent = transform;

            colliders[i] = new GameObject();
            colliders[i].name = "Trigger";
            colliders[i].tag = "Water";
            colliders[i].AddComponent<BoxCollider2D>();
            colliders[i].transform.parent = transform;
            colliders[i].transform.position = new Vector3(newLeft + newWidth * (i + 0.5f) / edgecount, newTop - 0.5f, 0);
            colliders[i].transform.localScale = new Vector3(newWidth / edgecount, 1, 1);
            colliders[i].GetComponent<BoxCollider2D>().isTrigger = true;
            colliders[i].AddComponent<WaterDetector>();
        }
    }

    void UpdateMeshes() {
        for (int i = 0; i < meshes.Length; i++) {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            vertices[2] = new Vector3(xpositions[i], bottom, z);
            vertices[3] = new Vector3(xpositions[i + 1], bottom, z);

            meshes[i].vertices = vertices;
        }
    }

    void FixedUpdate() {
        for (int i = 0; i < xpositions.Length; i++) {
            float force = springconstant * (ypositions[i] - baseheight) + velocities[i] * damping;
            accelerations[i] = -force; // make this -force/mass if mass != 1
            ypositions[i] += velocities[i];
            velocities[i] += accelerations[i];
            bodyOfWater.SetPosition(i, new Vector3(xpositions[i], ypositions[i], z));
        }

        float[] leftDeltas = new float[xpositions.Length];
        float[] rightDeltas = new float[xpositions.Length];

        for (int j = 0; j < 8; j++) {
            for (int i = 0; i < xpositions.Length; i++) {
                if (i > 0) {
                    leftDeltas[i] = spread * (ypositions[i] - ypositions[i - 1]);
                    velocities[i - 1] += leftDeltas[i];
                }
                if (i < xpositions.Length - 1) {
                    rightDeltas[i] = spread * (ypositions[i] - ypositions[i + 1]);
                    velocities[i + 1] += rightDeltas[i];
                }
            }
        }

        for (int i = 0; i < xpositions.Length; i++) {
            if (i > 0) {
                ypositions[i - 1] += leftDeltas[i];
            }
            if (i < xpositions.Length - 1) {
                ypositions[i + 1] += rightDeltas[i];
            }
        }

        UpdateMeshes();
    }

    public void Splash(float xpos, float velocity) {
        if (xpos >= xpositions[0] && xpos <= xpositions[xpositions.Length - 1]) {
            xpos -= xpositions[0];

            int index = Mathf.RoundToInt((xpositions.Length - 1) * (xpos / (xpositions[xpositions.Length - 1] - xpositions[0])));

            velocities[index] = velocity;
        }
    }
}
