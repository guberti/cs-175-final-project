using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class ChessGame
{
    public enum Type
    {
        PAWN, KNIGHT, BISHOP, ROOK, QUEEN, KING
    };

    public enum Color
    {
        WHITE, BLACK
    };

    public struct Square
    {
        public int row_;    // 1 - 8
        public int col_;    // 1 - 8 corrseponds to A - H

        public Square(int col, int row)
        {
            row_ = row;
            col_ = col;
        }

        public bool inBounds()
        {
            return (1 <= row_ && row_ <= 8) && (1 <= col_ && col_ <= 8);
        }

        public bool equals(Square other)
        {
            return (row_ == other.row_) && (col_ == other.col_);
        }

        public Square add(Square other)
        {
            return new Square(other.col_ + col_, other.row_ + row_);
        }

        public Square times(int i)
        {
            return new Square(col_ * i, row_ * i);
        }

        public override string ToString()
        {
            return "(" + col_ + "," + row_ + ")";
        }
    }

    public abstract class Piece
    {
        public Type t_;
        public Color c_;
        public Square s_;
        public bool moved_;

        public Piece(Type t, Color c, Square s, bool moved = true)
        {
            t_ = t;
            c_ = c;
            s_ = s;
            moved_ = moved;
        }

        public abstract List<Command> getAvailableMoves(out bool checking, bool tbd = false);

        public override string ToString()
        {
            return c_.ToString() + "\t " + t_.ToString() + "\t at \t" + s_.ToString();
        }
    };

    public abstract class Command
    {
        public enum Type
        {
            MOVE, TAKE, CASTLE, PROMOTION
        };
        public Type t_;

        public Command(Type t)
        {
            t_ = t;
        }

        public Pawn prevPassing_ = null;

        public abstract void execute();
        public abstract void undo();

        public abstract bool check();
        public abstract Square getHighlight();
        public abstract string ToString();
    }

    public class Move : Command
    {
        public Square start_, end_;
        public Piece piece_;
        public bool firstMove = false;
        public bool passing_;
        public Move(Square e, Piece p, bool passing = false) : base(Type.MOVE)
        {
            Debug.Assert(p != null, "Passed null piece to Move");
            end_ = e;
            start_ = p.s_;
            piece_ = p;
            passing_ = passing;
        }
        public override void execute()
        {
            Debug.Assert(ChessGame.turn == piece_.c_);
            Debug.Assert(piece_.s_.equals(start_));

            // clear inPassing
            prevPassing_ = inPassing;
            inPassing = null;

            piece_.s_ = end_;

            if (piece_.moved_ == false)
            {
                piece_.moved_ = true;
                firstMove = true;
            }

            if (passing_)
                inPassing = (Pawn)piece_;

            if (piece_.c_ == Color.WHITE)
                turn = Color.BLACK;
            else
                turn = Color.WHITE;
        }

        public override void undo()
        {
            Debug.Assert(ChessGame.turn != piece_.c_);
            Debug.Assert(piece_.s_.equals(end_));

            piece_.s_ = start_;

            inPassing = prevPassing_;

            if (firstMove)
                piece_.moved_ = false;

            if (piece_.c_ == Color.WHITE)
                turn = Color.WHITE;
            else
                turn = Color.BLACK;
        }

        public override bool check()
        {
            execute();
            bool r = ChessGame.inCheck(piece_.c_);
            undo();
            return r == false;
        }

        public override Square getHighlight()
        {
            return end_;
        }

        public override string ToString()
        {
            return piece_.t_.ToString() + start_.ToString() + "->" + end_.ToString();
        }
    }

    public class Take : Command
    {
        public Square start_, end_;
        public Piece taker_, taken_;
        bool firstMove = false;

        public Take(Square end, Piece taker, Piece taken) : base(Type.TAKE)
        {
            end_ = end;
            start_ = taker.s_;
            taker_ = taker;
            taken_ = taken;
        }

        public override void execute()
        {
            Debug.Log("EXECUTE");
            Debug.Assert(ChessGame.turn == taker_.c_);
            Debug.Assert(taker_.s_.equals(start_));
            Debug.Assert(ChessGame.pieces.Contains(taken_));

            taker_.s_ = end_;
            ChessGame.pieces.Remove(taken_);

            prevPassing_ = inPassing;
            inPassing = null;

            if (taker_.moved_ == false)
            {
                taker_.moved_ = true;
                firstMove = true; ;
            }

            if (taker_.c_ == Color.WHITE)
                turn = Color.BLACK;
            else
                turn = Color.WHITE;
        }

        public override void undo()
        {
            Debug.Assert(ChessGame.turn != taker_.c_);
            Debug.Assert(taker_.s_.equals(end_));
            Debug.Assert(ChessGame.pieces.Contains(taken_) == false);

            taker_.s_ = start_;
            ChessGame.pieces.Add(taken_);

            inPassing = prevPassing_;

            if (firstMove)
            {
                taker_.moved_ = false;
            }

            if (taker_.c_ == Color.WHITE)
                turn = Color.WHITE;
            else
                turn = Color.BLACK;
        }

        public override bool check()
        {
            execute();
            bool r = ChessGame.inCheck(taker_.c_);
            undo();
            return r == false;
        }

        public override Square getHighlight()
        {
            return end_;
        }
        public override string ToString()
        {
            return taker_.t_.ToString() + start_.ToString() + "x" + end_.ToString();
        }
    }

    public class Castle : Command
    {
        static Square king_movement_queenside = new Square(-2, 0);
        static Square rook_movement_queenside = new Square(3, 0);
        static Square king_movement_kingside = new Square(2, 0);
        static Square rook_movement_kingside = new Square(-2, 0);

        public Square start_rook_, start_king_, end_rook_, end_king_;
        public Piece king_, rook_;
        public bool kingside_;

        public Castle(Piece king, Piece rook, bool kingside) : base(Type.CASTLE)
        {
            king_ = king;
            rook_ = rook;
            kingside_ = kingside;

            start_rook_ = rook_.s_;
            start_king_ = king_.s_;

            if (kingside_)
            {
                end_rook_ = start_rook_.add(rook_movement_kingside);
                end_king_ = start_king_.add(king_movement_kingside);
            }
            else
            {
                end_rook_ = start_rook_.add(rook_movement_queenside);
                end_king_ = start_king_.add(king_movement_queenside);
            }
        }

        public override void execute()
        {
            Debug.Assert(ChessGame.turn == king_.c_);
            Debug.Assert(king_.s_.equals(start_king_));
            Debug.Assert(rook_.s_.equals(start_rook_));
            Debug.Assert(((King)king_).moved_ == false);
            Debug.Assert(((Rook)rook_).moved_ == false);

            prevPassing_ = inPassing;
            inPassing = null;

            king_.s_ = end_king_;
            rook_.s_ = end_rook_;

            ((King)king_).moved_ = true;
            ((Rook)rook_).moved_ = true;

            if (king_.c_ == Color.WHITE)
                turn = Color.BLACK;
            else
                turn = Color.WHITE;
        }

        public override void undo()
        {
            Debug.Assert(ChessGame.turn != king_.c_);
            Debug.Assert(king_.s_.equals(end_king_));
            Debug.Assert(rook_.s_.equals(end_rook_));
            Debug.Assert(((King)king_).moved_ == true);
            Debug.Assert(((Rook)rook_).moved_ == true);

            inPassing = prevPassing_;

            king_.s_ = start_king_;
            rook_.s_ = start_rook_;

            ((King)king_).moved_ = false;
            ((Rook)rook_).moved_ = false;

            if (king_.c_ == Color.WHITE)
                turn = Color.WHITE;
            else
                turn = Color.BLACK;
        }

        // returns false if the movement is invalid
        public override bool check()
        {
            Square step;
            int max;
            // check if pieces are blocking
            if (kingside_)
                step = new Square(1, 0);
            else
                step = new Square(-1, 0);

            Square curr = king_.s_.add(step);
            if (getPiece(curr) != null)
                return false;
            king_.s_ = curr;
            turn = otherColor(king_.c_);
            if (ChessGame.inCheck(king_.c_))
            {
                king_.s_ = start_king_;
                turn = king_.c_;
                return false;
            }

            curr = king_.s_.add(step);
            if (getPiece(curr) != null)
            {
                king_.s_ = start_king_;
                return false;
            }
            king_.s_ = curr;
            turn = otherColor(king_.c_);
            if (ChessGame.inCheck(king_.c_))
            {
                turn = king_.c_;
                king_.s_ = start_king_;
                return false;
            }

            // reset position
            king_.s_ = start_king_;
            turn = king_.c_;
            return true;
        }

        public override Square getHighlight()
        {
            return end_king_;
        }

        public override string ToString()
        {
            if (kingside_)
                return "0-0";
            return "0-0-0";
        }
    }

    public class Promotion : Command
    {
        public Square start_, end_;
        public Pawn pawn_;
        public Piece piece_ = null;
        public Piece other_ = null;

        public Promotion(Piece pawn, Square end, Piece other = null) : base(Type.PROMOTION)
        {
            Debug.Assert(pawn.t_ == ChessGame.Type.PAWN);
            pawn_ = (Pawn)pawn;
            end_ = end;
            start_ = pawn_.s_;
            other_ = other;
            setUpgrade(ChessGame.Type.QUEEN);
        }

        public void setUpgrade(ChessGame.Type upgrade)
        {
            Debug.Assert(upgrade != ChessGame.Type.PAWN && upgrade != ChessGame.Type.KING);

            switch (upgrade)
            {
                case ChessGame.Type.KNIGHT:
                    piece_ = new Knight(pawn_.c_, end_);
                    break;
                case ChessGame.Type.BISHOP:
                    piece_ = new Bishop(pawn_.c_, end_);
                    break;
                case ChessGame.Type.ROOK:
                    piece_ = new Rook(pawn_.c_, end_);
                    piece_.moved_ = true;
                    break;
                case ChessGame.Type.QUEEN:
                    piece_ = new Queen(pawn_.c_, end_);
                    break;
                default:
                    // should not get here
                    break;
            }
        }

        public override void execute()
        {
            Debug.Assert(piece_ != null);
            Debug.Assert(ChessGame.turn == pawn_.c_);
            Debug.Assert(pawn_.s_.equals(start_));
            Debug.Assert(ChessGame.pieces.Contains(piece_) == false);

            prevPassing_ = inPassing;
            inPassing = null;

            ChessGame.pieces.Remove(pawn_);
            ChessGame.pieces.Add(piece_);

            if (other_ != null)
                ChessGame.pieces.Remove(other_);

            if (pawn_.c_ == Color.WHITE)
                turn = Color.BLACK;
            else
                turn = Color.WHITE;
        }

        public override void undo()
        {
            Debug.Assert(piece_ != null);
            Debug.Assert(ChessGame.turn != pawn_.c_);
            Debug.Assert(ChessGame.pieces.Contains(piece_) == true);

            inPassing = prevPassing_;

            ChessGame.pieces.Remove(piece_);
            ChessGame.pieces.Add(pawn_);

            if (other_ != null)
                ChessGame.pieces.Add(other_);

            if (pawn_.c_ == Color.WHITE)
                turn = Color.WHITE;
            else
                turn = Color.BLACK;
        }

        // returns false if the movement is invalid
        public override bool check()
        {
            execute();
            bool r = ChessGame.inCheck(pawn_.c_);
            undo();
            return r == false;
        }

        public override Square getHighlight()
        {
            return end_;
        }

        public override string ToString()
        {
            return end_.ToString() + piece_.t_.ToString();
        }
    }

    static List<Piece> pieces = new List<Piece>();
    static Stack<Command> notation = new Stack<Command>();
    public static Color turn = Color.WHITE;
    public static Pawn inPassing = null;

    public static Piece getPiece(Square s)
    {
        Predicate<Piece> predicate = (Piece p) => { return (p.s_.row_ == s.row_) && (p.s_.col_ == s.col_); };
        return pieces.Find(predicate);
    }

    public class Pawn : Piece
    {
        static Square[] sides = new Square[]
        {
                new Square(1, 0),
                new Square(-1, 0)
        };

        public Pawn(Color c, Square s) : base(Type.PAWN, c, s, false) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd = false)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            // check for pushing
            Square step;
            if (c_ == Color.WHITE)
            {
                step = new Square(0, 1);
            }
            else
            {
                step = new Square(0, -1);
            }

            Square proposed = s_.add(step);
            if (proposed.inBounds())
            {
                Piece temp = getPiece(proposed);

                // empty
                if (temp == null)
                {
                    if (proposed.row_ == 8 || proposed.row_ == 1)
                    {
                        Promotion p = new Promotion(this, proposed);
                        if (tbd || p.check())
                            moves.Add(p);
                    }
                    else
                    {
                        Move m = new Move(proposed, this);
                        if (tbd || m.check())
                            moves.Add(m);
                    }

                    proposed = proposed.add(step);
                    if (proposed.inBounds() && moved_ == false)
                    {
                        temp = getPiece(proposed);

                        if (temp == null)
                        {
                            Move m2 = new Move(proposed, this, true);
                            if (tbd || m2.check())
                                moves.Add(m2);
                        }
                    }
                }
            }

            // check for taking
            foreach (Square side in sides)
            {
                // check for taking
                proposed = s_.add(side).add(step);
                if (proposed.inBounds())
                {
                    Piece temp = getPiece(proposed);

                    // if there is an enemy piece
                    if (temp != null && temp.c_ != c_)
                    {
                        if (proposed.row_ == 8 || proposed.row_ == 1)
                        {
                            Promotion p = new Promotion(this, proposed, temp);
                            if (tbd || p.check())
                                moves.Add(p);
                        }
                        else
                        {
                            Take t = new Take(proposed, this, temp);
                            if (tbd || t.check())
                                moves.Add(t);
                        }
                    }
                }

                // check for en passant
                if (inPassing != null && inPassing.c_ != c_)
                {
                    proposed = s_.add(side);
                    if (proposed.inBounds() && proposed.equals(inPassing.s_))
                    {
                        Take t = new Take(proposed.add(step), this, inPassing);
                        if (tbd || t.check())
                            moves.Add(t);
                    }
                }
            }

            return moves;
        }
    }

    public class Knight : Piece
    {
        static Square[] jumps = new Square[]
        {
            new Square(2, 1),
            new Square(2, -1),
            new Square(-2, 1),
            new Square(-2, -1),
            new Square(1, 2),
            new Square(1, -2),
            new Square(-1, 2),
            new Square(-1, -2)
        };
        public Knight(Color c, Square s) : base(Type.KNIGHT, c, s) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd = false)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            foreach (Square jump in jumps)
            {
                Square proposed = s_.add(jump);
                Piece temp = ChessGame.getPiece(proposed);
                if (proposed.inBounds())
                {
                    if (temp == null)
                    {
                        Move m = new Move(proposed, this);
                        if (tbd || m.check())
                            moves.Add(m);
                    }
                    else if (temp.c_ != c_)
                    {
                        Take t = new Take(proposed, this, temp);
                        if (temp.t_ == Type.KING)
                        {
                            checking = true;
                            moves.Add(t);
                        }
                        if (tbd || t.check())
                            moves.Add(t);
                    }
                }
            }
            return moves;
        }
    }

    public class Bishop : Piece
    {
        static Square[] directions = new Square[]
        {
            new Square(1, 1),
            new Square(1, -1),
            new Square(-1, -1),
            new Square(-1, 1)
        };

        public Bishop(Color c, Square s) : base(Type.BISHOP, c, s) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd = false)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            foreach (Square direction in directions)
            {
                int count = 1;
                bool condition = true;
                while (condition)
                {
                    Square proposed = s_.add(direction.times(count));
                    if (proposed.inBounds())
                    {
                        Piece temp = ChessGame.getPiece(proposed);
                        // if empty
                        if (temp == null)
                        {
                            Move m = new Move(proposed, this);
                            if (tbd || m.check())
                                moves.Add(m);
                        }

                        // if enemey
                        else if (temp.c_ != c_)
                        {
                            Take t = new Take(proposed, this, temp);
                            if (temp.t_ == Type.KING)
                            {
                                checking = true;
                                moves.Add(t);
                            }
                            else if (tbd || t.check())
                                moves.Add(t);

                            condition = false;
                        }

                        // if friendly
                        else
                            condition = false;
                    }
                    else
                        condition = false;
                    count++;
                }
            }
            return moves;
        }
    }

    public class Rook : Piece
    {
        static Square[] directions = new Square[]
        {
            new Square(1, 0),
            new Square(-1, 0),
            new Square(0, 1),
            new Square(0, -1)
        };

        public Rook(Color c, Square s) : base(Type.ROOK, c, s, false) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            foreach (Square direction in directions)
            {
                int count = 1;
                bool condition = true;
                while (condition)
                {
                    Square proposed = s_.add(direction.times(count));
                    if (proposed.inBounds())
                    {
                        Piece temp = ChessGame.getPiece(proposed);
                        // if empty
                        if (temp == null)
                        {
                            Move m = new Move(proposed, this);
                            if (tbd || m.check())
                                moves.Add(m);
                        }

                        // if enemey
                        else if (temp.c_ != c_)
                        {
                            Take t = new Take(proposed, this, temp);
                            if (temp.t_ == Type.KING)
                            {
                                checking = true;
                                moves.Add(t);
                            }
                            else if (tbd || t.check())
                                moves.Add(t);

                            condition = false;
                        }

                        // if friendly
                        else
                            condition = false;
                    }
                    else
                        condition = false;
                    count++;
                }
            }
            return moves;
        }
    }

    public class Queen : Piece
    {
        static Square[] directions = new Square[]
        {
            new Square(1, 1),
            new Square(1, -1),
            new Square(-1, -1),
            new Square(-1, 1),
            new Square(1, 0),
            new Square(-1, 0),
            new Square(0, 1),
            new Square(0, -1)
        };
        public Queen(Color c, Square s) : base(Type.QUEEN, c, s) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd = false)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            foreach (Square direction in directions)
            {
                int count = 1;
                bool condition = true;
                while (condition)
                {
                    Square proposed = s_.add(direction.times(count));
                    // if valid square
                    if (proposed.inBounds())
                    {
                        Piece temp = ChessGame.getPiece(proposed);
                        // if empty square
                        if (temp == null)
                        {
                            Move m = new Move(proposed, this);
                            if (tbd || m.check())
                                moves.Add(m);
                        }

                        // if enemy square
                        else if (temp.c_ != c_)
                        {
                            Take t = new Take(proposed, this, temp);
                            if (temp.t_ == Type.KING)
                            {
                                checking = true;
                                moves.Add(t);
                            }
                            else if (tbd || t.check())
                                moves.Add(t);

                            condition = false;
                        }

                        // if friendly square
                        else
                            condition = false;
                    }
                    else
                        condition = false;
                    count++;
                }
            }
            return moves;
        }
    }

    public class King : Piece
    {
        public bool moved_;

        public King(Color c, Square s) : base(Type.KING, c, s, false) { }

        public override List<Command> getAvailableMoves(out bool checking, bool tbd = false)
        {
            Debug.Log("GetAvailableMoves for " + ToString());
            List<Command> moves = new List<Command>();
            checking = false;

            // check basic movement
            for (int i = -1; i <= 1; i++)
            {
                for (int j = -1; j <= 1; j++)
                {
                    if (i == 0 && j == 0)
                        continue;
                    Square movement = new Square(i, j);
                    Square proposed = s_.add(movement);

                    if (proposed.inBounds())
                    {
                        Piece temp = getPiece(proposed);

                        // if empty
                        if (temp == null)
                        {
                            Move m = new Move(proposed, this);
                            if (tbd || m.check())
                                moves.Add(m);
                        }
                        // if enemy
                        else if (temp.c_ != c_)
                        {
                            Debug.Log("HERE");
                            Take t = new Take(proposed, this, temp);
                            if (temp.t_ == Type.KING)
                            {
                                Debug.Log("Over there");
                                checking = true;
                                moves.Add(t);
                            }
                            else if (tbd)
                            {
                                Debug.Log("INBETWEEN");
                                if (t.check())
                                    moves.Add(t);
                                Debug.Log("Over here");
                            }
                            Debug.Log("THERE");
                        }

                        // if friendly do nothing
                    }
                }
            }

            // check for castling
            if (moved_ == false)
            {
                Piece queenside_rook;
                Piece kingside_rook;
                if (c_ == Color.WHITE)
                {
                    queenside_rook = getPiece(new Square(1, 1));
                    kingside_rook = getPiece(new Square(8, 1));
                }
                else
                {
                    queenside_rook = getPiece(new Square(1, 8));
                    kingside_rook = getPiece(new Square(8, 8));
                }

                if (queenside_rook != null &&
                   (queenside_rook.c_ == c_) &&
                   (queenside_rook.t_ == Type.ROOK) &&
                   ((Rook)queenside_rook).moved_ == false)
                {
                    Castle c = new Castle(this, queenside_rook, false);
                    if (tbd || c.check())
                        moves.Add(c);
                }

                if (kingside_rook != null &&
                   (kingside_rook.c_ == c_) &&
                   (kingside_rook.t_ == Type.ROOK) &&
                   ((Rook)kingside_rook).moved_ == false)
                {
                    Castle c = new Castle(this, kingside_rook, true);
                    if (tbd || c.check())
                        moves.Add(c);
                }
            }

            return moves;
        }

    }

    public ChessGame()
    {
        notation.Clear();
        turn = Color.WHITE;
        buildBoard();
        showBoard();
    }

    static public bool inCheck(Color c)
    {
        var copy = new List<Piece>(pieces);
        foreach (Piece piece in copy)
        {
            // if not on the same side
            if (piece.c_ != c)
            {
                bool checking = false;
                piece.getAvailableMoves(out checking, true);
                if (checking)
                    return true;
            }
        }
        return false;
    }

    static public Color otherColor(Color c)
    {
        if (c == Color.WHITE)
            return Color.BLACK;
        else
            return Color.WHITE;
    }

    static public int end()
    {
        Debug.Log("TURN" + turn.ToString());
        bool attacked = inCheck(turn);
        bool hasMoves = false;
        var copy = new List<Piece>(pieces);
        foreach (Piece piece in copy)
        {
            if (piece.c_ == turn)
            {
                List<Command> moves = piece.getAvailableMoves(out bool temp);
                if (moves.Count != 0)
                {
                    hasMoves = true;
                    break;
                }
            }
        }

        // if in checkmate
        if (attacked && hasMoves == false)
            return 1;

        // if in stalemate
        if (attacked == false && hasMoves == false)
            return 2;

        return 0;
    }

    public void move(Command c)
    {
        c.execute();
        notation.Push(c);
    }

    public void undo()
    {
        notation.Pop().undo();
    }

    public void buildBoard()
    {
        // Reset the board
        pieces.Clear();

        // White Pieces
        // Set up the back row
        pieces.Add(new Rook(Color.WHITE, new Square(1, 1)));
        pieces.Add(new Knight(Color.WHITE, new Square(2, 1)));
        pieces.Add(new Bishop(Color.WHITE, new Square(3, 1)));
        pieces.Add(new Queen(Color.WHITE, new Square(4, 1)));
        pieces.Add(new King(Color.WHITE, new Square(5, 1)));
        pieces.Add(new Bishop(Color.WHITE, new Square(6, 1)));
        pieces.Add(new Knight(Color.WHITE, new Square(7, 1)));
        pieces.Add(new Rook(Color.WHITE, new Square(8, 1)));

        // Set up the front row
        for (int col = 1; col <= 8; col++)
        {
            pieces.Add(new Pawn(Color.WHITE, new Square(col, 2)));
        }


        // Black Pieces
        // Set up the back row
        pieces.Add(new Rook(Color.BLACK, new Square(1, 8)));
        pieces.Add(new Knight(Color.BLACK, new Square(2, 8)));
        pieces.Add(new Bishop(Color.BLACK, new Square(3, 8)));
        pieces.Add(new Queen(Color.BLACK, new Square(4, 8)));
        pieces.Add(new King(Color.BLACK, new Square(5, 8)));
        pieces.Add(new Bishop(Color.BLACK, new Square(6, 8)));
        pieces.Add(new Knight(Color.BLACK, new Square(7, 8)));
        pieces.Add(new Rook(Color.BLACK, new Square(8, 8)));

        // Set up the front row
        for (int col = 1; col <= 8; col++)
        {
            pieces.Add(new Pawn(Color.BLACK, new Square(col, 7)));
        }
    }

    public void showBoard()
    {
        foreach (Piece piece in pieces)
        {
            Console.WriteLine(piece.ToString());
        }
    }
}