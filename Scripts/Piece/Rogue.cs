using System.Collections.Generic;
using UnityEngine;

public class Rogue : Piece
{
    public override List<Vector2Int> GetMoves(Tile[,] board, int width, int height)
    {
        List<Vector2Int> moves = new List<Vector2Int>();

        Vector2Int[] offsets =
        {
            new Vector2Int(2,1), new Vector2Int(2,-1),
            new Vector2Int(-2,1), new Vector2Int(-2,-1),
            new Vector2Int(1,2), new Vector2Int(1,-2),
            new Vector2Int(-1,2), new Vector2Int(-1,-2)
        };

        foreach (var o in offsets)
        {
            int nx = x + o.x;
            int ny = y + o.y;

            if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;

            Tile tile = board[nx, ny];
            if (tile == null || tile.isWall) continue;

            Piece target = tile.currentPiece;
            if (target == null || target.isWhite != isWhite)
                moves.Add(new Vector2Int(nx, ny));
        }

        return moves;
    }

    public override void PlayAttackAnimation(Piece target, BoardManager board, System.Action onComplete)
    {
    }

}
