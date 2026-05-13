using UnityEngine;
using System.Collections.Generic;
using Pixelplacement; 

public class MergeManager : MonoBehaviour
{
    public static MergeManager Instance;

    private GameObject pendingRewardPrefab;
    private List<Piece> mergeCandidates = new List<Piece>();
    
    [Header("Visual Settings")]
    public Color mergeHighlightColor = Color.yellow; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

public void CheckMergePossibility(GameObject rewardPrefab, Piece selectedPiece)
{
    ClearMergeHighlights();

    if (rewardPrefab == null || selectedPiece == null)
    {
        pendingRewardPrefab = null;
        return;
    }

    pendingRewardPrefab = rewardPrefab;

    FindMergeCandidates(rewardPrefab);

    if (mergeCandidates.Count > 0 && TutorialManager.Instance != null)
    {
        TutorialManager.Instance.MergeTutorial();
    }

    HighlightMergeCandidates();
}

    private void FindMergeCandidates(GameObject rewardPrefab)
    {
        mergeCandidates.Clear();

        if (GameManager.Instance == null || GameManager.Instance.board == null)
            return;

        Tile[,] tiles = GameManager.Instance.board.tiles;
        int width = GameManager.Instance.board.width;
        int height = GameManager.Instance.board.height;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tile tile = tiles[x, y];
                if (tile?.currentPiece != null && tile.currentPiece.isWhite)
                {
                    Piece piece = tile.currentPiece;

                    if (IsPurchasedPlayerPiece(piece) && 
                        piece.prefabReference == rewardPrefab && 
                        piece.mergeLevel < 3)
                    {
                        mergeCandidates.Add(piece);
                    }
                }
            }
        }
    }

    private bool IsPurchasedPlayerPiece(Piece piece)
    {
        if (piece == null) return false;
        
        if (LevelManager.Instance != null && 
            LevelManager.Instance.RewardOptionsContains(piece))
        {
            return false;
        }
        
        return true;
    }

    private void HighlightMergeCandidates()
    {
        foreach (Piece candidate in mergeCandidates)
        {
            if (IsPurchasedPlayerPiece(candidate))
            {
                SetMergeHighlight(candidate, true);
                
                Tile tile = GetTileAt(candidate.x, candidate.y);
                if (tile != null)
                {
                    tile.HighlightMerge();
                }
            }
        }
    }

    private Tile GetTileAt(int x, int y)
    {
        if (GameManager.Instance == null || GameManager.Instance.board == null)
            return null;
            
        if (x >= 0 && x < GameManager.Instance.board.width &&
            y >= 0 && y < GameManager.Instance.board.height)
        {
            return GameManager.Instance.board.tiles[x, y];
        }
        
        return null;
    }

    public void ClearMergeHighlights()
    {
        foreach (Piece candidate in mergeCandidates)
        {
            SetMergeHighlight(candidate, false);
            
            Tile tile = GetTileAt(candidate.x, candidate.y);
            if (tile != null)
            {
                tile.ResetHighlight();
            }
        }
        
        mergeCandidates.Clear();
        pendingRewardPrefab = null;
    }

    private void SetMergeHighlight(Piece piece, bool enabled)
    {
        if (piece == null || piece.outline == null || piece.outlineSR == null) 
            return;

        if (enabled)
        {
            piece.outlineSR.color = mergeHighlightColor;
            piece.outline.SetActive(true);
            piece.isMergingCandidate = true;
        }
        else
        {
            piece.outline.SetActive(false);
            piece.isMergingCandidate = false; 
        }
    }

    public bool TryMerge(Piece rewardPiece, Tile targetTile)
    {
        if (rewardPiece == null || targetTile == null || targetTile.currentPiece == null)
            return false;

        Piece targetPiece = targetTile.currentPiece;

        if (!IsPurchasedPlayerPiece(targetPiece) || !CanMerge(rewardPiece, targetPiece))
            return false;

        SetMergeHighlight(targetPiece, false);
        
        Tile tile = GetTileAt(targetPiece.x, targetPiece.y);
        if (tile != null)
        {
            tile.ResetHighlight();
        }
        
        PerformMerge(rewardPiece, targetPiece, targetTile);
        return true;
    }

    private bool CanMerge(Piece piece1, Piece piece2)
    {
        if (piece1 == null || piece2 == null) return false;
        if (piece1.prefabReference != piece2.prefabReference) return false;
        if (piece1.mergeLevel >= 3 || piece2.mergeLevel >= 3) return false;
        if (!piece1.isWhite || !piece2.isWhite) return false;
        return true;
    }

    private void PerformMerge(Piece rewardPiece, Piece targetPiece, Tile targetTile)
    {
        int newMergeLevel = Mathf.Max(rewardPiece.mergeLevel, targetPiece.mergeLevel) + 1;

        rewardPiece.SetPosition(targetTile.x, targetTile.y);
        targetTile.currentPiece = rewardPiece;

        rewardPiece.ApplyMergeStats(newMergeLevel);

        ShowMergeLevelText(targetTile.transform.position, rewardPiece, newMergeLevel);


        Vector3 targetOriginalScale = targetPiece.transform.localScale;
        Vector3 rewardOriginalScale = rewardPiece.transform.localScale;

        Tween.LocalScale(targetPiece.transform, targetOriginalScale * 1.3f, 0.2f, 0f, AnimationCurve.EaseInOut(0,0,1,1));
        Tween.LocalScale(rewardPiece.transform, rewardOriginalScale * 1.3f, 0.2f, 0f, AnimationCurve.EaseInOut(0,0,1,1),
            completeCallback: () =>
            {
                Tween.LocalScale(rewardPiece.transform, rewardOriginalScale, 0.2f, 0f, AnimationCurve.EaseInOut(0,0,1,1));
                Destroy(targetPiece.gameObject);
            });

        Debug.Log($"New level: {newMergeLevel}");
    }

    private void ShowMergeLevelText(Vector3 position, Piece piece, int mergeLevel)
    {
        if (piece.floatingDamageTextPrefab == null) return;

        GameObject obj = Instantiate(piece.floatingDamageTextPrefab, position + Vector3.up * 0.5f, Quaternion.identity);

        FloatingDamageText text = obj.GetComponent<FloatingDamageText>();
        if (text != null)
        {
            text.InitMergeLevel(mergeLevel);
        }
    }
    
    public bool IsMergeCandidate(Tile tile)
    {
        if (tile?.currentPiece == null) return false;

        if (!IsPurchasedPlayerPiece(tile.currentPiece))
            return false;

        foreach (Piece candidate in mergeCandidates)
        {
            if (candidate.x == tile.x && candidate.y == tile.y)
                return true;
        }

        return false;
    }
    
    public bool IsPieceMergeHighlighted(Piece piece)
    {
        return piece != null && piece.isMergingCandidate;
    }
}