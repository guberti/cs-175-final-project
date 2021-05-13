using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WinTextManager : MonoBehaviour
{
	public GameObject fadePanel;
	public GameObject whiteWins;
	public GameObject blackWins;
	public GameObject drawGame;
	private CanvasGroup canvasGroup;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    // true = white win, false = black win
    public void DisplayWinText(bool whiteWon, bool drawnGame) {
        canvasGroup.blocksRaycasts = false;

        if (drawnGame) {
        	Destroy(whiteWins);
        	Destroy(blackWins);
        } else if (whiteWon) {
    		Destroy(blackWins);
    		Destroy(drawGame);
    	} else {
    		Destroy(whiteWins);
    		Destroy(drawGame);
    	}
        StartCoroutine(SmoothFade());

    }

    private static float fadeCurve(float input) {
    	return 1 - Mathf.Exp(-0.5f * input);
    }

    // Fade will never fully complete
    IEnumerator SmoothFade() {
        float currentTime = 0;
        while(true) {
        	canvasGroup.alpha = fadeCurve(currentTime);
            currentTime += Time.deltaTime;
            yield return null;
        }
    }
}
