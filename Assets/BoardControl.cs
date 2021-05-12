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

	public Vector3 getCenter() {
		return new Vector3((rank - 3.5f) * SQUARE_WIDTH, 0.001f, (file - 3.5f) * SQUARE_WIDTH);
	}
}

public class BoardControl : MonoBehaviour
{

	public GameObject moveHighlightPrefab;

	private List<GameObject> tileHighlights;

	private bool isSquareSelected;
	private Square selectedSquare;
	//private ChessGame gameManager;

    // Start is called before the first frame update
    void Start()
    {
    	Debug.Log("Running start!");
    	isSquareSelected = false;
    	moveHighlightPrefab = GameObject.Find("SelectedSquare");
    	tileHighlights = new List<GameObject>();
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

    private void ProcessClickedSquare(Square clicked) {
    	if (!isSquareSelected) {
    		selectedSquare = clicked;
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
    	}
    }

    private void RemoveHighlights() {

    }
}
