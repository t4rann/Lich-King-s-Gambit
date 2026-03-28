using UnityEngine;
using Pixelplacement;
using System.Collections.Generic;
using System.Collections;

public enum TileType
{
    Floor,   
    Empty,  
    Wall    
}

[System.Serializable]
public class EnemySpawnData
{
    public GameObject prefab;
    public int x;
    public int y;
}

public class BoardManager : MonoBehaviour
{
    [Header("Board Settings")]
    public int width = 7;
    public int height = 7;

    [Header("Tile Prefabs")]
    public GameObject lightTilePrefab; 
    public GameObject darkTilePrefab;  
    public GameObject wallTilePrefab;  

    public Transform boardParent;

    [Header("Board Layout")]
    public TileType[] boardLayout;

    [Header("Board Settings")]
    public float cellSize = 1.25f;

    [Header("Animation Settings")]
    public float boardAppearDuration = 0.35f;   
    public float boardDisappearDuration = 0.5f;
    public AnimationCurve boardAppearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public AnimationCurve boardDisappearCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public float piecesAppearDelay = 0.3f;
    public float piecesAppearStagger = 0.1f;

    [HideInInspector]
    public Tile[,] tiles;

    private float offsetX;
    private float offsetY;
    private bool isAnimating = false;

    public System.Action OnBoardAppearComplete;
    public System.Action OnBoardDisappearComplete;

    void Awake()
    {
        if (tiles != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (tiles[x, y] != null)
                        Destroy(tiles[x, y].gameObject);
                }
            }
            tiles = null;
        }
    }

void OnValidate()
{
    EnsureLayoutSize();
}

