using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectV1
{
    public class ChessEngine
    {

        private const int Empty = 0;
        private const int Pawn = 1;
        private const int Knight = 2;
        private const int Bishop = 3;
        private const int Rook = 4;
        private const int Queen = 5;
        private const int King = 6;

        private int[] _board = new int[64];
        private bool _whiteTurn = true;
        private int _castlingRights = 15;


        private static readonly int[] PawnTable = { 0, 0, 0, 0, 0, 0, 0, 0, 50, 50, 50, 50, 50, 50, 50, 50, 10, 10, 20, 30, 30, 20, 10, 10, 5, 5, 10, 25, 25, 10, 5, 5, 0, 0, 0, 20, 20, 0, 0, 0, 5, -5, -10, 0, 0, -10, -5, 5, 5, 10, 10, -20, -20, 10, 10, 5, 0, 0, 0, 0, 0, 0, 0, 0 };
        private static readonly int[] KnightTable = { -50, -40, -30, -30, -30, -30, -40, -50, -40, -20, 0, 0, 0, 0, -20, -40, -30, 0, 10, 15, 15, 10, 0, -30, -30, 5, 15, 20, 20, 15, 5, -30, -30, 0, 15, 20, 20, 15, 0, -30, -30, 5, 10, 15, 15, 10, 5, -30, -40, -20, 0, 5, 5, 0, -20, -40, -50, -40, -30, -30, -30, -30, -40, -50 };
        private static readonly int[] BishopTable = { -20, -10, -10, -10, -10, -10, -10, -20, -10, 0, 0, 0, 0, 0, 0, -10, -10, 0, 5, 10, 10, 5, 0, -10, -10, 5, 5, 10, 10, 5, 5, -10, -10, 0, 10, 10, 10, 10, 0, -10, -10, 10, 10, 10, 10, 10, 10, -10, -10, 5, 0, 0, 0, 0, 5, -10, -20, -10, -10, -10, -10, -10, -10, -20 };
        private static readonly int[] RookTable = { 0, 0, 0, 0, 0, 0, 0, 0, 5, 10, 10, 10, 10, 10, 10, 5, -5, 0, 0, 0, 0, 0, 0, -5, -5, 0, 0, 0, 0, 0, 0, -5, -5, 0, 0, 0, 0, 0, 0, -5, -5, 0, 0, 0, 0, 0, 0, -5, -5, 0, 0, 0, 0, 0, 0, -5, 0, 0, 0, 5, 5, 0, 0, 0 };
        private static readonly int[] QueenTable = { -20, -10, -10, -5, -5, -10, -10, -20, -10, 0, 0, 0, 0, 0, 0, -10, -10, 0, 5, 5, 5, 5, 0, -10, -5, 0, 5, 5, 5, 5, 0, -5, 0, 0, 5, 5, 5, 5, 0, -5, -10, 5, 5, 5, 5, 5, 0, -10, -10, 0, 5, 0, 0, 0, 0, -10, -20, -10, -10, -5, -5, -10, -10, -20 };
        private static readonly int[] KingTable = { -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -30, -40, -40, -50, -50, -40, -40, -30, -20, -30, -30, -40, -40, -30, -30, -20, -10, -20, -20, -20, -20, -20, -20, -10, 20, 20, 0, 0, 0, 0, 20, 20, 20, 30, 10, 0, 0, 10, 30, 20 };

        public ChessEngine(string fen) { ParseFen(fen); }

        public string GetBestMove()
        {
            int depth = 4;
            int bestScore = int.MinValue;
            Move bestMove = null;

            List<Move> moves = GenerateMoves(_whiteTurn);
            moves = OrderMoves(moves);

            foreach (var move in moves)
            {
                MakeMove(move);
                int score = -Minimax(depth - 1, -100000, 100000, !_whiteTurn);
                UndoMove(move);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove = move;
                }
            }

            if (bestMove == null) return "";

            // --- שינוי כאן: מחזיר גם את המהלך וגם את הציון מופרדים בקו ---
            return $"{GetMoveString(bestMove)}|{bestScore}";
        }

        // ... (כל שאר הפונקציות: Minimax, Quiescence, Evaluate, OrderMoves, GenerateMoves וכו' נשארות זהות לקוד האחרון שעבד טוב) ...


        private int Minimax(int depth, int alpha, int beta, bool maximizingPlayer)
        {
            if (depth == 0) return Quiescence(alpha, beta, maximizingPlayer, 4);

            List<Move> moves = GenerateMoves(maximizingPlayer);
            if (moves.Count == 0)
            {
                if (IsKingInCheck(maximizingPlayer)) return -99999 + depth;
                return 0;
            }

            moves = OrderMoves(moves);

            foreach (var move in moves)
            {
                MakeMove(move);
                int score = -Minimax(depth - 1, -beta, -alpha, !maximizingPlayer);
                UndoMove(move);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            return alpha;
        }

        private int Quiescence(int alpha, int beta, bool maximizingPlayer, int qDepth)
        {
            int standPat = Evaluate(maximizingPlayer);
            if (qDepth == 0) return standPat;
            if (standPat >= beta) return beta;
            if (alpha < standPat) alpha = standPat;

            List<Move> captures = GenerateMoves(maximizingPlayer, true);
            captures = OrderMoves(captures);

            foreach (var move in captures)
            {
                MakeMove(move);
                int score = -Quiescence(-beta, -alpha, !maximizingPlayer, qDepth - 1);
                UndoMove(move);

                if (score >= beta) return beta;
                if (score > alpha) alpha = score;
            }
            return alpha;
        }

        private List<Move> OrderMoves(List<Move> moves)
        {
            return moves.OrderByDescending(m =>
            {
                int score = 0;
                if (m.Captured != 0)
                    score = 10 * GetPieceValue(Math.Abs(m.Captured)) - GetPieceValue(Math.Abs(_board[m.From]));
                return score;
            }).ToList();
        }

        private int Evaluate(bool whiteTurn)
        {
            int score = 0;
            int whiteMaterial = 0;
            int blackMaterial = 0;

            for (int i = 0; i < 64; i++)
            {
                int piece = _board[i];
                if (piece == 0) continue;
                int type = Math.Abs(piece);
                int val = GetPieceValue(type);

                if (piece > 0) whiteMaterial += val; else blackMaterial += val;

                int posScore = 0;
                int idx = (piece > 0) ? i : (i ^ 56);
                switch (type)
                {
                    case Pawn: posScore = PawnTable[idx]; break;
                    case Knight: posScore = KnightTable[idx]; break;
                    case Bishop: posScore = BishopTable[idx]; break;
                    case Rook: posScore = RookTable[idx]; break;
                    case Queen: posScore = QueenTable[idx]; break;
                    case King: posScore = KingTable[idx]; break;
                }
                score += (piece > 0) ? (val + posScore) : -(val + posScore);
            }

            int eval = whiteTurn ? score : -score;

            // Endgame Logic
            int myMat = whiteTurn ? whiteMaterial : blackMaterial;
            int oppMat = whiteTurn ? blackMaterial : whiteMaterial;
            if (myMat > oppMat + 400 && oppMat < 1500)
            {
                int myKingIdx = Array.IndexOf(_board, whiteTurn ? King : -King);
                int oppKingIdx = Array.IndexOf(_board, whiteTurn ? -King : King);

                if (myKingIdx != -1 && oppKingIdx != -1)
                {
                    eval += 10 * (14 - (Math.Abs((myKingIdx / 8) - (oppKingIdx / 8)) + Math.Abs((myKingIdx % 8) - (oppKingIdx % 8))));
                    int oppRow = oppKingIdx / 8, oppCol = oppKingIdx % 8;
                    int centerDist = Math.Max(3 - oppRow, oppRow - 4) + Math.Max(3 - oppCol, oppCol - 4);
                    eval += centerDist * 10;
                }
            }
            return eval;
        }

        private int GetPieceValue(int pieceType)
        {
            switch (pieceType) { case Pawn: return 100; case Knight: return 320; case Bishop: return 330; case Rook: return 500; case Queen: return 900; case King: return 20000; default: return 0; }
        }

        private List<Move> GenerateMoves(bool white, bool capturesOnly = false)
        {
            List<Move> pseudoMoves = new List<Move>();
            for (int i = 0; i < 64; i++)
            {
                int piece = _board[i];
                if (piece == 0) continue;
                if ((white && piece < 0) || (!white && piece > 0)) continue;
                int type = Math.Abs(piece);
                int row = i / 8, col = i % 8;

                if (type == Pawn) GeneratePawnMoves(pseudoMoves, i, white, row, col, capturesOnly);
                else if (type == Knight) GenerateSteppingMoves(pseudoMoves, i, new int[] { -17, -15, -10, -6, 6, 10, 15, 17 }, capturesOnly);
                else if (type == King)
                {
                    GenerateSteppingMoves(pseudoMoves, i, new int[] { -9, -8, -7, -1, 1, 7, 8, 9 }, capturesOnly);
                    if (!capturesOnly) GenerateCastlingMoves(pseudoMoves, i, white);
                }
                else GenerateSlidingMoves(pseudoMoves, i, type, capturesOnly);
            }

            List<Move> legalMoves = new List<Move>();
            foreach (var move in pseudoMoves)
            {
                MakeMove(move);
                if (!IsKingInCheck(white)) legalMoves.Add(move);
                UndoMove(move);
            }
            return legalMoves;
        }

        private void GenerateCastlingMoves(List<Move> moves, int kingIdx, bool white)
        {
            if (white && kingIdx != 60) return;
            if (!white && kingIdx != 4) return;
            if (IsKingInCheck(white)) return;

            bool kingSide = white ? (_castlingRights & 1) != 0 : (_castlingRights & 4) != 0;
            bool queenSide = white ? (_castlingRights & 2) != 0 : (_castlingRights & 8) != 0;

            if (kingSide && _board[kingIdx + 1] == 0 && _board[kingIdx + 2] == 0)
                if (!IsSquareAttacked(kingIdx + 1, !white) && !IsSquareAttacked(kingIdx + 2, !white))
                    moves.Add(new Move { From = kingIdx, To = kingIdx + 2, Captured = 0, IsCastling = true });

            if (queenSide && _board[kingIdx - 1] == 0 && _board[kingIdx - 2] == 0 && _board[kingIdx - 3] == 0)
                if (!IsSquareAttacked(kingIdx - 1, !white) && !IsSquareAttacked(kingIdx - 2, !white))
                    moves.Add(new Move { From = kingIdx, To = kingIdx - 2, Captured = 0, IsCastling = true });
        }

        private void GeneratePawnMoves(List<Move> moves, int idx, bool white, int row, int col, bool capturesOnly)
        {
            int direction = white ? -8 : 8;
            if (!capturesOnly)
            {
                if (IsValid(idx + direction) && _board[idx + direction] == 0)
                {
                    moves.Add(new Move { From = idx, To = idx + direction, Captured = 0 });
                    int startRow = white ? 6 : 1;
                    if (row == startRow && _board[idx + direction * 2] == 0)
                        moves.Add(new Move { From = idx, To = idx + direction * 2, Captured = 0 });
                }
            }
            int[] captures = { direction - 1, direction + 1 };
            foreach (int cap in captures)
            {
                int target = idx + cap;
                if (IsValid(target) && Math.Abs((target % 8) - col) == 1)
                {
                    int targetPiece = _board[target];
                    if (targetPiece != 0 && (white ? targetPiece < 0 : targetPiece > 0))
                        moves.Add(new Move { From = idx, To = target, Captured = targetPiece });
                }
            }
        }

        private void GenerateSteppingMoves(List<Move> moves, int idx, int[] offsets, bool capturesOnly)
        {
            foreach (int offset in offsets)
            {
                int target = idx + offset;
                if (IsValid(target) && Math.Abs((idx % 8) - (target % 8)) <= 2)
                {
                    int targetPiece = _board[target];
                    bool isCapture = targetPiece != 0 && (_board[idx] > 0 ? targetPiece < 0 : targetPiece > 0);
                    if (!capturesOnly && targetPiece == 0) moves.Add(new Move { From = idx, To = target, Captured = 0 });
                    else if (isCapture) moves.Add(new Move { From = idx, To = target, Captured = targetPiece });
                }
            }
        }

        private void GenerateSlidingMoves(List<Move> moves, int idx, int type, bool capturesOnly)
        {
            int[] dirs = (type == Bishop) ? new int[] { -9, -7, 7, 9 } : (type == Rook) ? new int[] { -8, -1, 1, 8 } : new int[] { -9, -8, -7, -1, 1, 7, 8, 9 };
            foreach (int dir in dirs)
            {
                int target = idx;
                while (true)
                {
                    if (Math.Abs((target % 8) - ((target + dir) % 8)) > 1 && Math.Abs(dir) != 8) break;
                    target += dir;
                    if (!IsValid(target)) break;
                    int targetPiece = _board[target];
                    if (targetPiece == 0) { if (!capturesOnly) moves.Add(new Move { From = idx, To = target, Captured = 0 }); }
                    else
                    {
                        if (_board[idx] > 0 ? targetPiece < 0 : targetPiece > 0) moves.Add(new Move { From = idx, To = target, Captured = targetPiece });
                        break;
                    }
                }
            }
        }

        private bool IsKingInCheck(bool whiteKing)
        {
            int kingVal = whiteKing ? King : -King;
            int kingSquare = Array.IndexOf(_board, kingVal);
            if (kingSquare == -1) return true;
            return IsSquareAttacked(kingSquare, !whiteKing);
        }

        private bool IsSquareAttacked(int square, bool byWhite)
        {
            int pawnDir = byWhite ? 8 : -8;
            int targetPawn = byWhite ? Pawn : -Pawn;
            if (IsValid(square + pawnDir - 1) && Math.Abs(((square + pawnDir - 1) % 8) - (square % 8)) == 1 && _board[square + pawnDir - 1] == targetPawn) return true;
            if (IsValid(square + pawnDir + 1) && Math.Abs(((square + pawnDir + 1) % 8) - (square % 8)) == 1 && _board[square + pawnDir + 1] == targetPawn) return true;
            if (CheckLeaperAttack(square, Knight, new int[] { -17, -15, -10, -6, 6, 10, 15, 17 }, byWhite)) return true;
            if (CheckLeaperAttack(square, King, new int[] { -9, -8, -7, -1, 1, 7, 8, 9 }, byWhite)) return true;
            if (CheckSliderAttack(square, Bishop, new int[] { -9, -7, 7, 9 }, byWhite)) return true;
            if (CheckSliderAttack(square, Rook, new int[] { -8, -1, 1, 8 }, byWhite)) return true;
            if (CheckSliderAttack(square, Queen, new int[] { -9, -7, 7, 9, -8, -1, 1, 8 }, byWhite)) return true;
            return false;
        }

        private bool CheckLeaperAttack(int square, int type, int[] offsets, bool attackerIsWhite)
        {
            int targetVal = attackerIsWhite ? type : -type;
            foreach (int offset in offsets)
            {
                int fromSq = square - offset;
                if (IsValid(fromSq) && Math.Abs((fromSq % 8) - (square % 8)) <= 2 && _board[fromSq] == targetVal) return true;
            }
            return false;
        }

        private bool CheckSliderAttack(int square, int type, int[] dirs, bool attackerIsWhite)
        {
            int targetVal = attackerIsWhite ? type : -type;
            int queenVal = attackerIsWhite ? Queen : -Queen;
            foreach (int dir in dirs)
            {
                int cursor = square + dir;
                while (IsValid(cursor) && Math.Abs((cursor % 8) - ((cursor - dir) % 8)) <= 1)
                {
                    int piece = _board[cursor];
                    if (piece != 0) { if (piece == targetVal || piece == queenVal) return true; break; }
                    cursor += dir;
                }
            }
            return false;
        }

        private void MakeMove(Move move)
        {
            if (move.IsCastling)
            {
                if (move.To == move.From + 2) { _board[move.From + 1] = _board[move.From + 3]; _board[move.From + 3] = 0; }
                else if (move.To == move.From - 2) { _board[move.From - 1] = _board[move.From - 4]; _board[move.From - 4] = 0; }
            }
            _board[move.To] = _board[move.From];
            _board[move.From] = 0;
            int type = Math.Abs(_board[move.To]);
            if (type == Pawn)
            {
                int row = move.To / 8;
                if (row == 0 || row == 7) _board[move.To] = (_board[move.To] > 0) ? Queen : -Queen;
            }
        }

        private void UndoMove(Move move)
        {
            int piece = _board[move.To];
            if (move.Captured == 0 && Math.Abs(piece) == Queen)
            {
                int fromRow = move.From / 8;
                if (fromRow == 1 || fromRow == 6) piece = (piece > 0) ? Pawn : -Pawn;
            }
            _board[move.From] = piece;
            _board[move.To] = move.Captured;
            if (move.IsCastling)
            {
                if (move.To == move.From + 2) { _board[move.From + 3] = _board[move.From + 1]; _board[move.From + 1] = 0; }
                else if (move.To == move.From - 2) { _board[move.From - 4] = _board[move.From - 1]; _board[move.From - 1] = 0; }
            }
        }

        private void ParseFen(string fen)
        {
            string[] parts = fen.Split(' ');
            string position = parts[0];
            _whiteTurn = parts[1] == "w";
            string castling = parts.Length > 2 ? parts[2] : "-";
            _castlingRights = 0;
            if (castling.Contains("K")) _castlingRights |= 1;
            if (castling.Contains("Q")) _castlingRights |= 2;
            if (castling.Contains("k")) _castlingRights |= 4;
            if (castling.Contains("q")) _castlingRights |= 8;

            int square = 0;
            foreach (char c in position)
            {
                if (char.IsDigit(c)) square += (int)char.GetNumericValue(c);
                else if (c != '/')
                {
                    int p = 0;
                    switch (char.ToLower(c)) { case 'p': p = Pawn; break; case 'n': p = Knight; break; case 'b': p = Bishop; break; case 'r': p = Rook; break; case 'q': p = Queen; break; case 'k': p = King; break; }
                    _board[square++] = char.IsUpper(c) ? p : -p;
                }
            }
        }

        private string GetMoveString(Move move)
        {
            string[] cols = { "a", "b", "c", "d", "e", "f", "g", "h" };
            string[] rows = { "8", "7", "6", "5", "4", "3", "2", "1" };
            return cols[move.From % 8] + rows[move.From / 8] + cols[move.To % 8] + rows[move.To / 8];
        }

        private bool IsValid(int idx) => idx >= 0 && idx < 64;

        private class Move { public int From; public int To; public int Captured; public bool IsCastling; }
    }
}