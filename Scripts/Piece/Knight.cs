using System.Collections.Generic;
using UnityEngine;

public class Knight : Piece
{
public override List<Vector2Int> GetMoves(Tile[,] board, int width, int height)
{
    List<Vector2Int> moves = new List<Vector2Int>();

    Vector2Int[] allDirections =
    {
        new Vector2Int(1, 0),   new Vector2Int(-1, 0),
        new Vector2Int(0, 1),   new Vector2Int(0, -1),
        new Vector2Int(1, 1),   new Vector2Int(-1, 1),
        new Vector2Int(1, -1),  new Vector2Int(-1, -1)
    };

    foreach (var dir in allDirections)
    {
        int nx = x + dir.x;
        int ny = y + dir.y;

        if (nx < 0 || ny < 0 || nx >= width || ny >= height)
            continue;

        Tile tile = board[nx, ny];
        if (tile == null || tile.isWall) continue;

        Piece target = tile.currentPiece;
        
        bool isDiagonalMove = Mathf.Abs(dir.x) == 1 && Mathf.Abs(dir.y) == 1;
        
        if (target == null)
        {
            moves.Add(new Vector2Int(nx, ny));
        }
        else if (target.isWhite != isWhite)
        {
            if (isDiagonalMove)
            {
                moves.Add(new Vector2Int(nx, ny));
            }
        }
    }

    return moves;
}

    public override void PlayAttackAnimation(Piece target, BoardManager board, System.Action onComplete)
{
}

}
