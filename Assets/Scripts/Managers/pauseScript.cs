using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class pauseScript : MonoBehaviour {
    public bool pause;
    private GameObject player;
    private GameObject selectCanvas;

	// Use this for initialization
	void Start () {
        gameObject.SetActive(false);
        player = GameObject.Find("Player");
        pause = false;
        if (SceneManager.GetActiveScene() == SceneManager.GetSceneByName("listSceneSelection")) {
            selectCanvas = GameObject.Find("SceneSelectCanvas");
        }
    }

    void Update() {
        if (pause) {
            Time.timeScale = 0;  //pause game
            player.GetComponent<playerController>().move = false;
            player.GetComponent<PortalScript>().paused = true;

            if (selectCanvas != null && selectCanvas.activeSelf) {
                selectCanvas.SetActive(false);
            }
        }
    }

    public void ContinueGame() {
        gameObject.SetActive(false);
        Time.timeScale = 1;
        pause = false;
        if (SceneManager.GetActiveScene().buildIndex != 0) {
            player.GetComponent<playerController>().move = true;
            player.GetComponent<PortalScript>().paused = false;
        }

        //select level case
        if (selectCanvas != null) {
            selectCanvas.SetActive(true);
        }
    }

    public void RestartLevel() {
        Time.timeScale = 1;
        pause = false;
        gameObject.SetActive(false);
        //don't allow pausing
        var uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        uiManager.restarting = true;
        //call restart 
        var manager = GameObject.Find("roomManager").GetComponent<roomManagement>();
        manager.RestartLevel();
    }

    public void ExitGame() {
        Time.timeScale = 1;
        pause = false;
        SceneManager.LoadScene("endGame");
        Application.Quit();
    }

    public void SelectLevel() {
        Time.timeScale = 1;
        pause = false;
        var tracker = GameObject.Find("Tracker");
        if (tracker != null) {
            var trackerScript = tracker.GetComponent<tracker>();
            trackerScript.lastScene = SceneManager.GetActiveScene().buildIndex;
        }
        SceneManager.LoadScene("listSceneSelection");
    }
}
