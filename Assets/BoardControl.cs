using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct Square {
    static float SQUARE_WIDTH = 1.5f;

    int rank; // 0-7 corresponds to 1-8
    int file; // 0-7 corresponds to A-H

    public Square(RaycastHit hit) {
        rank = (int) (hit.point.x / SQUARE_WIDTH + 4);
        file = (int) (hit.point.z / SQUARE_WIDTH + 4);
        Debug.Log(rank);
        Debug.Log(file);
    }

    public Square(int rank, int file) {
        this.rank = rank;
        this.file = file;
    }

    public Vector3 getCenter() {
        return new Vector3((rank - 3.5f) * SQUARE_WIDTH, 0.001f, (file - 3.5f) * SQUARE_WIDTH);
    }
}

public struct PieceContainer {
    static float MOVE_DURATION = 3f;

    GameObject model;
    Square location;

    public PieceContainer(GameObject model, Square location) {
        this.model = model;
        this.model.SetActive(true);
        this.model.transform.position = location.getCenter();
        this.location = location;
    }

    public void UpdateLocation(Square newLocation, MonoBehaviour monoBehavior) {
        location = newLocation;
        monoBehavior.StartCoroutine(SmoothMove());
    }

    // Not a *real* s-curve, but a good simplification
    private static float SCurve(float x) {
        return 3f * Mathf.Pow(x, 2) - 2f * Mathf.Pow(x, 3);
    }

    IEnumerator SmoothMove() {
        float currentTime = 0;
        Vector3 start = model.transform.position;
        Vector3 end = location.getCenter();

        while(currentTime < MOVE_DURATION) {
            model.transform.position = Vector3.Lerp(
                start, end, SCurve(currentTime / MOVE_DURATION)
            );
            currentTime += Time.deltaTime;
            yield return null;
        }
        model.transform.position = end;
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
    public List<PieceContainer> pieces;

    private List<GameObject> tileHighlights;

    private bool isSquareSelected;
    private Square selectedSquare;
    //TODO private ChessGame gameManager;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Running start!");
        this.gameObject.SetActiveRecursively(false);
        this.gameObject.SetActive(true);

        isSquareSelected = false;
        tileHighlights = new List<GameObject>();
        pieces = new List<PieceContainer>();

        InstantiatePieces();
    }

    void InstantiatePieces() {
        pieces.Add(new PieceContainer(Instantiate(whiteRook),   new Square(0, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteKnight), new Square(1, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteBishop), new Square(2, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteQueen),  new Square(3, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteKing),   new Square(4, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteBishop), new Square(5, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteKnight), new Square(6, 0)));
        pieces.Add(new PieceContainer(Instantiate(whiteRook),   new Square(7, 0)));

        pieces.Add(new PieceContainer(Instantiate(blackRook),   new Square(0, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackKnight), new Square(1, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackBishop), new Square(2, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackQueen),  new Square(3, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackKing),   new Square(4, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackBishop), new Square(5, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackKnight), new Square(6, 7)));
        pieces.Add(new PieceContainer(Instantiate(blackRook),   new Square(7, 7)));

        for (int i = 0; i < 8; i++) {
            pieces.Add(new PieceContainer(Instantiate(whitePawn), new Square(i, 1)));
            pieces.Add(new PieceContainer(Instantiate(blackPawn), new Square(i, 6)));
        }
    }


    // Update is called once per frame
    void Update()
    {
        CheckSelect();

    }

    private void CheckSelect()
    {
        if (Input.GetMouseButtonDown(0)) {
            RaycastHit hit;
            if (
                Physics.Raycast(
                    Camera.main.ScreenPointToRay(Input.mousePosition), 
                    out hit, 50f, LayerMask.GetMask("Board")
                )) {
                Debug.Log("Instantiating prefab!");
                Square clicked = new Square(hit);
                ProcessClickedSquare(clicked);
            }
        }
    }

    /*private PieceContainer getAtCoordinate(Square coordinate) {

    }*/

    private void ProcessClickedSquare(Square clicked) {
        if (!isSquareSelected) {
            selectedSquare = clicked;

            // TODO check if the current active player has a piece at selectedSquare

            isSquareSelected = true;
            Square[] validMoves = {clicked};

            foreach (Square validMove in validMoves) {
                GameObject o = Instantiate(moveHighlightPrefab);
                o.SetActive(true);
                o.transform.position = clicked.getCenter();
                tileHighlights.Add(o);
            }
        } else {
            isSquareSelected = false;
            foreach (GameObject highlight in tileHighlights) {
                Destroy(highlight);
            }

            // TODO pseudocode for moving pieces
            /* 
            foreach ()
            */

            StartCoroutine(SmoothCameraMove());
        }
    }


    // Not a *real* s-curve, but a good simplification
    private static float SCurve(float x) {
        return 3f * Mathf.Pow(x, 2) - 2f * Mathf.Pow(x, 3);
    }

    static float CAMERA_MOVE_DURATION = 3f;
    IEnumerator SmoothCameraMove() {
        float currentTime = 0;
        Vector3 start = Camera.main.transform.position;
        float startRotation = Camera.main.transform.eulerAngles.y;

        while(currentTime < CAMERA_MOVE_DURATION) {
            float rotation = startRotation + 180 * SCurve(currentTime / CAMERA_MOVE_DURATION);
            float rotationRad = Mathf.PI * rotation / 180f;
            Camera.main.transform.rotation = Quaternion.Euler(60, rotation, 0);
            Camera.main.transform.position = new Vector3(0, 10, -7 * Mathf.Cos(rotationRad));
            currentTime += Time.deltaTime;
            yield return null;
        }
    }
}
