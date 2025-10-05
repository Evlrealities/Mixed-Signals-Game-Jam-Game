using UnityEngine;
using TMPro;

public class IsoGridLines : MonoBehaviour {
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;
    public Material lineMaterial;          // Unlit/Color or Sprites/Default recommended
    public float lineWidth = 0.02f;

    // hover/selection
    public Color gridColor = new Color(1f, 1f, 1f, 0.25f);
    public Color hoverColor = Color.yellow;
    public Color selectedColor = new Color(0.1f, 1f, 0.1f, 0.9f);
    public float hoverLineWidth = 0.035f;
    public float selectedLineWidth = 0.04f;

    // label
    public float labelFontSize = 1.2f;

    // If your grid plane is offset from this transform.position along its local forward, set this.
    public float planeOffset = 0f;

    // internals
    private LineRenderer[,] lines;
    private LineRenderer hoverOutline;
    private LineRenderer selectedOutline;
    private TextMeshPro hoverLabel;
    private Vector2Int lastHover = new Vector2Int(-999, -999);

    void Start() {
        DrawGrid();
        CreateHoverOutline();
        CreateSelectedOutline();
        CreateHoverLabel();
    }

    void Update() {
        UpdateHover();
        if (Input.GetMouseButtonDown(0)) {
            CommitSelection();
        }
    }

