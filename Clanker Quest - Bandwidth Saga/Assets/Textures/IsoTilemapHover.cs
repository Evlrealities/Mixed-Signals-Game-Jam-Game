using UnityEngine;
using UnityEngine.Tilemaps;
using TMPro;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class IsoTilemapHover_v2 : MonoBehaviour
{
    [Header("Scene References")]
    public Grid grid;                    // Grid (Isometric)
    public Tilemap baseTilemap;          // Your art/level map (optional for Exists check)
    public Tilemap overlayTilemap;       // Highlight cursor
    public TextMeshProUGUI coordLabel;   // UI label (Canvas)

    [Header("Highlight Style")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.35f); // yellow, semi-transparent
    public Color labelColor = Color.white;

    [Header("Options")]
    public bool onlyHighlightWhereTileExists = false; // ignore empty base tiles
    public int overlaySortingOrder = 5000;            // keep this on top

    [Header("Diagnostics")]
    public bool runSelfTestAtStart = true;
    public bool verbose = false;

    // --- internals ---
    private Tile _highlightTile;
    private Vector3Int? _lastCell = null;

    void Awake()
    {
        if (!grid) grid = FindObjectOfType<Grid>();
        if (!_highlightTile) _highlightTile = BuildHighlightTile(highlightColor);

        if (coordLabel) coordLabel.gameObject.SetActive(false);

        // Make sure the overlay renders above everything
        var r = overlayTilemap ? overlayTilemap.GetComponent<TilemapRenderer>() : null;
        if (r) r.sortingOrder = overlaySortingOrder;
    }

    void Start()
    {
        if (!overlayTilemap)
        {
            Debug.LogError("[IsoHover] Assign an Overlay Tilemap.", this);
            enabled = false;
            return;
        }

        if (runSelfTestAtStart)
        {
            overlayTilemap.ClearAllTiles();
            overlayTilemap.SetTile(Vector3Int.zero, _highlightTile);
            overlayTilemap.RefreshAllTiles();
            Debug.Log("[IsoTilemapHover] SelfTest: placed highlight at cell (0,0). Do you see a tinted diamond there?");
        }
    }

    void OnDisable() => ClearHover();

    void Update()
    {
        var cam = Camera.main;
        if (!cam || !grid || !overlayTilemap) return;

        // --- Input (works with new or old Input System) ---
        Vector2 mousePos;
#if ENABLE_INPUT_SYSTEM
        mousePos = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
#else
        mousePos = Input.mousePosition;
#endif

        // --- Mouse → world (hit the actual plane the base/overlay lives on) ---
        Vector3 world = MouseWorldOnTilemapPlane(cam, overlayTilemap, mousePos);

        // Convert to grid cell
        Vector3Int cell = grid.WorldToCell(world);
        if (verbose) Debug.Log($"[Hover] world:{world} cell:{cell}");

        // Optional: require a real tile under cursor
        if (onlyHighlightWhereTileExists && baseTilemap && !baseTilemap.HasTile(cell))
        {
            ClearHover();
            return;
        }

        // Nothing changed?
        if (_lastCell.HasValue && cell == _lastCell.Value) return;

        // Move overlay tile
        if (_lastCell.HasValue) overlayTilemap.SetTile(_lastCell.Value, null);
        overlayTilemap.SetTile(cell, _highlightTile);
        overlayTilemap.RefreshTile(cell);

        // Move + show UI label at cell center (screen space)
        if (coordLabel)
        {
            Vector3 centerWorld = grid.GetCellCenterWorld(cell);
            Vector3 screen = cam.WorldToScreenPoint(centerWorld);
            coordLabel.rectTransform.position = screen;
            coordLabel.color = labelColor;
            coordLabel.text = $"({cell.x}, {cell.y})";
            if (!coordLabel.gameObject.activeSelf) coordLabel.gameObject.SetActive(true);
        }

        _lastCell = cell;
    }

    // Helpers -------------------------------------------------------------

    // Robust mouse→world on the plane that the tilemaps live on (works for ortho/persp).
    private Vector3 MouseWorldOnTilemapPlane(Camera cam, Tilemap map, Vector2 mousePos)
    {
        Ray ray = cam.ScreenPointToRay(mousePos);

        // Plane with normal = tilemap's forward, through its transform position
        Plane plane = new Plane(map.transform.forward, map.transform.position);
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        // Fallback for orthographic / parallel case
        Vector3 s = new Vector3(mousePos.x, mousePos.y, Mathf.Abs(cam.transform.position.z - map.transform.position.z));
        return cam.ScreenToWorldPoint(s);
    }

    private void ClearHover()
    {
        if (overlayTilemap && _lastCell.HasValue)
        {
            overlayTilemap.SetTile(_lastCell.Value, null);
            overlayTilemap.RefreshTile(_lastCell.Value);
        }
        _lastCell = null;
        if (coordLabel && coordLabel.gameObject.activeSelf)
            coordLabel.gameObject.SetActive(false);
    }

    // Build a solid, tintable runtime Tile
    private Tile BuildHighlightTile(Color c)
    {
        // Tiny 2x2 white texture → clean scaling on any cell size
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();

        // Full-rect sprite; PPU doesn't matter for tilemaps
        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sprite;
        t.color = c;
        return t;
    }
}
