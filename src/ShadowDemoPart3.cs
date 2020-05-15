using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ShadowDemoPart3 : UnityFSM {

    public Material ShadowMat;
    public Material InvisMat;
    public Transform LightSource;
    public bool UseMouse;
    public float ObjectHeight;
    public Transform Pivot;
    public TextMeshProUGUI TextBox;

    // STATES
    public SD_IdleState SD_Idle;
    public SD_SpinState SD_Spin;
    public SD_OutlineState SD_Outline;
    public SD_NormalsState SD_Normals;
    public SD_LightDirectionState SD_LightDirection;
    public SD_AffectedEdgesState SD_AffectedEdges;
    public SD_ProjectionVectorsState SD_ProjectionVectors;
    public SD_GeneratedMeshesState SD_GeneratedMeshes;
    public SD_PostState SD_Post;

    // CALCULATION
    public SpriteRenderer spriteRenderer { get; set; }
    public ushort[] spriteTris { get; set; }
    public Vector2[] spriteVerts { get; set; }
    public Vector2 normal { get; set; }
    public Vector2 lightDirection { get; set; }
    public List<Edge> boundaries { get; set; }

    // SHADOW OBJECT
    public GameObject shadowGO { get; set; }
    public MeshFilter shadowFilter { get; set; }
    public MeshRenderer shadowRenderer { get; set; }
    public Mesh shadowMesh { get; set; }
    public int[] shadowTris { get; set; }
    public Vector3[] shadowVerts { get; set; }

    // DEBUG
    public Vector2 midpoint { get; set; }
    public Vector2[] worldVerts { get; set; }
    public float normalLength { get { return 0.05f; } }
    public float DelayDisplayTextTime { get { return 0.1f; } }

    private bool flag = false;
    private float min;
    private float max;

    public override void CreateStates() {
        SD_Idle = new SD_IdleState(this);
        SD_Spin = new SD_SpinState(this);
        SD_Outline = new SD_OutlineState(this);
        SD_Normals = new SD_NormalsState(this);
        SD_LightDirection = new SD_LightDirectionState(this);
        SD_AffectedEdges = new SD_AffectedEdgesState(this);
        SD_ProjectionVectors = new SD_ProjectionVectorsState(this);
        SD_GeneratedMeshes = new SD_GeneratedMeshesState(this);
        SD_Post = new SD_PostState(this);
    }

    private void Awake() {
        spriteRenderer = this.GetComponent<SpriteRenderer>();
        spriteTris = spriteRenderer.sprite.triangles;
        spriteVerts = spriteRenderer.sprite.vertices;
        worldVerts = new Vector2[spriteVerts.Length];

        shadowGO = new GameObject("Shadow");
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

        min = Pivot.position.x;
        max = Pivot.position.x + 1;

        CreateStates();
        StartingState(SD_Idle);
    }

    private void Update() {
        for (int i = 0; i < spriteVerts.Length; i++) {
            worldVerts[i] = transform.TransformPoint(spriteVerts[i]);
        }

        if (Input.GetKey(KeyCode.Space)) {
            if (!flag) {
                flag = true;
                StartCoroutine(FullWalkthrough());
            }
        }

        CurrentState.OnUpdate();
    }

    private IEnumerator FullWalkthrough() {
        GoToState(SD_Spin);
        yield return new WaitForSeconds(5f);
        GoToState(SD_Outline);
        yield return new WaitForSeconds(7f);
        GoToState(SD_Normals);
        yield return new WaitForSeconds(7f);
        GoToState(SD_LightDirection);
        yield return new WaitForSeconds(7f);
        GoToState(SD_AffectedEdges);
        yield return new WaitForSeconds(8f);
        GoToState(SD_ProjectionVectors);
        yield return new WaitForSeconds(8f);
        GoToState(SD_GeneratedMeshes);
        yield return new WaitForSeconds(8f);
        GoToState(SD_Post);
        flag = false;
    }

    public Vector2 GetNormal2D(Vector2 vec, bool clockwise = true) {
        Vector2 v;
        v.x = vec.y;
        v.y = vec.x;
        v.x *= clockwise ? -1 : 1;
        v.y *= clockwise ? 1 : -1;
        return v.normalized;
    }

    public struct Edge {
        public int v1;
        public int v2;
        public Edge(int aV1, int aV2) {
            v1 = aV1;
            v2 = aV2;
        }
    }

    public List<Edge> GetBoundaries2D(ushort[] triangles) {
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
                    edges.RemoveAt(i);
                    edges.RemoveAt(n);
                    i--;
                    break;
                }
            }
        }

        return edges;
    }

    public void Spin() {
        transform.Rotate(Vector3.forward, -1.25f);
        Pivot.position = new Vector3(Mathf.PingPong(Time.time * 0.25f, max - min) + min, Pivot.position.y, Pivot.position.z);
    }
}

