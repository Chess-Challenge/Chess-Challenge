using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Linq;

public class MyBot : IChessBot
{
    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 200 };
    Dictionary<ulong, Node> transpositionTable;

    public Move Think(Board board, Timer timer)
    {
#if DEBUG
        transpositionTableUpdated = 0;
        transpositionTableUsed = 0;
        totalCounter = 0;
#endif

        transpositionTable = new();

        var node = new Node(4);
        node.Eval = this.Search(node, 4, float.NegativeInfinity, float.PositiveInfinity, board);

#if DEBUG
        Console.WriteLine($"{node.Eval:0.00}");

        Console.WriteLine($"TT-Updated: {100f * transpositionTableUpdated / (float)totalCounter:0.00}%");
        Console.WriteLine($"TT-Used: {100f * transpositionTableUsed / (float)totalCounter:0.00}%");
        Console.WriteLine();
#endif

        return node.SubNodes.MaxBy(x => x.Eval).Moves.Last();
    }

#if DEBUG
    int transpositionTableUpdated;
    int transpositionTableUsed;
    int totalCounter;
#endif

    private float Search(Node node, int depthLeft, float alpha, float beta, Board board)
    {
        if (depthLeft == 0)
        {
            var materialValue = board.GetAllPieceLists().Sum(pl => pl.Sum(p =>
                (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType])) /*random.Next(0, +100)*/;

            var mobilityValue = 0.1f * board.GetLegalMoves().Length;
            if (board.TrySkipTurn())
            {
                mobilityValue += -0.1f * board.GetLegalMoves().Length;
                board.UndoSkipTurn();
                return materialValue + mobilityValue;
            }
            return materialValue;
        }

        var moves = board.GetLegalMoves();
        float max = float.NegativeInfinity;

        foreach (var move in moves)
        {
            board.MakeMove(move);

            var sub_playedMoves = node.Moves.ToList();
            sub_playedMoves.Add(move);

            bool found = transpositionTable.TryGetValue(board.ZobristKey, out var sub_node);
            if (!found)
            {
                sub_node = new Node(depthLeft, moves: sub_playedMoves);
            }

#if DEBUG
            if (found)
            {
                if (depthLeft > sub_node.DepthLeft)
                {
                    transpositionTableUpdated++;
                }
                else
                {
                    transpositionTableUsed++;
                }
            }
            totalCounter++;
#endif

            if (!found || depthLeft > sub_node.DepthLeft)
            {
                sub_node.DepthLeft = depthLeft;
                sub_node.Moves = sub_playedMoves;

                if (board.IsDraw())
                {
                    sub_node.Eval = 0;
                }
                else if (board.IsInCheckmate())
                {
                    sub_node.Eval = +99999 + depthLeft;
                }
                else
                {
                    sub_node.Eval = -this.Search(sub_node, depthLeft - 1, -beta, -alpha, board);
                }

                transpositionTable.TryAdd(board.ZobristKey, sub_node);
            }
            node.SubNodes.Add(sub_node);

            if (sub_node.Eval > max)
            {
                max = sub_node.Eval;
            }
            alpha = Math.Max(sub_node.Eval, alpha);

            board.UndoMove(move);

            if (alpha >= beta)
            {
                break;
            }
        }

        node.SubNodes = node.SubNodes.OrderByDescending(x => x.Eval).ToList();

        return max;
    }

    class Node
    {
        public float Eval { get; set; }
        public int DepthLeft { get; set; }

        public List<Move> Moves { get; set; }
        public List<Node> SubNodes { get; set; }

        public Node(int depthLeft, float? eval = null, List<Move>? moves = null, List<Node>? subNodes = null)
        {
            this.DepthLeft = depthLeft;
            this.Eval = eval ?? float.NaN;

            this.Moves = moves ?? new List<Move>();
            this.SubNodes = subNodes ?? new List<Node>();
        }
    }
}