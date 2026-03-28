using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool whiteTurn = true;
    public Piece selectedPiece;
    public BoardManager board;

    [HideInInspector]
    public List<Vector2Int> currentMoves = new List<Vector2Int>();
    
    [Header("Controllers")]
    public MoveController moveController;
    
    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this) Destroy(gameObject);
    }

    void Start()
    {
        EnableAllTileColliders();
        UpdateTileCollidersUnderPieces();
    }

    private void EnableAllTileColliders()
    {
        if (board == null || board.tiles == null) return;
        for (int x = 0; x < board.width; x++)
            for (int y = 0; y < board.height; y++)
                board.tiles[x, y]?.SetColliderEnabled(true);
    }

    private void UpdateTileCollidersUnderPieces()
    {
        if (board == null || board.tiles == null) return;
        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Tile tile = board.tiles[x, y];
                if (tile != null) tile.SetColliderEnabled(tile.currentPiece == null);
            }
        }
    }

    public void UpdateTileCollidersImmediately() => UpdateTileCollidersUnderPieces();

    public void SelectPiece(Piece piece, bool isAI = false)
    {
        if (piece == null) return;

        if (LevelManager.Instance != null && LevelManager.Instance.IsWaitingForRewardPlacement())
        {
            if (LevelManager.Instance.RewardOptionsContains(piece))
            {
                LevelManager.Instance.SelectReward(piece);
                HighlightRewardPlacement();
            }
            return;
        }

        if (piece.isWhite != whiteTurn)
        {
            if (LevelManager.Instance == null || !LevelManager.Instance.IsWaitingForRewardPlacement())
            {
                ClearHighlights();
                selectedPiece = null;
                currentMoves.Clear();
                UpdateTileCollidersUnderPieces();
            }
            return;
        }

        if (selectedPiece != null && selectedPiece != piece)
            selectedPiece.SetSelected(false);

        selectedPiece = piece;
        selectedPiece.SetSelected(true);

        currentMoves = piece.GetMoves(board.tiles, board.width, board.height);

        currentMoves.RemoveAll(move => !IsValidRangedAttack(piece, move));

        HighlightMoves();
        SetCollidersForSelectedPiece();
    }

    private bool IsValidRangedAttack(Piece piece, Vector2Int move)
    {
        if (move.x < 0 || move.x >= board.width || move.y < 0 || move.y >= board.height)
            return false;

        Tile tile = board.tiles[move.x, move.y];
        if (tile == null) return false;

        if (!piece.IsRangedAttack(move))
            return true;

        if (tile.currentPiece == null)
            return true;

        return moveController.HasLineOfSight(piece, move);
    }

    private void SetCollidersForSelectedPiece()
    {
        if (board == null || board.tiles == null) return;

        for (int x = 0; x < board.width; x++)
            for (int y = 0; y < board.height; y++)
                board.tiles[x, y]?.SetColliderEnabled(false);

        foreach (var move in currentMoves)
        {
            if (move.x < 0 || move.x >= board.width ||
                move.y < 0 || move.y >= board.height)
                continue;

            Tile tile = board.tiles[move.x, move.y];
            if (tile == null) continue;

            if (tile.currentPiece == null)
            {
                tile.SetColliderEnabled(true);
            }
            else
            {
                tile.SetColliderEnabled(false);
            }
        }
    }

    public bool TryAIMove(Piece piece, Tile targetTile)
    {
        if (piece == null || targetTile == null) return false;
        if (piece.isWhite) return false;

        selectedPiece = piece;
        currentMoves = piece.GetMoves(board.tiles, board.width, board.height);

        Vector2Int target = new Vector2Int(targetTile.x, targetTile.y);
        if (!currentMoves.Contains(target))
        {
            selectedPiece = null;
            currentMoves.Clear();
            UpdateTileCollidersUnderPieces();
            return false;
        }

        TryMove(targetTile);
        return true;
    }

    public void TryMove(Tile tile)
    {
        bool success = moveController.TryExecuteMove(selectedPiece, tile, currentMoves);
        
        if (!success)
        {
            ClearHighlights();
            selectedPiece = null;
            currentMoves.Clear();
            UpdateTileCollidersUnderPieces();
        }
    }

    public void HandlePieceDeath(Piece killedPiece)
    {
        if (killedPiece == null) return;

        Tile tile = board.tiles[killedPiece.x, killedPiece.y];
        
        if (!killedPiece.isWhite)
            killedPiece.SpawnCurrencyAround();

        if (tile != null)
            tile.currentPiece = null;

        Destroy(killedPiece.gameObject);

        LevelManager.Instance?.OnPieceKilled(killedPiece);

        LevelManager.Instance?.CheckLevelCompletion();

        UpdateTileCollidersUnderPieces();
    }

    public void EndTurn()
    {
        ClearHighlights();
        
        if (selectedPiece != null)
        {
            selectedPiece.SetSelected(false);
            selectedPiece = null;
        }
        
        currentMoves.Clear();
        whiteTurn = !whiteTurn;
        
        LevelManager.Instance?.CheckLevelCompletion();

        if (!whiteTurn && EnemyAI.Instance != null)
        {
            bool hasEnemy = false;
            foreach (Tile t in board.tiles)
                if (t?.currentPiece != null && !t.currentPiece.isWhite) { hasEnemy = true; break; }

            if (hasEnemy) 
                Invoke(nameof(EnemyTurn), 1f);
            else 
                EndTurn();
        }
        else
        {
            EnableAllTileColliders();
            UpdateTileCollidersUnderPieces();
        }
    }

    void EnemyTurn()
    {
        if (EnemyAI.Instance != null && EnemyAI.Instance.gameObject.activeInHierarchy)
            EnemyAI.Instance.MakeMove();
        else EndTurn();
    }

    void HighlightMoves()
    {
        if (board == null || currentMoves == null) return;

        foreach (var move in currentMoves)
        {
            if (move.x < 0 || move.x >= board.width ||
                move.y < 0 || move.y >= board.height)
                continue;

            Tile tile = board.tiles[move.x, move.y];
            if (tile == null) continue;

            if (tile.currentPiece == null)
            {
                tile.HighlightMove();
            }
            else
            {
                Piece targetPiece = tile.currentPiece;

                if (selectedPiece is Cleric cleric)
                {
                    if (targetPiece.isWhite == selectedPiece.isWhite)
                    {
                        tile.HighlightHeal();
                        targetPiece.SetHealHighlight(true);
                    }
                    else
                    {
                        tile.HighlightAttack();
                        targetPiece.SetAttackHighlight(true);
                    }
                }
                else
                {
                    tile.HighlightAttack();

                    if (targetPiece.isWhite != selectedPiece.isWhite)
                    {
                        targetPiece.SetAttackHighlight(true);
                    }
                }
            }
        }
    }

    public void HighlightRewardPlacement()
    {
        bool shouldKeepPieceHighlight = (selectedPiece != null && 
                                        LevelManager.Instance != null && 
                                        LevelManager.Instance.RewardOptionsContains(selectedPiece));
        
        ClearHighlights();
        
        if (shouldKeepPieceHighlight && selectedPiece != null)
        {
            selectedPiece.SetSelected(true);
        }
        
        if (board == null) return;

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                Tile tile = board.tiles[x, y];
                if (tile != null)
                {
                    if (tile.currentPiece == null)
                    {
                        tile.HighlightMove();
                        tile.SetColliderEnabled(true);
                    }
                    else 
                    {
                        tile.SetColliderEnabled(false);
                    }
                }
            }
        }

        for (int x = 0; x < board.width; x++)
            for (int y = 2; y < board.height; y++)
                board.tiles[x, y]?.SetColliderEnabled(false);
    }

    public void ClearHighlights()
    {
        if (board == null || board.tiles == null) return;

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Tile tile = board.tiles[x, y];
                if (tile != null)
                {
                    if (tile.currentPiece == null || 
                        (MergeManager.Instance != null && 
                         !MergeManager.Instance.IsPieceMergeHighlighted(tile.currentPiece)))
                    {
                        tile.ResetHighlight();
                    }
                }
            }
        }

        ClearEnemyHighlights();
        EnableAllTileColliders();
        UpdateTileCollidersUnderPieces();
    }

    private void ClearEnemyHighlights()
    {
        if (board == null || board.tiles == null) return;

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Piece piece = board.tiles[x, y]?.currentPiece;
                if (piece != null)
                {
                    if (!piece.isMergingCandidate)
                    {
                        piece.SetAttackHighlight(false);
                        piece.SetHealHighlight(false);
                        piece.SetSelected(false);
                    }
                }
            }
        }
    }
}
