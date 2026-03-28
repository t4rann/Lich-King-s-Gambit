using UnityEngine;
using UnityEditor;
using System.Linq;

[CustomEditor(typeof(LevelLayoutSO))]
public class LevelLayoutSOEditor : Editor
{
    private TileType paintTileType = TileType.Floor;
    private PaintMode paintMode = PaintMode.Tile;

    private GameObject enemyPrefab;
    private int selectedEnemyIndex = 0;

    public override void OnInspectorGUI()
    {
        LevelLayoutSO level = (LevelLayoutSO)target;

        DrawDefaultInspector();
        EditorGUILayout.Space(10);

        if (level.layout == null || level.layout.Length != level.width * level.height)
        {
            EditorGUILayout.HelpBox("Layout will be regenerated", MessageType.Info);
            level.EnsureLayoutSize();
        }

        DrawPaintModeSelector();
        EditorGUILayout.Space(5);

        if (paintMode == PaintMode.Tile)
            DrawTileSelector();
        else
            DrawEnemySelector(level);

        DrawGrid(level);

        if (GUI.changed)
            EditorUtility.SetDirty(level);
    }

    // ================= MODES =================
    void DrawPaintModeSelector()
    {
        EditorGUILayout.LabelField("Paint Mode", EditorStyles.boldLabel);
        paintMode = (PaintMode)GUILayout.Toolbar(
            (int)paintMode,
            new[] { "Tiles", "Enemies" }
        );
    }

    // ================= TILE =================
    void DrawTileSelector()
    {
        EditorGUILayout.LabelField("Paint Tile", EditorStyles.boldLabel);
        paintTileType = (TileType)GUILayout.Toolbar(
            (int)paintTileType,
            new[] { "Floor", "Empty", "Wall" }
        );
    }

    // ================= ENEMY =================
    void DrawEnemySelector(LevelLayoutSO level)
    {
        EditorGUILayout.LabelField("Enemy Palette", EditorStyles.boldLabel);

        if (level.enemyPrefabs == null || level.enemyPrefabs.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Добавь enemyPrefabs в LevelLayoutSO",
                MessageType.Warning
            );
            return;
        }

        string[] names = level.enemyPrefabs
            .Select(p => p != null ? p.name : "NULL")
            .ToArray();

        selectedEnemyIndex = GUILayout.Toolbar(
            selectedEnemyIndex,
            names
        );
    }

    // ================= GRID =================
    void DrawGrid(LevelLayoutSO level)
    {
        EditorGUILayout.LabelField("Board Layout", EditorStyles.boldLabel);

        for (int y = level.height - 1; y >= 0; y--)
        {
            EditorGUILayout.BeginHorizontal();

            for (int x = 0; x < level.width; x++)
            {
                int index = y * level.width + x;

                EnemySpawnData enemy = level.enemies?
                    .FirstOrDefault(e => e.x == x && e.y == y);

                // Цвет клетки
                GUI.backgroundColor = enemy != null
                    ? new Color(1f, 0.6f, 0.6f)
                    : GetColor(level.layout[index]);

                // Текст кнопки
                string label = enemy != null
                    ? enemy.prefab != null
                        ? enemy.prefab.name.Substring(0, 1)
                        : "E"
                    : GetLabel(level.layout[index]);

                // Tooltip
                GUIContent content = new GUIContent(
                    label,
                    enemy != null && enemy.prefab != null
                        ? enemy.prefab.name
                        : $"({x},{y})"
                );

                if (GUILayout.Button(
                    content,
                    GUILayout.Width(30),
                    GUILayout.Height(30)
                ))
                {
                    HandleCellClick(level, x, y, index);
                }
            }

            EditorGUILayout.EndHorizontal();
        }

        GUI.backgroundColor = Color.white;
    }

    // ================= CLICK =================
    void HandleCellClick(LevelLayoutSO level, int x, int y, int index)
    {
        if (paintMode == PaintMode.Tile)
        {
            level.layout[index] = paintTileType;
            return;
        }

        // ===== ENEMY MODE =====
        EnemySpawnData existing = level.enemies
            .FirstOrDefault(e => e.x == x && e.y == y);

        // Удалить врага
        if (existing != null)
        {
            level.enemies.Remove(existing);
            return;
        }

        // Добавить врага
        if (level.enemyPrefabs == null || level.enemyPrefabs.Count == 0)
            return;

        GameObject prefab = level.enemyPrefabs[selectedEnemyIndex];
        if (prefab == null) return;

        level.enemies.Add(new EnemySpawnData
        {
            prefab = prefab,
            x = x,
            y = y
        });
    }

    // ================= UI =================
    Color GetColor(TileType type)
    {
        return type switch
        {
            TileType.Floor => new Color(0.7f, 1f, 0.7f),
            TileType.Empty => Color.gray,
            TileType.Wall  => new Color(0.6f, 0.6f, 0.6f),
            _ => Color.white
        };
    }

    string GetLabel(TileType type)
    {
        return type switch
        {
            TileType.Floor => "F",
            TileType.Empty => " ",
            TileType.Wall  => "W",
            _ => "?"
        };
    }
}

public enum PaintMode
{
    Tile,
    Enemy
}