    // ---------- DRAWING ----------
    void DrawGrid() {
        lines = new LineRenderer[width, height];

        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                Vector3[] cornersW = TileCornersWorld(x, y);

                LineRenderer lr = NewLineRenderer("Tile");
                lr.positionCount = 4;
                lr.loop = true;
                lr.SetPositions(cornersW);
                SetLine(lr, gridColor, lineWidth);

                lines[x, y] = lr;
            }
        }
    }

    LineRenderer NewLineRenderer(string name) {
        GameObject go = new GameObject(name);
        go.transform.parent = transform;
        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.material = new Material(lineMaterial);
        lr.useWorldSpace = true;
        lr.numCornerVertices = 2;
        lr.numCapVertices = 2;
        lr.sortingOrder = 5000; // on top
        return lr;
    }

    void CreateHoverOutline() {
        hoverOutline = NewLineRenderer("HoverOutline");
        hoverOutline.positionCount = 4;
        hoverOutline.loop = true;
        SetLine(hoverOutline, hoverColor, hoverLineWidth);
        hoverOutline.enabled = false;
    }

    void CreateSelectedOutline() {
        selectedOutline = NewLineRenderer("SelectedOutline");
        selectedOutline.positionCount = 4;
        selectedOutline.loop = true;
        SetLine(selectedOutline, selectedColor, selectedLineWidth);
        selectedOutline.enabled = false;
    }

    void CreateHoverLabel() {
        GameObject labelObj = new GameObject("HoverLabel");
        labelObj.transform.parent = transform;

        hoverLabel = labelObj.AddComponent<TextMeshPro>();
        hoverLabel.fontSize = labelFontSize;
        hoverLabel.alignment = TextAlignmentOptions.Center;
        hoverLabel.color = Color.white;
        hoverLabel.text = "";
        hoverLabel.gameObject.SetActive(false);
    }

    // ---------- HOVER / SELECT ----------
    void UpdateHover() {
        if (Camera.main == null) return;

        Vector3 worldHit = MouseWorldOnGridPlane();
        // convert hit to grid-local space for math
        Vector3 localHit = transform.InverseTransformPoint(worldHit);

        // local isometric math (assumes grid lies in this transform's local XY plane)
        Vector2 g = WorldLocalToGrid(localHit);
        int gx = Mathf.FloorToInt(g.x);
        int gy = Mathf.FloorToInt(g.y);

        bool inside = gx >= 0 && gy >= 0 && gx < width && gy < height;

        if (inside) {
            if (gx != lastHover.x || gy != lastHover.y) {
                Vector3[] cornersW = TileCornersWorld(gx, gy);
                hoverOutline.SetPositions(cornersW);
                if (!hoverOutline.enabled) hoverOutline.enabled = true;

                Vector3 centreW = GridToWorldCentre(gx, gy);
                hoverLabel.transform.position = centreW;
                hoverLabel.text = $"({gx},{gy})";
                if (!hoverLabel.gameObject.activeSelf) hoverLabel.gameObject.SetActive(true);

                lastHover = new Vector2Int(gx, gy);
            }
        } else {
            if (hoverOutline.enabled) hoverOutline.enabled = false;
            if (hoverLabel.gameObject.activeSelf) hoverLabel.gameObject.SetActive(false);
            lastHover = new Vector2Int(-999, -999);
        }

        // Debug:
        // Debug.Log($"hitW:{worldHit} local:{localHit} grid:{g} -> ({gx},{gy})");
    }

    void CommitSelection() {
        if (lastHover.x < 0) {
            selectedOutline.enabled = false;
            return;
        }
        Vector3[] cornersW = TileCornersWorld(lastHover.x, lastHover.y);
        selectedOutline.SetPositions(cornersW);
        if (!selectedOutline.enabled) selectedOutline.enabled = true;
    }

    // ---------- GEOMETRY HELPERS ----------
    // Mouse → intersection with THIS grid's plane (works with any rotation/position)
    Vector3 MouseWorldOnGridPlane() {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // plane through transform.position + planeOffset along local forward,
        // with normal = this transform's forward (so the plane follows rotation)
        Vector3 planePoint = transform.position + transform.forward * planeOffset;
        Plane plane = new Plane(transform.forward, planePoint);

        float enter;
        if (plane.Raycast(ray, out enter)) {
            return ray.GetPoint(enter);
        }
        return planePoint; // fallback
    }

    // Grid (x,y) corner positions in WORLD space
    Vector3[] TileCornersWorld(int gx, int gy) {
        Vector3 bl = GridToWorldPoint(gx,     gy);
        Vector3 br = GridToWorldPoint(gx + 1, gy);
        Vector3 tr = GridToWorldPoint(gx + 1, gy + 1);
        Vector3 tl = GridToWorldPoint(gx,     gy + 1);
        return new Vector3[] { bl, br, tr, tl };
    }

    Vector3 GridToWorldCentre(int gx, int gy) {
        return GridToWorldPoint(gx + 0.5f, gy + 0.5f);
    }

    // Map grid coords -> LOCAL XY (isometric diamond) then to WORLD
    Vector3 GridToWorldPoint(float x, float y) {
        // local isometric placement on this transform's XY plane
        float lx = (x - y) * (cellSize / 2f);
        float ly = (x + y) * (cellSize / 4f);
        Vector3 local = new Vector3(lx, ly, 0f); // local Z = 0 plane
        // push to the plane offset along local forward, then to world
        Vector3 localWithOffset = local + Vector3.forward * planeOffset;
        return transform.TransformPoint(localWithOffset);
    }

    // Inverse: LOCAL (x,y) -> grid coords (fractional)
    Vector2 WorldLocalToGrid(Vector3 local) {
        // remove plane offset along local forward
        float lx = local.x;
        float ly = local.y;
        float a = (2f * lx) / cellSize; // x - y
        float b = (4f * ly) / cellSize; // x + y
        float gx = 0.5f * (a + b);
        float gy = 0.5f * (b - a);
        return new Vector2(gx, gy);
    }

    void SetLine(LineRenderer lr, Color c, float w) {
        if (lr == null) return;

        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(c, 0f),
                new GradientColorKey(c, 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(c.a, 0f),
                new GradientAlphaKey(c.a, 1f)
            }
        );
        lr.colorGradient = grad;
        lr.widthMultiplier = w;

        if (lr.material.HasProperty("_Color")) {
            lr.material.color = c;
        }
    }
}
