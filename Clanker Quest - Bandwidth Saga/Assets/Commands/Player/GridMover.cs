using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridMover : MonoBehaviour
{
    [Header("Scene References")]
    public Grid grid;
    public Tilemap walkableTilemap;   // optional: if set + blockOnEmpty=true, only walk on tiles that exist

    [Header("Movement")]
    public float baseStepDuration = 0.18f;   // seconds per cell at normal speed
    public bool blockOnEmpty = false;        // if true, prevent moving into cells with no tile on walkableTilemap
    public bool snapToCellOnStart = true;

    // runtime
    public Vector3Int CurrentCell { get; private set; }
    bool isRunning;
    readonly Queue<IEnumerator> _queue = new Queue<IEnumerator>();

    void Awake()
    {
        if (!grid) grid = FindObjectOfType<Grid>();
    }

    void Start()
    {
        if (snapToCellOnStart) SnapToNearestCell();
    }

    public void SnapToNearestCell()
    {
        CurrentCell = grid.WorldToCell(transform.position);
        transform.position = grid.GetCellCenterWorld(CurrentCell);
    }

    public void EnqueueMoves(IEnumerable<Vector3Int> deltas, float speedMultiplier = 1f, bool drunk = false)
    {
        foreach (var d in deltas)
            _queue.Enqueue(MoveBy(d, speedMultiplier, drunk));

        if (!isRunning) StartCoroutine(RunQueue());
    }

    public void EnqueueGoto(Vector3Int targetCell, float speedMultiplier = 1f, bool drunk = false)
    {
        foreach (var step in ManhattanPath(CurrentCell, targetCell))
            _queue.Enqueue(MoveBy(step, speedMultiplier, drunk));

        if (!isRunning) StartCoroutine(RunQueue());
    }

    IEnumerator RunQueue()
    {
        isRunning = true;
        while (_queue.Count > 0)
            yield return StartCoroutine(_queue.Dequeue());
        isRunning = false;
    }

    IEnumerator MoveBy(Vector3Int delta, float speedMult, bool drunk)
    {
        var next = CurrentCell + delta;

        if (blockOnEmpty && walkableTilemap && !walkableTilemap.HasTile(next))
            yield break; // blocked

        var start = grid.GetCellCenterWorld(CurrentCell);
        var end = grid.GetCellCenterWorld(next);
        float dur = Mathf.Max(0.01f, baseStepDuration * (1f / Mathf.Max(0.01f, speedMult)));

        float t = 0f;
        Vector3 wobble = Vector3.zero;
        while (t < 1f)
        {
            t += Time.deltaTime / dur;
            float u = Mathf.Clamp01(t);
            var pos = Vector3.Lerp(start, end, u);

            if (drunk)
            {
                // small wobble in local right/up to make it silly but readable
                float n = Time.time * 7.73f;
                wobble.x = (Mathf.PerlinNoise(n, 0.123f) - 0.5f) * 0.08f;
                wobble.y = (Mathf.PerlinNoise(0.456f, n) - 0.5f) * 0.08f;
            }
            else wobble = Vector3.zero;

            transform.position = pos + wobble;
            yield return null;
        }

        transform.position = end;
        CurrentCell = next;
    }

    // Simple Manhattan route (no obstacles/pathfinding)
    IEnumerable<Vector3Int> ManhattanPath(Vector3Int a, Vector3Int b)
    {
        var d = b - a;
        int sx = d.x >= 0 ? 1 : -1;
        int sy = d.y >= 0 ? 1 : -1;
        for (int i = 0; i < Mathf.Abs(d.x); i++) yield return new Vector3Int(sx, 0, 0);
        for (int j = 0; j < Mathf.Abs(d.y); j++) yield return new Vector3Int(0, sy, 0);
    }
}
