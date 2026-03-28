using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAI : MonoBehaviour
{
    public static EnemyAI Instance;

    [Header("AI Settings")]
    public float thinkTime = 0.3f;

    private bool isMakingMove = false;
    private Piece lastMovedPiece;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void MakeMove()
    {
        if (isMakingMove || GameManager.Instance == null || GameManager.Instance.whiteTurn)
            return;

        StartCoroutine(MakeMoveRoutine());
    }

    private IEnumerator MakeMoveRoutine()
    {
        isMakingMove = true;
        yield return new WaitForSeconds(thinkTime);

        BoardManager board = GameManager.Instance.board;
        List<Piece> enemyPieces = GetAllEnemyPieces();
        AIMove chosenMove = null;

        foreach (var piece in enemyPieces)
        {
            foreach (var pos in piece.GetMoves(board.tiles, board.width, board.height))
            {
                Tile target = board.tiles[pos.x, pos.y];
                if (target == null || target.currentPiece == null)
                    continue;

                if (!target.currentPiece.isWhite)
                    continue;

                if (piece.IsRangedAttack(new Vector2Int(target.x, target.y)) &&
                    !HasLineOfSightAI(piece, target))
                    continue;

                chosenMove = new AIMove(piece, target);
                break;
            }

            if (chosenMove != null)
                break;
        }

        if (chosenMove == null)
        {
            AIMove bestAttackSetupMove = null;
            int minDistance = int.MaxValue;
            bool canSkipLastMoved = enemyPieces.Count > 1;

            foreach (var piece in enemyPieces)
            {
                if (piece == lastMovedPiece && canSkipLastMoved)
                    continue;

                foreach (var pos in piece.GetMoves(board.tiles, board.width, board.height))
                {
                    Tile target = board.tiles[pos.x, pos.y];
                    if (target == null || target.currentPiece != null)
                        continue;

                    if (!MoveCreatesAttack(piece, target))
                        continue;

                    int dist = DistanceToClosestWhite(piece, target);
                    if (dist < minDistance)
                    {
                        minDistance = dist;
                        bestAttackSetupMove = new AIMove(piece, target);
                    }
                }
            }

            if (bestAttackSetupMove != null)
                chosenMove = bestAttackSetupMove;
        }

        if (chosenMove == null)
        {
            AIMove bestMove = null;
            int bestDistance = int.MaxValue;
            bool canSkipLastMoved = enemyPieces.Count > 1;

            foreach (var piece in enemyPieces)
            {
                if (piece == lastMovedPiece && canSkipLastMoved)
                    continue;

                foreach (var pos in piece.GetMoves(board.tiles, board.width, board.height))
                {
                    Tile target = board.tiles[pos.x, pos.y];
                    if (target == null || target.currentPiece != null)
                        continue;

                    int dist = DistanceToClosestWhite(piece, target);
                    if (dist < bestDistance)
                    {
                        bestDistance = dist;
                        bestMove = new AIMove(piece, target);
                    }
                }
            }

            if (bestMove != null)
                chosenMove = bestMove;
        }

        if (chosenMove == null)
        {
            foreach (var piece in enemyPieces)
            {
                foreach (var pos in piece.GetMoves(board.tiles, board.width, board.height))
                {
                    Tile target = board.tiles[pos.x, pos.y];
                    if (target != null && target.currentPiece == null)
                    {
                        chosenMove = new AIMove(piece, target);
                        break;
                    }
                }

                if (chosenMove != null)
                    break;
            }
        }

        if (chosenMove != null)
        {
            lastMovedPiece = chosenMove.piece;
            GameManager.Instance.TryAIMove(chosenMove.piece, chosenMove.targetTile);
        }
        else
        {
            GameManager.Instance.EndTurn();
        }

        isMakingMove = false;
    }

    bool HasLineOfSightAI(Piece attacker, Tile target)
    {
        BoardManager board = GameManager.Instance.board;

        int dx = Mathf.Clamp(target.x - attacker.x, -1, 1);
        int dy = Mathf.Clamp(target.y - attacker.y, -1, 1);

        int x = attacker.x;
        int y = attacker.y;

        while (true)
        {
            x += dx;
            y += dy;

            if (x < 0 || y < 0 || x >= board.width || y >= board.height)
                return false;

            Tile tile = board.tiles[x, y];
            if (tile == null)
                continue;

            if (tile.isWall)
                return false;

            if (x == target.x && y == target.y)
                return true;

            if (tile.currentPiece != null)
                return false;
        }
    }

    bool MoveCreatesAttack(Piece piece, Tile targetTile)
    {
        BoardManager board = GameManager.Instance.board;

        Tile fromTile = board.tiles[piece.x, piece.y];
        Piece savedTargetPiece = targetTile.currentPiece;

        int oldX = piece.x;
        int oldY = piece.y;

        // временно делаем ход
        fromTile.currentPiece = null;
        targetTile.currentPiece = piece;
        piece.x = targetTile.x;
        piece.y = targetTile.y;

        bool canAttack = false;

        foreach (var pos in piece.GetMoves(board.tiles, board.width, board.height))
        {
            Tile t = board.tiles[pos.x, pos.y];
            if (t == null || t.currentPiece == null)
                continue;

            if (!t.currentPiece.isWhite)
                continue;

            if (piece.IsRangedAttack(new Vector2Int(t.x, t.y)) &&
                !HasLineOfSightAI(piece, t))
                continue;

            canAttack = true;
            break;
        }

        fromTile.currentPiece = piece;
        targetTile.currentPiece = savedTargetPiece;
        piece.x = oldX;
        piece.y = oldY;

        return canAttack;
    }

    int DistanceToClosestWhite(Piece piece, Tile tile)
    {
        BoardManager board = GameManager.Instance.board;
        int minDistance = int.MaxValue;

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Tile t = board.tiles[x, y];
                if (t == null || t.currentPiece == null)
                    continue;

                if (t.currentPiece.isWhite)
                {
                    int dist = Mathf.Abs(tile.x - x) + Mathf.Abs(tile.y - y);
                    if (dist < minDistance)
                        minDistance = dist;
                }
            }
        }

        return minDistance;
    }

    private List<Piece> GetAllEnemyPieces()
    {
        List<Piece> pieces = new List<Piece>();
        BoardManager board = GameManager.Instance.board;

        for (int x = 0; x < board.width; x++)
        {
            for (int y = 0; y < board.height; y++)
            {
                Tile tile = board.tiles[x, y];
                if (tile != null && tile.currentPiece != null && !tile.currentPiece.isWhite)
                    pieces.Add(tile.currentPiece);
            }
        }

        return pieces;
    }
}

public class AIMove
{
    public Piece piece;
    public Tile targetTile;

    public AIMove(Piece p, Tile t)
    {
        piece = p;
        targetTile = t;
    }
}
