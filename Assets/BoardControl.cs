using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Square
{
    static float SQUARE_WIDTH = 1.5f;

    public int rank; // 0-7 corresponds to 1-8
    public int file; // 0-7 corresponds to A-H

    public Square(RaycastHit hit)
    {
        rank = (int)(hit.point.z / SQUARE_WIDTH + 4);
        file = (int)(hit.point.x / SQUARE_WIDTH + 4);
        // Debug.Log(rank);
        // Debug.Log(file);
    }

    public Square(int rank, int file)
    {
        this.rank = rank;
        this.file = file;
    }

    public Vector3 getCenter()
    {
        return new Vector3((file - 3.5f) * SQUARE_WIDTH, 0.001f, (rank - 3.5f) * SQUARE_WIDTH);
    }
}

public class PieceContainer
{
    static float MOVE_DURATION = 3f;
    static float SCALE_DURATION = 2f;

    public GameObject model;
    public Square location;

    public PieceContainer(GameObject model, Square location)
    {
        this.model = model;
        this.model.SetActive(true);
        this.model.transform.position = location.getCenter();
        this.location = location;
    }

    public void UpdateLocation(Square newLocation, MonoBehaviour monoBehavior)
    {
        location = newLocation;
        Debug.Log("new location : " + newLocation.file + ", " + newLocation.rank);
        monoBehavior.StartCoroutine(SmoothMove());
    }

    public void DoGrowAnimation(MonoBehaviour monoBehavior)
    {
        model.transform.localScale = new Vector3(0, 0, 0);
        monoBehavior.StartCoroutine(SmoothScale(1));
    }

    public void DoShrinkAnimation(MonoBehaviour monoBehavior)
    {
        model.transform.localScale = new Vector3(1, 1, 1);
        monoBehavior.StartCoroutine(SmoothScale(-1));
    }

    // Not a *real* s-curve, but a good simplification
    // Faster s-curve: f(x) = x^n/(x^n + (1-x)^n)
    private static float SCurve(float x)
    {
        return 3f * Mathf.Pow(x, 2) - 2f * Mathf.Pow(x, 3);
    }

    IEnumerator SmoothMove()
    {
        float currentTime = 0;
        Vector3 start = model.transform.position;
        Vector3 end = location.getCenter();

        while (currentTime < MOVE_DURATION)
        {
            model.transform.position = Vector3.Lerp(
                start, end, SCurve(currentTime / MOVE_DURATION)
            );
            currentTime += Time.deltaTime;
            yield return null;
        }
        model.transform.position = end;
    }

    private static float ScaleCurve(float x)
    {
        return Mathf.Pow(x, 0.25f);
    }

    // 1 grows piece from nothing, -1 shrinks piece to nothing
    IEnumerator SmoothScale(int direction)
    {
        float currentTime = 0;
        float start = (direction == 1) ? 0 : 1;
        float end = (direction == 1) ? 1 : 0;

        while (currentTime < SCALE_DURATION)
        {
            model.transform.localScale = new Vector3(1, 1, 1) * Mathf.Lerp(
                start, end, ScaleCurve(currentTime / SCALE_DURATION)
            );
            currentTime += Time.deltaTime;
            yield return null;
        }

        model.transform.localScale = new Vector3(1, 1, 1);
    }
}

public class BoardControl : MonoBehaviour
{

    // Chess piece prefabs
    public GameObject whiteKing;
    public GameObject whiteQueen;
    public GameObject whiteRook;
    public GameObject whiteBishop;
    public GameObject whiteKnight;
    public GameObject whitePawn;

    public GameObject blackKing;
    public GameObject blackQueen;
    public GameObject blackRook;
    public GameObject blackBishop;
    public GameObject blackKnight;
    public GameObject blackPawn;

    public GameObject moveHighlightPrefab;
    public GameObject canvas;
    public List<PieceContainer> pieces;

    private List<GameObject> tileHighlights;

    private bool isSquareSelected;
    private bool gameOver;
    private Square selectedSquare;
    private ChessGame gameManager;

    public PieceContainer getPieceAtSquare(Square s)
    {
        Debug.Log("Finding piece at " + s.file + "," + s.rank);
        string locs = "";
        foreach (PieceContainer piece in pieces)
        {
            locs += "\n(" + piece.location.file + "," + piece.location.rank + ")";
            if (piece.location.rank == s.rank && piece.location.file == s.file)
                return piece;
        }
        Debug.Log("locations : " + locs);
        Debug.Log("Didn't find piece");
        return null;
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Running start!");
        this.gameObject.SetActiveRecursively(false);
        this.gameObject.SetActive(true);

        isSquareSelected = false;
        gameOver = false;
        tileHighlights = new List<GameObject>();
        pieces = new List<PieceContainer>();
        gameManager = new ChessGame();

        InstantiatePieces();
    }

