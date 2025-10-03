using UnityEngine;
using TMPro;

public class IsoGridLines : MonoBehaviour {
    public int width = 10;
    public int height = 10;
    public float cellSize = 1f;
    public Material lineMaterial;
    public float lineWidth = 0.02f;

    void Start() {
        DrawGrid();
    }

    void DrawGrid() {
        for (int x = 0; x < width; x++) {
            for (int y = 0; y < height; y++) {
                // corners of a tile in grid space
                Vector2 bottomLeft = GridToWorldPos(x, y, cellSize);
                Vector2 bottomRight = GridToWorldPos(x + 1, y, cellSize);
                Vector2 topRight = GridToWorldPos(x + 1, y + 1, cellSize);
                Vector2 topLeft = GridToWorldPos(x, y + 1, cellSize);

                DrawDiamond(new Vector3[] {
                    bottomLeft, bottomRight, topRight, topLeft
                });

                Vector2 centre = GridToWorldPos(x + 0.5f, y + 0.5f, cellSize);
                AddLabel(new Vector3(centre.x, centre.y, 0), $"({x},{y})");
            }
        }
    }

    void DrawDiamond(Vector3[] corners) {
        GameObject lineObj = new GameObject("Diamond");
        lineObj.transform.parent = transform;
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = lineMaterial;
        lr.widthMultiplier = lineWidth;
        lr.loop = true; // closes the shape
        lr.positionCount = 4;
        lr.useWorldSpace = true;
        lr.SetPositions(corners);
    }

    public Vector2 GridToWorldPos(float x, float y, float cellSize) {
        float worldX = (x - y) * (cellSize / 2f);
        float worldY = (x + y) * (cellSize / 4f);
        return new Vector2(worldX, worldY);
    }

    private void AddLabel(Vector3 pos, string text) {
        GameObject labelObj = new GameObject("GridLabel");
        labelObj.transform.position = pos;
        labelObj.transform.parent = transform;

        TextMeshPro tm = labelObj.AddComponent<TextMeshPro>();
        tm.text = text;
        tm.fontSize = 3;
        tm.alignment = TextAlignmentOptions.Center;
        tm.color = Color.white;
    }
}