public class SD_IdleState : UnityFSMState<ShadowDemoPart3> {

    public SD_IdleState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnUpdate() {
        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);
        myFSM.shadowTris = new int[6 * myFSM.boundaries.Count];
        myFSM.shadowVerts = new Vector3[6 * myFSM.boundaries.Count];

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                myFSM.shadowVerts[(i * 6)] = myFSM.spriteVerts[myFSM.boundaries[i].v1];
                myFSM.shadowVerts[(i * 6) + 1] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 2] = myFSM.spriteVerts[myFSM.boundaries[i].v2];

                myFSM.shadowVerts[(i * 6) + 3] = myFSM.spriteVerts[myFSM.boundaries[i].v2];
                myFSM.shadowVerts[(i * 6) + 4] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 5] = myFSM.spriteVerts[myFSM.boundaries[i].v2] + myFSM.lightDirection;

                myFSM.shadowTris[(i * 6)] = (i * 6);
                myFSM.shadowTris[(i * 6) + 1] = (i * 6) + 1;
                myFSM.shadowTris[(i * 6) + 2] = (i * 6) + 2;
                myFSM.shadowTris[(i * 6) + 3] = (i * 6) + 3;
                myFSM.shadowTris[(i * 6) + 4] = (i * 6) + 4;
                myFSM.shadowTris[(i * 6) + 5] = (i * 6) + 5;
            }
        }

        myFSM.shadowMesh.Clear();
        myFSM.shadowMesh.vertices = myFSM.shadowVerts;
        myFSM.shadowMesh.triangles = myFSM.shadowTris;
        myFSM.shadowMesh.RecalculateNormals();
    }
}

public class SD_SpinState : UnityFSMState<ShadowDemoPart3> {

    public SD_SpinState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        myFSM.TextBox.text = "<color=white>How is this done?</color>";
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
    }

    public override void OnUpdate() {
        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);
        myFSM.shadowTris = new int[6 * myFSM.boundaries.Count];
        myFSM.shadowVerts = new Vector3[6 * myFSM.boundaries.Count];

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                myFSM.shadowVerts[(i * 6)] = myFSM.spriteVerts[myFSM.boundaries[i].v1];
                myFSM.shadowVerts[(i * 6) + 1] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 2] = myFSM.spriteVerts[myFSM.boundaries[i].v2];

                myFSM.shadowVerts[(i * 6) + 3] = myFSM.spriteVerts[myFSM.boundaries[i].v2];
                myFSM.shadowVerts[(i * 6) + 4] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 5] = myFSM.spriteVerts[myFSM.boundaries[i].v2] + myFSM.lightDirection;

                myFSM.shadowTris[(i * 6)] = (i * 6);
                myFSM.shadowTris[(i * 6) + 1] = (i * 6) + 1;
                myFSM.shadowTris[(i * 6) + 2] = (i * 6) + 2;
                myFSM.shadowTris[(i * 6) + 3] = (i * 6) + 3;
                myFSM.shadowTris[(i * 6) + 4] = (i * 6) + 4;
                myFSM.shadowTris[(i * 6) + 5] = (i * 6) + 5;
            }
        }

        myFSM.shadowMesh.Clear();
        myFSM.shadowMesh.vertices = myFSM.shadowVerts;
        myFSM.shadowMesh.triangles = myFSM.shadowTris;
        myFSM.shadowMesh.RecalculateNormals();
    }
}

public class SD_OutlineState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_OutlineState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        myFSM.spriteRenderer.enabled = false;
        myFSM.shadowRenderer.material = myFSM.InvisMat;
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=white>1. Boundary Edges</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            // draw all edges
            Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.white);
        }
    }
}

public class SD_NormalsState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_NormalsState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=red>2. Normals</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);

            // draw all edges
            Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.white);

            // draw normals
            myFSM.midpoint = (myFSM.worldVerts[myFSM.boundaries[i].v1] + myFSM.worldVerts[myFSM.boundaries[i].v2]) / 2;
            Debug.DrawLine(myFSM.midpoint, myFSM.midpoint + (Vector2)myFSM.transform.TransformDirection(myFSM.normal) * myFSM.normalLength, Color.red);
        }
    }
}

public class SD_LightDirectionState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_LightDirectionState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=yellow>3. Light Direction</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);

            // draw light direction vector
            Utility.DrawLine((myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position), myFSM.transform.position, 5, Color.yellow);

            // draw all edges
            Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.white);

            // draw normals
            myFSM.midpoint = (myFSM.worldVerts[myFSM.boundaries[i].v1] + myFSM.worldVerts[myFSM.boundaries[i].v2]) / 2;
            Debug.DrawLine(myFSM.midpoint, myFSM.midpoint + (Vector2)myFSM.transform.TransformDirection(myFSM.normal) * myFSM.normalLength, Color.red);
        }
    }
}

