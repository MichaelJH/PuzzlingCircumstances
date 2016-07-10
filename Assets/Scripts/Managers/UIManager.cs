using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class UIManager : MonoBehaviour {
    //public GameObject pauseMenuPrefab;
    private GameObject pauseMenu;
    private bool isLevel;

    //So we can instantiate all in game play instead of in scene
    public GameObject pausePrefab;
    public Canvas canvasPrefab;
    public GameObject eventSystemPrefab;
    public bool restarting;

    void Start() {
        Time.timeScale = 1;
        isLevel = false;
        if (SceneManager.GetActiveScene() != SceneManager.GetSceneByName("sceneSelection")) {
            //Not the scene selection 
            isLevel = true;
            //Instantiate objects
            Camera camera = GameObject.Find("Main Camera").GetComponent<Camera>();
            Canvas canvas = Instantiate(canvasPrefab);
            Instantiate(eventSystemPrefab);
            pauseMenu = Instantiate(pausePrefab);

            //Set canvas rendering to be in terms of main camera
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 4;

            //set the pause panel to be a child of the canvas
            pauseMenu.transform.SetParent(canvas.transform, false);
            //Center the pause menu
            pauseMenu.transform.position = canvas.transform.position;
        }
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Escape) && isLevel && !restarting) {
            pauseMenu.GetComponent<pauseScript>().pause = true;
            pauseMenu.SetActive(true);
        }
    }

    public void LoadScene(int level) {
        SceneManager.LoadScene(level);
    }
}
