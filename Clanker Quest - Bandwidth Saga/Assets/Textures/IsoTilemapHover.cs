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
    public Tilemap baseTilemap;          // Optional: for Exists check
    public Tilemap overlayTilemap;       // Highlight cursor
    public TextMeshProUGUI coordLabel;   // UI label that follows the hovered cell (Canvas)
    public TextMeshProUGUI hudLabel;     // NEW: fixed top-left HUD readout (Canvas)

    [Header("Highlight Style")]
    public Color highlightColor = new Color(1f, 1f, 0f, 0.35f); // yellow, semi-transparent
    public Color labelColor = Color.white;

    [Header("Options")]
    public bool onlyHighlightWhereTileExists = false; // ignore empty base tiles
    public int overlaySortingOrder = 5000;            // keep this on top

    [Header("HUD")]
    [Tooltip("Format for the top-left HUD readout")]
    public string hudFormat = "Cell: ({0}, {1})";

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

        // Ensure HUD is visible immediately (even before first mouse move)
        if (hudLabel) hudLabel.gameObject.SetActive(true);
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

        // --- Mouse → world (plane of overlay/base) ---
        Vector3 world = MouseWorldOnTilemapPlane(cam, overlayTilemap, mousePos);

        // Grid cell
        Vector3Int cell = grid.WorldToCell(world);
        if (verbose) Debug.Log($"[Hover] world:{world} cell:{cell}");

        // Top-left HUD (always-on)
        if (hudLabel)
        {
            hudLabel.text = $"Mouse Position: ({cell.x}, {cell.y})";
            if (!hudLabel.gameObject.activeSelf) hudLabel.gameObject.SetActive(true);
        }

        // Optional: require a real tile
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

        // Cursor-following label
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

    private Vector3 MouseWorldOnTilemapPlane(Camera cam, Tilemap map, Vector2 mousePos)
    {
        Ray ray = cam.ScreenPointToRay(mousePos);

        Plane plane = new Plane(map.transform.forward, map.transform.position);
        if (plane.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        // Fallback for orthographic / parallel
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
        // HUD stays on; no action
    }

    private Tile BuildHighlightTile(Color c)
    {
        Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        tex.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
        tex.Apply();

        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f), 2f);

        var t = ScriptableObject.CreateInstance<Tile>();
        t.sprite = sprite;
        t.color = c;
        return t;
    }
}
