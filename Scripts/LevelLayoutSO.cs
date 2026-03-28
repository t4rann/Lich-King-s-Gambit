using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Game/Level Layout")]
public class LevelLayoutSO : ScriptableObject
{
    public int width = 7;
    public int height = 7;
    public TileType[] layout;

    [Header("Enemies")]
    public List<EnemySpawnData> enemies = new();
    
    [Header("Enemy Palette")]
    public List<GameObject> enemyPrefabs = new();

    public void EnsureLayoutSize()
    {
        int size = width * height;
        if (layout == null || layout.Length != size)
        {
            layout = new TileType[size];
            for (int i = 0; i < size; i++)
                layout[i] = TileType.Floor;
        }
    }
}