public void EnsureLayoutSize()
{
    int size = width * height;

    if (boardLayout == null || boardLayout.Length != size)
    {
        boardLayout = new TileType[size];
        for (int i = 0; i < size; i++)
            boardLayout[i] = TileType.Floor;
    }
}

    public void InitBoard()
    {
        offsetX = (width - 1) / 2f;
        offsetY = (height - 1) / 2f;
    }

    #region Board Generation
    public Vector3 GetWorldPosition(int x, int y)
    {
        return new Vector3(
            (x - offsetX) * cellSize,
            (y - offsetY) * cellSize,
            0
        );
    }

    public void GenerateBoardAnimated()
    {
        if (isAnimating) return;
        StartCoroutine(AnimateBoardAppearance());
    }

    private IEnumerator AnimateBoardAppearance()
    {
        isAnimating = true;

        if (tiles != null)
            ClearBoard();

        tiles = new Tile[width, height];
        Tile[,] tempTiles = new Tile[width, height];

        List<Transform> wallTransforms = new List<Transform>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                TileType type = GetTileType(x, y);
                Vector3 pos = GetWorldPosition(x, y);

                bool isWall = (type == TileType.Wall);
                GameObject prefabToSpawn = GetTilePrefab(x, y, type);

                if (prefabToSpawn == null)
                {
                    tempTiles[x, y] = null;
                    continue;
                }

                GameObject tileObj = Instantiate(prefabToSpawn, pos, Quaternion.identity, boardParent);
                Tile tile = tileObj.GetComponent<Tile>();

                tile.Init(x, y, isWall);
                tile.SetInputLocked(true);
                tile.transform.localScale = Vector3.zero;

                tempTiles[x, y] = tile;
                tiles[x, y] = tile;

                if (isWall)
                    wallTransforms.Add(tile.transform);
            }
        }

        for (int sum = 0; sum <= width + height; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height) continue;

                Tile tile = tempTiles[x, y];
                if (tile == null || tile.isWall) continue;

                if ((x + y) % 2 == 0)
                {
                    Tween.LocalScale(
                        tile.transform,
                        Vector3.one,
                        boardAppearDuration,
                        0f,
                        boardAppearCurve
                    );
                }
            }

            yield return new WaitForSeconds(0.015f);
        }

        for (int sum = 0; sum <= width + height; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height) continue;

                Tile tile = tempTiles[x, y];
                if (tile == null || tile.isWall) continue;

                if ((x + y) % 2 == 1)
                {
                    Tween.LocalScale(
                        tile.transform,
                        Vector3.one,
                        boardAppearDuration,
                        0f,
                        boardAppearCurve
                    );
                }
            }

            yield return new WaitForSeconds(0.015f);
        }

        foreach (var t in wallTransforms)
        {
            Tween.LocalScale(
                t,
                Vector3.one,
                boardAppearDuration,
                0f,
                boardAppearCurve
            );
        }

        yield return new WaitForSeconds(boardAppearDuration);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null)
                    tiles[x, y].SetInputLocked(false);
            }
        }

        isAnimating = false;
        OnBoardAppearComplete?.Invoke();
    }

    private GameObject GetTilePrefab(int x, int y, TileType type)
    {
        switch(type)
        {
            case TileType.Wall: return wallTilePrefab;
            case TileType.Floor: return ((x + y) % 2 == 0) ? lightTilePrefab : darkTilePrefab;
            case TileType.Empty: return null;
            default: return null;
        }
    }

    public void ClearBoardAnimated()
    {
        if (isAnimating || tiles == null) return;
        StartCoroutine(AnimateBoardDisappearance());
    }

    private IEnumerator AnimateBoardDisappearance()
    {
        if (tiles == null) yield break;

        isAnimating = true;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null)
                    tiles[x, y].SetInputLocked(true);
            }
        }

        yield return StartCoroutine(AnimateAllPiecesDisappear());

        Tile[,] tempTiles = tiles;
        List<Transform> wallTransforms = new List<Transform>();

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tempTiles[x, y];
                if (tile == null) continue;

                if (tile.isWall)
                    wallTransforms.Add(tile.transform);
            }
        }

        for (int sum = 0; sum <= width + height; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height) continue;

                Tile tile = tempTiles[x, y];
                if (tile == null || tile.isWall) continue;

                if ((x + y) % 2 == 0)
                {
                    Tween.LocalScale(
                        tile.transform,
                        Vector3.zero,
                        boardDisappearDuration,
                        0f,
                        boardDisappearCurve
                    );
                }
            }

            yield return new WaitForSeconds(0.015f);
        }

        for (int sum = 0; sum <= width + height; sum++)
        {
            for (int x = 0; x < width; x++)
            {
                int y = sum - x;
                if (y < 0 || y >= height) continue;

                Tile tile = tempTiles[x, y];
                if (tile == null || tile.isWall) continue;

                if ((x + y) % 2 == 1)
                {
                    Tween.LocalScale(
                        tile.transform,
                        Vector3.zero,
                        boardDisappearDuration,
                        0f,
                        boardDisappearCurve
                    );
                }
            }

            yield return new WaitForSeconds(0.015f);
        }

        foreach (var t in wallTransforms)
        {
            Tween.LocalScale(
                t,
                Vector3.zero,
                boardDisappearDuration,
                0f,
                boardDisappearCurve
            );
        }

        yield return new WaitForSeconds(boardDisappearDuration);

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tempTiles[x, y];
                if (tile == null) continue;

                Destroy(tile.gameObject);
                tiles[x, y] = null;
            }
        }

        tiles = null;
        isAnimating = false;

        OnBoardDisappearComplete?.Invoke();
    }

    private IEnumerator AnimateAllPiecesDisappear()
    {
        if (tiles == null) yield break;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tiles[x, y];
                if (tile == null) continue;

                if (tile.currentPiece != null)
                {
                    StartCoroutine(
                        AnimatePieceDisappear(
                            tile.currentPiece,
                            (x + y) * 0.03f
                        )
                    );

                    tile.currentPiece = null;
                }
            }
        }

        yield return new WaitForSeconds(0.015f);
    }

    #endregion

    #region Spawning
    public void SpawnPieceAnimated(GameObject prefab, bool isWhite, int x, int y)
    {
        if (tiles == null || x >= width || y >= height || tiles[x, y] == null)
            return;

        if (tiles[x, y].currentPiece != null)
        {
            StartCoroutine(
                AnimatePieceDisappear(tiles[x, y].currentPiece)
            );
            tiles[x, y].currentPiece = null;
        }

        GameObject obj = Instantiate(prefab);
        Piece piece = obj.GetComponent<Piece>();
        piece.isWhite = isWhite;
        piece.prefabReference = prefab;
        piece.SetPosition(x, y);

        piece.transform.localScale = Vector3.zero;

        Tween.LocalScale(piece.transform, Vector3.one, 0.5f,
            piecesAppearDelay + ((x + y) * 0.05f), boardAppearCurve);

        tiles[x, y].currentPiece = piece;
    }

    public void SpawnAllPiecesAnimated(List<LevelManager.PlayerPieceData> playerPieces,
                                       System.Action spawnEnemiesCallback)
    {
        StartCoroutine(AnimatePiecesSpawn(playerPieces, spawnEnemiesCallback));
    }

    private IEnumerator AnimatePiecesSpawn(List<LevelManager.PlayerPieceData> playerPieces,
                                           System.Action spawnEnemiesCallback)
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (tiles[x, y] != null && tiles[x, y].currentPiece != null && tiles[x, y].currentPiece.isWhite)
                {
                    StartCoroutine(
                        AnimatePieceDisappear(tiles[x, y].currentPiece)
                    );
                    tiles[x, y].currentPiece = null;
                }
            }
        }

        foreach (var data in playerPieces)
        {
            if (data.x >= width || data.y >= height) continue;

            GameObject obj = Instantiate(data.prefabReference);
            Piece p = obj.GetComponent<Piece>();
            p.isWhite = true;
            p.prefabReference = data.prefabReference;
            
            p.mergeLevel = data.mergeLevel;
            p.ApplyMergeStats(data.mergeLevel);
            
            p.SetPosition(data.x, data.y);

            p.transform.localScale = Vector3.zero;

            Tween.LocalScale(p.transform, Vector3.one, 0.5f,
                ((data.x + data.y) * 0.1f), boardAppearCurve);

            tiles[data.x, data.y].currentPiece = p;
            
            yield return null;
        }

        yield return new WaitForSeconds(piecesAppearDelay);

        spawnEnemiesCallback?.Invoke();
    }
    #endregion


    #region Board Management
    public void ClearBoard()
    {
        if (tiles == null) return;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tiles[x, y];
                if (tile == null) continue;

                if (tile.currentPiece != null)
                {
                    Destroy(tile.currentPiece.gameObject);
                    tile.currentPiece = null;
                }

                Destroy(tile.gameObject);
                tiles[x, y] = null;
            }
        }

        tiles = null;
    }

    private IEnumerator AnimatePieceDisappear(Piece piece, float delay = 0f)
    {
        if (piece == null) yield break;

        Transform t = piece.transform;

        if (delay > 0)
            yield return new WaitForSeconds(delay);

        if (t == null) yield break; 

        Tween.LocalScale(
            t,
            Vector3.zero,
            0.35f,
            0f,
            boardDisappearCurve
        );

        Tween.LocalRotation(
            t,
            Quaternion.Euler(0, 0, Random.Range(-90f, 90f)),
            0.35f,
            0f
        );

        yield return new WaitForSeconds(0.4f);

        if (t != null)
            Destroy(t.gameObject);
    }

    public Tile GetTile(int x, int y)
    {
        if (x >= 0 && x < width && y >= 0 && y < height)
            return tiles[x, y];
        return null;
    }
    
    public TileType GetTileType(int x, int y)
    {
        int index = y * width + x;
        if (index < 0 || index >= boardLayout.Length)
            return TileType.Empty;

        return boardLayout[index];
    }

    public bool CanPlacePiece(int x, int y, bool isWhite)
    {
        if (x < 0 || x >= width || y < 0 || y >= height)
            return false;

        Tile tile = tiles[x, y];
        if (tile == null || tile.isWall)
            return false;

        if (isWhite && y > 1)
            return false;

        return tile.currentPiece == null;
    }
    #endregion
}