    void InstantiatePieces()
    {
        pieces.Add(new PieceContainer(Instantiate(whiteRook), new Square(0, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteKnight), new Square(0, 1)));
        pieces.Add(new PieceContainer(Instantiate(whiteBishop), new Square(0, 2)));
        pieces.Add(new PieceContainer(Instantiate(whiteQueen), new Square(0, 3)));
        pieces.Add(new PieceContainer(Instantiate(whiteKing), new Square(0, 4)));
        pieces.Add(new PieceContainer(Instantiate(whiteBishop), new Square(0, 5)));
        pieces.Add(new PieceContainer(Instantiate(whiteKnight), new Square(0, 6)));
        pieces.Add(new PieceContainer(Instantiate(whiteRook), new Square(0, 7)));

        pieces.Add(new PieceContainer(Instantiate(blackRook), new Square(7, 0)));
        pieces.Add(new PieceContainer(Instantiate(blackKnight), new Square(7, 1)));
        pieces.Add(new PieceContainer(Instantiate(blackBishop), new Square(7, 2)));
        pieces.Add(new PieceContainer(Instantiate(blackQueen), new Square(7, 3)));
        pieces.Add(new PieceContainer(Instantiate(blackKing), new Square(7, 4)));
        pieces.Add(new PieceContainer(Instantiate(blackBishop), new Square(7, 5)));
        pieces.Add(new PieceContainer(Instantiate(blackKnight), new Square(7, 6)));
        pieces.Add(new PieceContainer(Instantiate(blackRook), new Square(7, 7)));

        for (int i = 0; i < 8; i++)
        {
            pieces.Add(new PieceContainer(Instantiate(whitePawn), new Square(1, i)));
            pieces.Add(new PieceContainer(Instantiate(blackPawn), new Square(6, i)));
        }

        foreach (PieceContainer pieceContainer in pieces)
        {
            pieceContainer.DoGrowAnimation(this);
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (!gameOver)
        {
            CheckSelect();
        }

    }

    private void CheckSelect()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            if (
                Physics.Raycast(
                    Camera.main.ScreenPointToRay(Input.mousePosition),
                    out hit, 50f, LayerMask.GetMask("Board")
                ))
            {
                Debug.Log("Instantiating prefab!");
                Square clicked = new Square(hit);
                ProcessClickedSquare(clicked);
            }
        }
    }

    private void ProcessClickedSquare(Square clicked)
    {
        Debug.Log("Clicked square");
        if (!isSquareSelected)
        {
            Debug.Log("Update Selected");
            selectedSquare = clicked;

            // TODO check if the current active player has a piece at selectedSquare
            ChessGame.Square diego = new ChessGame.Square(clicked.file + 1, clicked.rank + 1);
            Debug.Log(diego.ToString());
            ChessGame.Piece current = ChessGame.getPiece(diego);
            if (current == null)
            {
                Debug.Log("Not a piece");
                return;
            }
            if (current.c_ != ChessGame.turn)
            {
                Debug.Log("Not your piece");
                return;
            }

            isSquareSelected = true;
            List<ChessGame.Command> validMoves = current.getAvailableMoves(out bool temp);
            // Square[] validMoves = {clicked}; // TODO add valid moves here

            foreach (ChessGame.Command move in validMoves)
            {
                // get the highlight square and convert it to board square
                ChessGame.Square highlight = move.getHighlight();
                Square boardSquare = new Square(highlight.row_ - 1, highlight.col_ - 1);

                GameObject o = Instantiate(moveHighlightPrefab);
                o.SetActive(true);
                o.transform.position = boardSquare.getCenter();
                tileHighlights.Add(o);
            }
        }
        else
        {
            isSquareSelected = false;
            foreach (GameObject highlight in tileHighlights)
            {
                Destroy(highlight);
            }

            ChessGame.Square square = new ChessGame.Square(selectedSquare.file + 1, selectedSquare.rank + 1);
            ChessGame.Piece current = ChessGame.getPiece(square);

            ChessGame.Square to = new ChessGame.Square(clicked.file + 1, clicked.rank + 1);

            ChessGame.Command chosen = null;
            List<ChessGame.Command> validMoves = current.getAvailableMoves(out bool temp);
            foreach (ChessGame.Command move in validMoves)
            {
                if (move.getHighlight().equals(to))
                {
                    chosen = move;
                    break;
                }
            }

            if (chosen != null)
            {
                switch (chosen.t_)
                {
                    case ChessGame.Command.Type.MOVE:
                        ChessGame.Move move = (ChessGame.Move)chosen;
                        Debug.Log(move.ToString());

                        ChessGame.Square oldSquare = move.start_;
                        ChessGame.Square newSquare = move.end_;

                        Square oldBoardSquare = new Square(oldSquare.row_ - 1, oldSquare.col_ - 1);
                        Square newBoardSquare = new Square(newSquare.row_ - 1, newSquare.col_ - 1);

                        PieceContainer piece = getPieceAtSquare(oldBoardSquare);
                        Debug.Assert(piece != null);
                        piece.UpdateLocation(newBoardSquare, this);

                        gameManager.move(move);
                        gameManager.showBoard();
                        break;
                    case ChessGame.Command.Type.TAKE:
                        ChessGame.Take take = (ChessGame.Take)chosen;
                        Debug.Log(take.ToString());

                        ChessGame.Square oldTakerSquare = take.start_;
                        ChessGame.Square newTakerSquare = take.end_;

                        Square oldTakerBoardSquare = new Square(oldTakerSquare.row_ - 1, oldTakerSquare.col_ - 1);
                        Square newTakerBoardSquare = new Square(newTakerSquare.row_ - 1, newTakerSquare.col_ - 1);

                        ChessGame.Square takenSquare = take.taken_.s_;
                        Square takenBoardSquare = new Square(takenSquare.row_ - 1, takenSquare.col_ - 1);

                        PieceContainer taker = getPieceAtSquare(oldTakerBoardSquare);
                        PieceContainer taken = getPieceAtSquare(takenBoardSquare);
                        Debug.Assert(taker != null);
                        taker.UpdateLocation(newTakerBoardSquare, this);

                        // remove taken
                        pieces.Remove(taken);
                        taken.DoShrinkAnimation(this);
                        Destroy(taken.model, 2f);

                        gameManager.move(take);
                        break;
                    case ChessGame.Command.Type.CASTLE:
                        ChessGame.Castle castle = (ChessGame.Castle)chosen;
                        Debug.Log(castle.ToString());

                        ChessGame.Square oldRookSquare = castle.start_rook_;
                        ChessGame.Square newRookSquare = castle.end_rook_;

                        Square oldRookBoardSquare = new Square(oldRookSquare.row_ - 1, oldRookSquare.col_ - 1);
                        Square newRookBoardSquare = new Square(newRookSquare.row_ - 1, newRookSquare.col_ - 1);

                        ChessGame.Square oldKingSquare = castle.start_king_;
                        ChessGame.Square newKingSquare = castle.end_king_;

                        Square oldKingBoardSquare = new Square(oldKingSquare.row_ - 1, oldKingSquare.col_ - 1);
                        Square newKingBoardSquare = new Square(newKingSquare.row_ - 1, newKingSquare.col_ - 1);

                        PieceContainer rook = getPieceAtSquare(oldRookBoardSquare);
                        PieceContainer king = getPieceAtSquare(oldKingBoardSquare);

                        rook.UpdateLocation(newRookBoardSquare, this);
                        king.UpdateLocation(newKingBoardSquare, this);

                        gameManager.move(castle);
                        break;
                    default:
                        Debug.LogError("Shouldn't get here");
                        break;
                }
                // rotate the camera
                StartCoroutine(SmoothCameraMove());
            }

            if (ChessGame.end() == 1)
            {
                Debug.Log("END OF GAME");
                if (ChessGame.turn == ChessGame.Color.WHITE)
                    canvas.GetComponent<WinTextManager>().DisplayWinText(true);
                else
                    canvas.GetComponent<WinTextManager>().DisplayWinText(false);
            }

            // TODO psuedocode for moving pieces
            /* 
            if (player has a piece at the selected square) {
                EditedPiece[] editedPieces = gameManager.applyMove(selectedSquare, clicked);
                foreach(EditedPiece editedPiece in editedPieces) {
                    // Piece is neither created nor destroyed
                    if (editedPiece.start.x >= 0 && editedPiece.end.x >= 0) {
                        PieceContainer pieceAsset = editedPiece.getAsset();
                        pieceAsset.UpdateLocation(editedPiece.end);

                    // Piece was destroyed
                    } else if (editedPiece.start.x > 0) {
                        PieceContainer pieceAsset = editedPiece.getAsset();
                        pieces.Remove(pieceAsset);
                        pieceAsset.DoShrinkAnimation();
                        Destroy(pieceAsset.model, 2f); // Wait two seconds for piece to be shrunk before destroying

                    // Piece was created
                    } else {
                        GameObject prefabModel = editedPiece.getPrefab();
                        pieces.Add(new PieceContainer(Instantiate(prefabModel), editedPiece.end));
                        prefabModel.DoGrowAnimation();
                    }
                }
                
                // Move camera to other side
                StartCoroutine(SmoothCameraMove());
            }

            if (game has been won) {
                gameOver = true;

                // Call this function with "TRUE" if white won, and with "FALSE" if black won
                canvas.GetComponent<WinTextManager>().DisplayWinText(true);
            }*/

            // For demo purposes only, remove when done

        }
    }


    // Not a *real* s-curve, but a good simplification
    private static float SCurve(float x)
    {
        return 3f * Mathf.Pow(x, 2) - 2f * Mathf.Pow(x, 3);
    }

    static float CAMERA_MOVE_DURATION = 3f;
    IEnumerator SmoothCameraMove()
    {
        float currentTime = 0;
        float startRotation = Camera.main.transform.eulerAngles.y;

        while (currentTime < CAMERA_MOVE_DURATION)
        {
            float rotation = startRotation + 180 * SCurve(currentTime / CAMERA_MOVE_DURATION);
            float rotationRad = Mathf.PI * rotation / 180f;
            Camera.main.transform.rotation = Quaternion.Euler(60, rotation, 0);
            Camera.main.transform.position = new Vector3(0, 10, -7 * Mathf.Cos(rotationRad));
            currentTime += Time.deltaTime;
            yield return null;
        }
    }
}