public class SD_AffectedEdgesState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_AffectedEdgesState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=#00ffff>4. Affected Edges</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                // draw affected edges in bold
                Utility.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], 5, Color.cyan);
            }
            else {
                // draw all other edges
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.gray);
            }

            // draw light direction vector
            Debug.DrawLine((myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position), myFSM.transform.position, Color.yellow);

            // draw normals
            myFSM.midpoint = (myFSM.worldVerts[myFSM.boundaries[i].v1] + myFSM.worldVerts[myFSM.boundaries[i].v2]) / 2;
            Debug.DrawLine(myFSM.midpoint, myFSM.midpoint + (Vector2)myFSM.transform.TransformDirection(myFSM.normal) * myFSM.normalLength, Color.red);
        }
    }
}

public class SD_ProjectionVectorsState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_ProjectionVectorsState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=white>5. Projection Vectors</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                //prelim calc
                myFSM.shadowVerts[(i * 6)] = myFSM.spriteVerts[myFSM.boundaries[i].v1];
                myFSM.shadowVerts[(i * 6) + 1] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 2] = myFSM.spriteVerts[myFSM.boundaries[i].v2];

                myFSM.shadowVerts[(i * 6) + 3] = myFSM.spriteVerts[myFSM.boundaries[i].v2];
                myFSM.shadowVerts[(i * 6) + 4] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 5] = myFSM.spriteVerts[myFSM.boundaries[i].v2] + myFSM.lightDirection;

                myFSM.shadowTris[(i * 6)] = (i * 6);
                myFSM.shadowTris[(i * 6) + 1] = (i * 6) + 1;
                myFSM.shadowTris[(i * 6) + 2] = (i * 6) + 2;
                myFSM.shadowTris[(i * 6) + 3] = (i * 6) + 3;
                myFSM.shadowTris[(i * 6) + 4] = (i * 6) + 4;
                myFSM.shadowTris[(i * 6) + 5] = (i * 6) + 5;

                // draw shadow projection lines
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v1] + (Vector2)myFSM.transform.TransformDirection(myFSM.lightDirection), Color.white);
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v2], myFSM.worldVerts[myFSM.boundaries[i].v2] + (Vector2)myFSM.transform.TransformDirection(myFSM.lightDirection), Color.white);

                // draw affected edges in bold
                Utility.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], 5, Color.cyan);
            }
            else {
                // draw all other edges
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.grey);
            }

            // draw light direction vector
            Debug.DrawLine((myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position), myFSM.transform.position, Color.yellow);

            // draw normals
            myFSM.midpoint = (myFSM.worldVerts[myFSM.boundaries[i].v1] + myFSM.worldVerts[myFSM.boundaries[i].v2]) / 2;
            Debug.DrawLine(myFSM.midpoint, myFSM.midpoint + (Vector2)myFSM.transform.TransformDirection(myFSM.normal) * myFSM.normalLength, Color.red);
        }

        // prelim, so no pop in next state
        myFSM.shadowMesh.Clear();
        myFSM.shadowMesh.vertices = myFSM.shadowVerts;
        myFSM.shadowMesh.triangles = myFSM.shadowTris;
        myFSM.shadowMesh.RecalculateNormals();
    }
}

