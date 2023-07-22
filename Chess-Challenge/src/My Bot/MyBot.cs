using ChessChallenge.API;
using System;
using System.Linq;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 100, 300, 300, 500, 900, 100000 };
    static Random random = new();

    public Move Think(Board board, Timer timer)
    {
        var (score, move) = NegaMax(board, int.MinValue, int.MaxValue, 4, timer);


        //board.MakeMove(move);

        //if (board.IsDraw())
        //{
        //}

        //board.UndoMove(move);


        return move;
    }

    (int, Move) NegaMax(Board board, int alpha, int beta, int depth, Timer timer)
    {
        //if (timer.MillisecondsElapsedThisTurn > 2_000)
        //{
        //    return (board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType])), Move.NullMove);
        //}
        if (depth == 0)
        {
            return (board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType])), Move.NullMove);
        }

        var moves = board.GetLegalMoves();
        var max = (-int.MaxValue, Move.NullMove);

        foreach (var move in moves)
        {
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return (int.MaxValue, move);
            }
            if (board.IsDraw())
            {
                board.UndoMove(move);
                continue;
            }

            var negaScore = NegaMax(board, -beta, -alpha, depth - 1, timer).Item1;
            if (-negaScore > max.Item1)
            {
                max = (-negaScore, move);
            }
            if (-negaScore >= beta)
            {
                board.UndoMove(move);
                return (beta, move); //  fail hard beta-cutoff
            }
            if (-negaScore > alpha)
            {
                alpha = -negaScore; // alpha acts like max in MiniMa
            }

            board.UndoMove(move);
        }

        if (max.Item2.IsNull)
        {
            if (moves.Length == 0)
            {
                return (0, Move.NullMove);
            }
            return (0, moves[random.Next(moves.Length)]);
        }
        return max;
    }

    //int alphaBeta(int alpha, int beta, int depthleft)
    //{
    //    if (depthleft == 0)
    //        return quiesce(alpha, beta);

    //    for (all moves)
    //    {
    //        score = -alphaBeta(-beta, -alpha, depthleft - 1);
    //        if (score >= beta)
    //            return beta;   //  fail hard beta-cutoff
    //        if (score > alpha)
    //            alpha = score; // alpha acts like max in MiniMax
    //    }
    //    return alpha;
    //}
}