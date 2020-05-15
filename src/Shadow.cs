using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shadow : MonoBehaviour {

    public Material ShadowMat;
    public Transform LightSource;
    public bool UseMouse;
    public float ObjectHeight;

    ushort[] spriteTris;
    Vector2[] spriteVerts;
    Vector2 normal;
    Vector2 lightDirection;
    List<Edge> boundaries;

    // SHADOW OBJECT
    MeshFilter shadowFilter;
    MeshRenderer shadowRenderer;
    Mesh shadowMesh;
    int[] shadowTris;
    Vector3[] shadowVerts;

    // DEBUG
    Vector2 midpoint;
    Vector2[] worldVerts;
    float normalLength = 0.05f;

    private void Awake() {
        spriteTris = this.GetComponent<SpriteRenderer>().sprite.triangles;
        spriteVerts = this.GetComponent<SpriteRenderer>().sprite.vertices;

        GameObject shadowGO = new GameObject("Shadow");
        shadowGO.transform.parent = transform;
        shadowGO.transform.localPosition = Vector3.zero;
        shadowGO.transform.localRotation = Quaternion.identity;
        shadowGO.transform.localScale = Vector3.one;
        shadowFilter = shadowGO.AddComponent<MeshFilter>();
        shadowRenderer = shadowGO.AddComponent<MeshRenderer>();
        shadowRenderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
        shadowRenderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;
        shadowRenderer.sortingOrder = -500;
        shadowRenderer.material = ShadowMat;
        shadowMesh = shadowFilter.mesh;

        worldVerts = new Vector2[spriteVerts.Length];
    }

    private void Update() {
        for (int i = 0; i < spriteVerts.Length; i++) {
            worldVerts[i] = transform.TransformPoint(spriteVerts[i]);
        }

        DrawShadow();
    }

    private void DrawShadow() {
        boundaries = GetBoundaries2D(spriteTris);
        shadowTris = new int[6 * boundaries.Count];
        shadowVerts = new Vector3[6 * boundaries.Count];

        for (int i = 0; i < boundaries.Count; i++) {
            normal = GetNormal2D(spriteVerts[boundaries[i].v2] - spriteVerts[boundaries[i].v1]);
            lightDirection = (transform.position - (UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : LightSource.position)).normalized * ObjectHeight;
            lightDirection = transform.InverseTransformDirection(lightDirection);

            if (Vector2.Dot(normal, lightDirection) > 0) {
                shadowVerts[(i * 6)] = spriteVerts[boundaries[i].v1];
                shadowVerts[(i * 6) + 1] = spriteVerts[boundaries[i].v1] + lightDirection;
                shadowVerts[(i * 6) + 2] = spriteVerts[boundaries[i].v2];

                shadowVerts[(i * 6) + 3] = spriteVerts[boundaries[i].v2];
                shadowVerts[(i * 6) + 4] = spriteVerts[boundaries[i].v1] + lightDirection;
                shadowVerts[(i * 6) + 5] = spriteVerts[boundaries[i].v2] + lightDirection;

                shadowTris[(i * 6)] = (i * 6);
                shadowTris[(i * 6) + 1] = (i * 6) + 1;
                shadowTris[(i * 6) + 2] = (i * 6) + 2;
                shadowTris[(i * 6) + 3] = (i * 6) + 3;
                shadowTris[(i * 6) + 4] = (i * 6) + 4;
                shadowTris[(i * 6) + 5] = (i * 6) + 5;

                // draw shadow projection lines
                Debug.DrawLine(worldVerts[boundaries[i].v1], worldVerts[boundaries[i].v1] + (Vector2)transform.TransformDirection(lightDirection), Color.white);
                Debug.DrawLine(worldVerts[boundaries[i].v2], worldVerts[boundaries[i].v2] + (Vector2)transform.TransformDirection(lightDirection), Color.white);

                // draw affected edges in bold
                Utility.DrawLine(worldVerts[boundaries[i].v1], worldVerts[boundaries[i].v2], 5, Color.blue);

                // draw light to affected vertices
                //Debug.DrawLine((UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : LightSource.position), worldVerts[boundaries[i].v1], Color.yellow);
                //Debug.DrawLine((UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : LightSource.position), worldVerts[boundaries[i].v2], Color.yellow);
            }
            else {
                // draw all other edges
                Debug.DrawLine(worldVerts[boundaries[i].v1], worldVerts[boundaries[i].v2], Color.grey);
            }

            // draw light direction vector
            Debug.DrawLine((UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : LightSource.position), transform.position, Color.yellow);

            // draw normals
            midpoint = (worldVerts[boundaries[i].v1] + worldVerts[boundaries[i].v2]) / 2;
            Debug.DrawLine(midpoint, midpoint + (Vector2)transform.TransformDirection(normal) * normalLength, Color.red);
        }

        shadowMesh.Clear();
        shadowMesh.vertices = shadowVerts;
        shadowMesh.triangles = shadowTris;
        shadowMesh.RecalculateNormals();
    }

    private Vector2 GetNormal2D(Vector2 vec, bool clockwise = true) {
        Vector2 v;
        v.x = vec.y;
        v.y = vec.x;
        v.x *= clockwise ? -1 : 1;
        v.y *= clockwise ? 1 : -1;
        return v.normalized;
    }

    private struct Edge {
        public int v1;
        public int v2;
        public Edge(int aV1, int aV2) {
            v1 = aV1;
            v2 = aV2;
        }
    }

    private List<Edge> GetBoundaries2D(ushort[] triangles) {
        List<Edge> edges = new List<Edge>();

        for (int i = 0; i < triangles.Length; i += 3) {
            edges.Add(new Edge(triangles[i], triangles[i + 1]));
            edges.Add(new Edge(triangles[i + 1], triangles[i + 2]));
            edges.Add(new Edge(triangles[i + 2], triangles[i]));
        }

        for (int i = edges.Count - 1; i > 0; i--) {
            for (int n = i - 1; n >= 0; n--) {
                if (edges[i].v1 == edges[n].v2 && 
                    edges[i].v2 == edges[n].v1) {
                    // shared edge so remove both
                    edges.RemoveAt(i);
                    edges.RemoveAt(n);
                    i--;
                    break;
                }
            }
        }

        return edges;
    }
}