public class SD_GeneratedMeshesState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_GeneratedMeshesState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        myFSM.shadowRenderer.material = myFSM.ShadowMat;
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= myFSM.DelayDisplayTextTime) {
            myFSM.TextBox.text = "<color=#101010>6. Generated Meshes</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);
        myFSM.shadowTris = new int[6 * myFSM.boundaries.Count];
        myFSM.shadowVerts = new Vector3[6 * myFSM.boundaries.Count];

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                myFSM.shadowVerts[(i * 6)] = myFSM.spriteVerts[myFSM.boundaries[i].v1];
                myFSM.shadowVerts[(i * 6) + 1] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 2] = myFSM.spriteVerts[myFSM.boundaries[i].v2];

                myFSM.shadowVerts[(i * 6) + 3] = myFSM.spriteVerts[myFSM.boundaries[i].v2];
                myFSM.shadowVerts[(i * 6) + 4] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 5] = myFSM.spriteVerts[myFSM.boundaries[i].v2] + myFSM.lightDirection;

                myFSM.shadowTris[(i * 6)] = (i * 6);
                myFSM.shadowTris[(i * 6) + 1] = (i * 6) + 1;
                myFSM.shadowTris[(i * 6) + 2] = (i * 6) + 2;
                myFSM.shadowTris[(i * 6) + 3] = (i * 6) + 3;
                myFSM.shadowTris[(i * 6) + 4] = (i * 6) + 4;
                myFSM.shadowTris[(i * 6) + 5] = (i * 6) + 5;

                // draw shadow projection lines
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v1] + (Vector2)myFSM.transform.TransformDirection(myFSM.lightDirection), Color.white);
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v2], myFSM.worldVerts[myFSM.boundaries[i].v2] + (Vector2)myFSM.transform.TransformDirection(myFSM.lightDirection), Color.white);

                // draw affected edges in bold
                Utility.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], 5, Color.cyan);
            }
            else {
                // draw all other edges
                Debug.DrawLine(myFSM.worldVerts[myFSM.boundaries[i].v1], myFSM.worldVerts[myFSM.boundaries[i].v2], Color.grey);
            }

            // draw light direction vector
            Debug.DrawLine((myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position), myFSM.transform.position, Color.yellow);

            // draw normals
            myFSM.midpoint = (myFSM.worldVerts[myFSM.boundaries[i].v1] + myFSM.worldVerts[myFSM.boundaries[i].v2]) / 2;
            Debug.DrawLine(myFSM.midpoint, myFSM.midpoint + (Vector2)myFSM.transform.TransformDirection(myFSM.normal) * myFSM.normalLength, Color.red);
        }

        myFSM.shadowMesh.Clear();
        myFSM.shadowMesh.vertices = myFSM.shadowVerts;
        myFSM.shadowMesh.triangles = myFSM.shadowTris;
        myFSM.shadowMesh.RecalculateNormals();
    }
}

public class SD_PostState : UnityFSMState<ShadowDemoPart3> {

    private float elapsedTime;
    private float startTime;

    public SD_PostState(ShadowDemoPart3 sd) : base(sd) { }

    public override void OnStateEntry() {
        myFSM.spriteRenderer.enabled = true;
        startTime = Time.time;
    }

    public override void OnStateExit() {
        myFSM.TextBox.text = "";
        elapsedTime = 0f;
    }

    public override void OnUpdate() {
        elapsedTime = Time.time - startTime;
        if (elapsedTime >= 2f) {
            myFSM.TextBox.text = "<color=white>2D Shadow Projection</color>";
        }

        myFSM.Spin();

        myFSM.boundaries = myFSM.GetBoundaries2D(myFSM.spriteTris);
        myFSM.shadowTris = new int[6 * myFSM.boundaries.Count];
        myFSM.shadowVerts = new Vector3[6 * myFSM.boundaries.Count];

        for (int i = 0; i < myFSM.boundaries.Count; i++) {
            myFSM.normal = myFSM.GetNormal2D(myFSM.spriteVerts[myFSM.boundaries[i].v2] - myFSM.spriteVerts[myFSM.boundaries[i].v1]);
            myFSM.lightDirection = (myFSM.transform.position - (myFSM.UseMouse ? Camera.main.ScreenToWorldPoint(Input.mousePosition) : myFSM.LightSource.position)).normalized * myFSM.ObjectHeight;
            myFSM.lightDirection = myFSM.transform.InverseTransformDirection(myFSM.lightDirection);

            if (Vector2.Dot(myFSM.normal, myFSM.lightDirection) > 0) {
                myFSM.shadowVerts[(i * 6)] = myFSM.spriteVerts[myFSM.boundaries[i].v1];
                myFSM.shadowVerts[(i * 6) + 1] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 2] = myFSM.spriteVerts[myFSM.boundaries[i].v2];

                myFSM.shadowVerts[(i * 6) + 3] = myFSM.spriteVerts[myFSM.boundaries[i].v2];
                myFSM.shadowVerts[(i * 6) + 4] = myFSM.spriteVerts[myFSM.boundaries[i].v1] + myFSM.lightDirection;
                myFSM.shadowVerts[(i * 6) + 5] = myFSM.spriteVerts[myFSM.boundaries[i].v2] + myFSM.lightDirection;

                myFSM.shadowTris[(i * 6)] = (i * 6);
                myFSM.shadowTris[(i * 6) + 1] = (i * 6) + 1;
                myFSM.shadowTris[(i * 6) + 2] = (i * 6) + 2;
                myFSM.shadowTris[(i * 6) + 3] = (i * 6) + 3;
                myFSM.shadowTris[(i * 6) + 4] = (i * 6) + 4;
                myFSM.shadowTris[(i * 6) + 5] = (i * 6) + 5;
            }
        }

        myFSM.shadowMesh.Clear();
        myFSM.shadowMesh.vertices = myFSM.shadowVerts;
        myFSM.shadowMesh.triangles = myFSM.shadowTris;
        myFSM.shadowMesh.RecalculateNormals();
    }
}