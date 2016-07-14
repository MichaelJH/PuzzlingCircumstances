using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WaterBehaviour : MonoBehaviour {
    private float[] xpositions;
    private float[] ypositions;
    private float[] velocities;
    private float[] accelerations;

    private LineRenderer bodyOfWater;

    private GameObject[] meshobjects;
    private Mesh[] meshes;
    private float[] bottoms;

    private GameObject[] colliders;

    private LayerMask _collisionMask;

    // constants
    const float springconstant = 0.025f;
    const float damping = 0.05f;
    const float spread = 0.05f;
    const int nodesPerUnit = 4; // number of nodes per unit length

    const float z = -.1f;

    // dimensions
    float baseheight;

    public Material mat;
    public GameObject watermesh;

    // render layer for the surface
    private string sortLayer = "Sconce";

    // for mapping underwater
    float tileSize = 1.5f; // size of block tiles on the map

	// Use this for initialization
	void Start () {
        _collisionMask = LayerMask.GetMask("Platform", "PortalPlatform");
        float[] dimensions = GenerateDimensions();
        SpawnWater(dimensions[0], dimensions[1], dimensions[2]);
	}

    private float[] GenerateDimensions() {
        float top = transform.position.y; // y coordinate for the surface of the water

        RaycastHit2D leftHit = Physics2D.Raycast(transform.position, Vector2.left, Mathf.Infinity, _collisionMask);
        float left = leftHit.point.x; // x coordinate for the lhs of the water

        RaycastHit2D rightHit = Physics2D.Raycast(transform.position, Vector2.right, Mathf.Infinity, _collisionMask);
        float width = rightHit.point.x - left; // width of the surface of the water

        float[] dims = { left, width, top,};
        return dims;
    }

    public void SpawnWater(float left, float width, float top) {
        int edgecount = Mathf.RoundToInt(width) * nodesPerUnit;
        int nodecount = edgecount + 1;

        // create the water surface
        bodyOfWater = gameObject.AddComponent<LineRenderer>();
        bodyOfWater.material = mat;
        bodyOfWater.material.renderQueue = 1000;
        bodyOfWater.SetVertexCount(nodecount);
        bodyOfWater.SetWidth(0.2f, 0.2f);
        bodyOfWater.sortingLayerName = sortLayer;

        // instantiate arrays 
        xpositions = new float[nodecount];
        ypositions = new float[nodecount];
        bottoms = new float[edgecount];
        velocities = new float[nodecount];
        accelerations = new float[nodecount];

        meshobjects = new GameObject[edgecount];
        meshes = new Mesh[edgecount];
        colliders = new GameObject[edgecount];
        
        // for reference in UpdateMeshes
        baseheight = top;
        
        // set the initial x and y positions, accelerations and velocities for each node
        for (int i = 0; i < nodecount; i++) {
            ypositions[i] = top;
            xpositions[i] = left + width * i / edgecount;
            accelerations[i] = 0;
            velocities[i] = 0;
            bodyOfWater.SetPosition(i, new Vector3(xpositions[i], ypositions[i], z));
        }

        // build the meshes and colliders for each edge
        for (int i = 0; i < edgecount; i++) {
            meshes[i] = new Mesh();

            // detect the depth for this edge
            RaycastHit2D downHit = Physics2D.Raycast(new Vector2(xpositions[i + 1]-0.1f, top), Vector2.down, Mathf.Infinity, _collisionMask);
            bottoms[i] = downHit.point.y;

            // set the vertices of the polygon
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            vertices[2] = new Vector3(xpositions[i], bottoms[i], z);
            vertices[3] = new Vector3(xpositions[i + 1], bottoms[i], z);

            // create UVs
            Vector2[] UVs = new Vector2[4];
            UVs[0] = new Vector2(0, 1);
            UVs[1] = new Vector2(1, 1);
            UVs[2] = new Vector2(0, 0);
            UVs[3] = new Vector2(1, 0);

            // define triangles from the vertices
            int[] tris = new int[6] { 0, 1, 3, 3, 2, 0 };

            meshes[i].vertices = vertices;
            meshes[i].uv = UVs;
            meshes[i].triangles = tris;

            // instantiate the mesh
            meshobjects[i] = Instantiate(watermesh, Vector3.zero, Quaternion.identity) as GameObject;
            meshobjects[i].GetComponent<MeshFilter>().mesh = meshes[i];
            meshobjects[i].transform.parent = transform;

            // create the collider
            colliders[i] = new GameObject();
            colliders[i].name = "Trigger";
            colliders[i].tag = "Water";
            colliders[i].layer = LayerMask.NameToLayer("Water"); // for detection in the playerCollisions script
            colliders[i].AddComponent<BoxCollider2D>();
            colliders[i].transform.parent = transform;
            colliders[i].transform.position = new Vector3(left + width * (i + 0.5f) / edgecount, top - 0.5f, 0);
            colliders[i].transform.localScale = new Vector3(width / edgecount, 0.05f, 1);
            colliders[i].GetComponent<BoxCollider2D>().isTrigger = true;
            colliders[i].AddComponent<WaterDetector>();
        }

        MapUnderwater(left, left + width, top);
    }

    private void MapUnderwater(float leftEdge, float rightEdge, float surface) {
        // find x and y coordinates of the unit to start from
        float xOrigin = leftEdge + (tileSize / 2);
        float ceiling = Physics2D.Raycast(new Vector2(xOrigin, surface), Vector2.up, Mathf.Infinity, _collisionMask).point.y;
        float yOrigin = ceiling - ((Mathf.Ceil((ceiling - surface) / tileSize) * tileSize) + (tileSize / 2));

        Vector2 origin = new Vector2(xOrigin, yOrigin);
        Vector2 originalTile = origin;

        Dictionary<float, List<float>> initialMap = new Dictionary<float, List<float>>();
        Dictionary<float, List<float>> exploredMap = new Dictionary<float, List<float>>();

        // create the initial map underneath the surface
        while (origin.x < rightEdge) {
            origin.y = yOrigin;

            List<float> yMap = new List<float>();
            float bottom = Physics2D.Raycast(origin, Vector2.down, Mathf.Infinity, _collisionMask).point.y;

            while (origin.y > bottom) {
                float yRound = Mathf.Round(origin.y * 100);
                //Debug.Log("y = " + yRound);
                yMap.Add(yRound);
                origin.y -= tileSize;
            }

            float xRound = Mathf.Round(origin.x * 100);
            initialMap.Add(xRound, yMap); // add the y collumn map to at the x coordinate
            origin.x += tileSize; // increment x coordinate
        }

        origin.x -= tileSize; // return to rightmost tile
        origin.y = yOrigin; // return to uppermost tile

        SkimDown(initialMap, exploredMap, origin, originalTile);

        float halfTile = tileSize / 2;
        foreach(float x in exploredMap.Keys) {
            float xR = x / 100;
            List<float> yCoords = exploredMap[x];
            foreach(float y in yCoords) {
                float yR = y / 100;
                CreateWaterMesh(xR - halfTile, xR + halfTile, yR + halfTile, yR - halfTile);
            }
        }
    }

    private Dictionary<float, List<float>> SkimDown(Dictionary<float, List<float>> initialMap, Dictionary<float, List<float>> exploredMap, Vector2 origin, Vector2 originalTile) {
        // begin moving downward, looking for openings on the right
        float edgeBottom = Physics2D.Raycast(origin, Vector2.down, Mathf.Infinity, _collisionMask).point.y;
        List<float> temp = null;

        while (origin.y > edgeBottom) {
            RaycastHit2D hitCheck = Physics2D.Raycast(origin, Vector2.right, tileSize, _collisionMask);

            if (hitCheck) {
                Debug.DrawRay(origin, Vector2.right, Color.red, 50f);
            } else {
                float xPlusTile = Mathf.Round((origin.x + tileSize) * 100);
                float yRound = Mathf.Round(origin.y * 100);

                if (!exploredMap.TryGetValue(xPlusTile, out temp)
                    || !exploredMap[xPlusTile].Contains(yRound)) {
                    Debug.DrawRay(origin, Vector2.right, Color.white, 50f);
                    exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x + tileSize, origin.y));
                } else {
                    Debug.DrawRay(origin, Vector2.right, Color.red, 50f);
                }
            }
            origin.y -= tileSize;
        }

        origin.y += tileSize;
        return SkimLeft(initialMap, exploredMap, origin, originalTile);
    }

    private Dictionary<float, List<float>> SkimLeft(Dictionary<float, List<float>> initialMap, Dictionary<float, List<float>> exploredMap, Vector2 origin, Vector2 originalTile) {
        // begin moving to the left, looking for openings on the bottom or unmapped tiles
        float edgeLeft = Physics2D.Raycast(origin, Vector2.left, Mathf.Infinity, _collisionMask).point.x;
        List<float> temp = null;

        float yRound = Mathf.Round(origin.y * 100);

        while (origin.x > edgeLeft) {
            RaycastHit2D hitDownCheck = Physics2D.Raycast(origin, Vector2.down, tileSize, _collisionMask);
            RaycastHit2D hitLeftCheck = Physics2D.Raycast(origin, Vector2.left, tileSize, _collisionMask);
            
            float xRound = Mathf.Round(origin.x * 100);

            if (hitDownCheck) {
                Debug.DrawRay(origin, Vector2.down, Color.red, 50f);
                float xMinusTile = xRound - (tileSize * 100);
                if (!hitLeftCheck
                    && (!initialMap.TryGetValue(xMinusTile, out temp)
                    || !initialMap[xMinusTile].Contains(yRound))
                    && (!exploredMap.TryGetValue(xMinusTile, out temp)
                    || !exploredMap[xMinusTile].Contains(yRound))) {

                    Debug.DrawRay(origin, Vector2.left, Color.white, 50f);
                    exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x - tileSize, origin.y));
                    return SkimUp(initialMap, exploredMap, new Vector2(origin.x, origin.y + tileSize), originalTile);

                } else {
                    Debug.DrawRay(origin, Vector2.left, Color.red, 50f);
                }
            } else {
                Debug.DrawRay(origin, Vector2.down, Color.white, 50f);
                return SkimDown(initialMap, exploredMap, new Vector2(origin.x, origin.y - tileSize), originalTile);
            }

            origin.x -= tileSize;
        }

        origin.x += tileSize;
        return SkimUp(initialMap, exploredMap, origin, originalTile);
    }

    private Dictionary<float, List<float>> SkimUp(Dictionary<float, List<float>> initialMap, Dictionary<float, List<float>> exploredMap, Vector2 origin, Vector2 originalTile) {
        List<float> temp = null;

        float xMinusTile = Mathf.Round((origin.x - tileSize) * 100);
        while (origin.y <= originalTile.y) {
            RaycastHit2D hitLeftCheck = Physics2D.Raycast(origin, Vector2.left, tileSize, _collisionMask);

            float yRound = Mathf.Round(origin.y * 100);

            if (hitLeftCheck) {
                Debug.DrawRay(origin, Vector2.left, Color.red, 50f);
            } else {
                if (initialMap.TryGetValue(xMinusTile, out temp)
                    && initialMap[xMinusTile].Contains(yRound)) {
                    Debug.DrawRay(origin, Vector2.left, Color.white, 50f);
                    return SkimLeft(initialMap, exploredMap, new Vector2(origin.x - tileSize, origin.y), originalTile);
                } else if (!exploredMap.TryGetValue(xMinusTile, out temp)
                    || !exploredMap[xMinusTile].Contains(yRound)) {
                    Debug.DrawRay(origin, Vector2.left, Color.white, 50f);
                    exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x - tileSize, origin.y));
                } else {
                    Debug.DrawRay(origin, Vector2.left, Color.red, 50f);
                }
            }
            origin.y += tileSize;
        }

        return exploredMap;
    }

    private Dictionary<float, List<float>> MapArea(Dictionary<float, List<float>> initialMap, Dictionary<float, List<float>> exploredMap, Vector2 origin) {
        float xRound = Mathf.Round(origin.x * 100);
        float yRound = Mathf.Round(origin.y * 100);
        List<float> temp = null;

        if (exploredMap.TryGetValue(xRound, out temp))
            exploredMap[xRound].Add(yRound);
        else {
            List<float> newlist = new List<float>();
            newlist.Add(yRound);
            exploredMap.Add(xRound, newlist);
        }

        float xPlusTile = xRound + (tileSize * 100);
        if (!Physics2D.Raycast(origin, Vector2.right, tileSize, _collisionMask)
            && (!exploredMap.TryGetValue(xPlusTile, out temp)
            || !exploredMap[xPlusTile].Contains(yRound))
            && (!initialMap.TryGetValue(xPlusTile, out temp)
            || !initialMap[xPlusTile].Contains(yRound))) {
            exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x + tileSize, origin.y));
            Debug.DrawRay(origin, Vector2.right, Color.white, 50f);
        } else {
            Debug.DrawRay(origin, Vector2.right, Color.red, 50f);
        }

        float yMinusTile = yRound - (tileSize * 100);
        if (!Physics2D.Raycast(origin, Vector2.down, tileSize, _collisionMask)
            && !exploredMap[xRound].Contains(yMinusTile)) {
            exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x, origin.y - tileSize));
            Debug.DrawRay(origin, Vector2.down, Color.white, 50f);
        } else {
            Debug.DrawRay(origin, Vector2.down, Color.red, 50f);
        }

        float xMinusTile = xRound - (tileSize * 100);
        if (!Physics2D.Raycast(origin, Vector2.left, tileSize, _collisionMask)
            && (!exploredMap.TryGetValue(xMinusTile, out temp)
            || !exploredMap[xMinusTile].Contains(yRound))
            && (!initialMap.TryGetValue(xMinusTile, out temp)
            || !initialMap[xMinusTile].Contains(yRound))) {
            exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x - tileSize, origin.y));
            Debug.DrawRay(origin, Vector2.left, Color.white, 50f);
        } else {
            Debug.DrawRay(origin, Vector2.left, Color.red, 50f);
        }

        // comment out this last move (upward moving) to avoid filling in upwards (if that is desirable)
        float yPlusTile = yRound + (tileSize * 100);
        if (!Physics2D.Raycast(origin, Vector2.up, tileSize, _collisionMask)
            && !exploredMap[xRound].Contains(yPlusTile)) {
            exploredMap = MapArea(initialMap, exploredMap, new Vector2(origin.x, origin.y + tileSize));
            Debug.DrawRay(origin, Vector2.up, Color.white, 50f);
        } else {
            Debug.DrawRay(origin, Vector2.up, Color.red, 50f);
        }

        return exploredMap;
    }

    private void CreateWaterMesh(float leftPoint, float rightPoint, float topPoint, float bottomPoint) {
        Vector3[] vertices = new Vector3[4];
        vertices[0] = new Vector3(leftPoint, topPoint, z);
        vertices[1] = new Vector3(rightPoint, topPoint, z);
        vertices[2] = new Vector3(leftPoint, bottomPoint, z);
        vertices[3] = new Vector3(rightPoint, bottomPoint, z);

        Vector2[] UVs = new Vector2[4];
        UVs[0] = new Vector2(0, 1);
        UVs[1] = new Vector2(1, 1);
        UVs[2] = new Vector2(0, 0);
        UVs[3] = new Vector2(1, 0);

        int[] tris = new int[6] { 0, 1, 3, 3, 2, 0 };

        Mesh underMesh = new Mesh();
        GameObject meshObj = new GameObject();

        underMesh.vertices = vertices;
        underMesh.uv = UVs;
        underMesh.triangles = tris;

        meshObj = Instantiate(watermesh, Vector3.zero, Quaternion.identity) as GameObject;
        meshObj.GetComponent<MeshFilter>().mesh = underMesh;
        meshObj.transform.parent = transform;
    }

    void UpdateMeshes() {
        for (int i = 0; i < meshes.Length; i++) {
            Vector3[] vertices = new Vector3[4];
            vertices[0] = new Vector3(xpositions[i], ypositions[i], z);
            vertices[1] = new Vector3(xpositions[i + 1], ypositions[i + 1], z);
            vertices[2] = new Vector3(xpositions[i], bottoms[i], z);
            vertices[3] = new Vector3(xpositions[i + 1], bottoms[i], z);

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

        for (int j = 0; j < 16; j++) {
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
