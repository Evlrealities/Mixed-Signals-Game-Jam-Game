using System;
using System.Collections.Generic;
using UnityEngine;

public class MoveExecutor : Executor
{
    [Header("Scene References")]
    [SerializeField] GridMover mover;   // drag your Player (with GridMover) here in the prefab

    [Header("Speed Presets")]
    public float normalSpeed = 1f;
    public float fastSpeed   = 1.8f;
    public float slowSpeed   = 0.6f;

    static readonly Dictionary<string, Vector3Int> DIR = new(StringComparer.OrdinalIgnoreCase)
    {
        { "e",  new Vector3Int( 1,  0, 0) }, { "east",  new Vector3Int( 1,  0, 0) },
        { "w",  new Vector3Int(-1,  0, 0) }, { "west",  new Vector3Int(-1,  0, 0) },
        { "n",  new Vector3Int( 0,  1, 0) }, { "north", new Vector3Int( 0,  1, 0) },
        { "s",  new Vector3Int( 0, -1, 0) }, { "south", new Vector3Int( 0, -1, 0) },
        { "ne", new Vector3Int( 1,  1, 0) },
        { "nw", new Vector3Int(-1,  1, 0) },
        { "se", new Vector3Int( 1, -1, 0) },
        { "sw", new Vector3Int(-1, -1, 0) },
    };

    protected override void Execute(string[] inputWords)
    {
        if (!mover) { Debug.LogWarning("MoveExecutor has no GridMover set."); return; }

        // very simple parse: supports speed modifiers anywhere, directions + optional counts
        float speed = normalSpeed;
        bool drunk = false;

        var queue = new List<Vector3Int>();

        for (int i = 0; i < inputWords.Length; i++)
        {
            string w = inputWords[i].ToLowerInvariant();

            // modifiers
            if (w is "quick" or "quickly" or "fast" or "speedy" or "rapid") { speed = fastSpeed; continue; }
            if (w is "slow" or "slowly" or "careful" or "cautious") { speed = slowSpeed; continue; }
            if (w is "drunk" or "wobbly" or "tipsy" or "chaotic") { drunk = true; continue; }
            if (w is "then" or "and" or "next" or "after") continue;

            // directions
            if (DIR.TryGetValue(w, out var delta))
            {
                int count = 1;
                // optional integer after direction
                if (i + 1 < inputWords.Length && int.TryParse(inputWords[i + 1], out int n))
                {
                    count = Mathf.Clamp(n, 1, 999);
                    i++;
                }
                for (int k = 0; k < count; k++) queue.Add(delta);
                continue;
            }

            // goto (x, y)
            if (w == "goto" || w == "to")
            {
                // accept forms: (x,y), x y, or x,y
                if (TryParseXY(inputWords, ref i, out var cell))
                {
                    mover.EnqueueGoto(cell, speed, drunk);
                }
                continue;
            }
        }

        if (queue.Count > 0)
            mover.EnqueueMoves(queue, speed, drunk);
    }

    bool TryParseXY(string[] words, ref int i, out Vector3Int cell)
    {
        cell = default;
        var buf = new List<string>();
        for (int t = i + 1; t < Mathf.Min(i + 5, words.Length); t++) buf.Add(words[t]);

        string joined = string.Join(" ", buf).Replace(",", " ").Replace("(", " ").Replace(")", " ");
        var parts = joined.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return false;

        if (int.TryParse(parts[0], out int x) && int.TryParse(parts[1], out int y))
        {
            cell = new Vector3Int(x, y, 0);
            i += 1 + 2; // consume tokens roughly
            return true;
        }
        return false;
    }
}
