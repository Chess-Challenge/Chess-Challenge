﻿//using ChessChallenge.API;
//using System;
//using System.Collections.Generic;
//using System.Diagnostics;
//using System.Linq;

//public class MyBot : IChessBot
//{
//    int[] pieceValues = { 0, 1, 3, 3, 5, 9, 200 };

//    public Move Think(Board board, Timer timer)
//    {
//        var node = new Node();
//        node.Eval = this.Search(node, 4, float.NegativeInfinity, float.PositiveInfinity, board);

//        var bestSubNode = node.SubNodes.MaxBy(x => x.Eval);
//        return bestSubNode.Moves.Last();
//    }

//    private float Search(Node node, int depth, float alpha, float beta, Board board)
//    {
//        node.FEN = board.GetFenString();

//        if (depth == 0)
//        {
//            var materialValue = board.GetAllPieceLists().Sum(pl => pl.Sum(p => (p.IsWhite == board.IsWhiteToMove) ? pieceValues[(int)p.PieceType] : -pieceValues[(int)p.PieceType])) /*random.Next(0, +100)*/;

//            var mobilityValue = 0.1f * board.GetLegalMoves().Length;
//            //board.TrySkipTurn();
//            //mobilityValue += -0.1f * board.GetLegalMoves().Length;
//            //board.UndoSkipTurn();

//            return materialValue + mobilityValue;
//        }

//        var moves = board.GetLegalMoves();
//        float max = float.NegativeInfinity;

//        foreach (var move in moves)
//        {
//            board.MakeMove(move);

//            var sub_playedMoves = node.Moves.ToList();
//            sub_playedMoves.Add(move);

//            var sub_node = new Node(moves: sub_playedMoves);
//            node.SubNodes.Add(sub_node);

//            float eval = 0;
//            if (board.IsDraw())
//            {
//                eval = 0;
//            }
//            else if (board.IsInCheckmate())
//            {
//                eval = +99999;
//            }
//            else
//            {
//                eval = -this.Search(sub_node, depth - 1, -beta, -alpha, board);
//            }
//            sub_node.Eval = eval;

//            if (eval > max)
//            {
//                max = eval;
//            }
//            alpha = Math.Max(eval, alpha);
            
//            board.UndoMove(move);

//            if (alpha >= beta)
//            {
//                break;
//            }
//        }

//        node.SubNodes = node.SubNodes.OrderByDescending(x => x.Eval).ToList();

//        return max;
//    }

//    [DebuggerDisplay("{debug_RecentMove} ({Eval})")]
//    class Node
//    {
//        public string debug_RecentMove => this.Moves.Last().ToString().Replace("Move: ", string.Empty).Trim('\'');

//        public List<Move> Moves { get; set; }
//        public float Eval { get; set; }
//        public List<Node> SubNodes { get; set; }
//        public string? FEN { get; set; } = null;

//        public Node(List<Move>? moves = null, float? eval = null, List<Node>? subNodes = null)
//        {
//            this.Moves = moves ?? new List<Move>();
//            this.SubNodes = subNodes ?? new List<Node>();
//            this.Eval = eval ?? float.NaN;
//        }
//    }
//}