using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class levelSelect : MonoBehaviour {
    private int selectedScene; //Keep track of which scene is currently selected
    private bool animate;
    private GameObject door;
    private int scenePaused;
    private Image defaultImage;
    public int numScenes;

    private Color selectedColor;

    //For scrolling
    public ScrollRect scrollRect;
    public RectTransform contentPanel;
    private Vector2 origin;
    private int section;

    // Use this for initialization
    void Start () {
        defaultImage = GameObject.Find("Image").GetComponent<Image>();
        door = GameObject.Find("Door");
        var tracker = GameObject.Find("Tracker").GetComponent<tracker>();
        scenePaused = tracker.lastScene;
        if (scenePaused == 0) {
            selectedScene = 1;
            SelectScene();
        }
        origin = contentPanel.anchoredPosition;
    }

    void Update () {
        // >>> Can remove this and just make Clicked() call ExitAnimation
        if (animate == true) {
            ExitAnimation();
        } // <<<
        if (scenePaused != 0) {
            selectedScene = scenePaused;
            SelectScene();
        }

        //Navigate with arrow keys
        if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S)) {
            UnSelectScene();
            selectedScene = MyMod(selectedScene, numScenes)+1;
            SelectScene();
        } else if (Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W)) {
            UnSelectScene();
            if (selectedScene - 1 == 0) {
                selectedScene = numScenes;
            } else {
                selectedScene = MyMod(selectedScene - 1, numScenes);
            }
            SelectScene();
        } else if (Input.GetKeyDown(KeyCode.Return)) {
            Clicked();
        }
    }
	
    public void Selected(int sceneNum) {
        //Change previously selected to original color
        UnSelectScene();
        //new scene
        selectedScene = sceneNum;
        SelectScene();
    }

    public void Clicked() {
        animate = true;
        door.GetComponent<doorScript>().RaiseGate();
    }

    private void ExitAnimation() {
        GameObject player = GameObject.Find("Player");
        var playerScript = player.GetComponent<playerController>();

        //Make player move towards door
        float exitSpeed = playerScript.GetPlayerTopSpeed() * 30;
        Vector2 start = player.transform.position;
        Vector2 end = door.transform.position;
        end.y = start.y;

        Vector2 newPos = Vector2.MoveTowards(start, end, exitSpeed * Time.deltaTime);
        player.transform.position = newPos;

        door.GetComponent<doorScript>().TransitionRoomCall(selectedScene);
    }

    private void SelectScene() {
        var level = GameObject.Find("Level" + selectedScene);
        string sceneName = "Room" + selectedScene;
        
        if (level != null) {
            selectedColor = level.GetComponentInChildren<Text>().color;
            level.GetComponentInChildren<Text>().color = Color.white;
            defaultImage.sprite = Resources.Load<Sprite>(sceneName);
        }

        SnapTo(level.GetComponent<RectTransform>());
        scenePaused = 0;
    }

    private void UnSelectScene() {
        GameObject level;
        level = GameObject.Find("Level" + selectedScene);

        if (scenePaused != 0) {
            level = GameObject.Find("Level" + scenePaused);
        }

        level.GetComponentInChildren<Text>().color = selectedColor;

        //Scroll focus
        RectTransform target = level.GetComponent<RectTransform>();
        Vector2 move = (Vector2)scrollRect.transform.InverseTransformPoint(contentPanel.position)
                            - (Vector2)scrollRect.transform.InverseTransformPoint(target.position);

        section = detectSection(move); //last button's section
    }

    //Heavily modified from user maksymiuk on StackOverflow 
    //at http://stackoverflow.com/questions/30766020/how-to-scroll-to-a-specific-element-in-scrollrect-with-unity-ui
    private void SnapTo(RectTransform target) {
        Vector2 sizeOfRect = scrollRect.GetComponent<RectTransform>().sizeDelta;
        //Vector2 sizeOfContent = contentPanel.GetComponent<RectTransform>().sizeDelta;
        float offset = sizeOfRect.y / 2.5f;

        Canvas.ForceUpdateCanvases();

        Vector2 move = (Vector2)scrollRect.transform.InverseTransformPoint(contentPanel.position)
                            - (Vector2)scrollRect.transform.InverseTransformPoint(target.position);

        
        int newSection = detectSection(move);

        Vector2 newPos = new Vector2(0, 0);
        if (newSection != section) { //Only update when buttons are in a new section
            switch (newSection) {
                case 1:
                    newPos = origin;
                    break;
                case 2:
                    newPos = new Vector2(contentPanel.anchoredPosition.x, origin.y + offset);
                    break;
                case 3:
                    newPos = new Vector2(contentPanel.anchoredPosition.x, origin.y + 2 * offset);
                    break;
                case 4:
                    newPos = new Vector2(contentPanel.anchoredPosition.x, origin.y + 3 * offset);
                    break;
            }

            contentPanel.anchoredPosition = newPos;
            section = newSection;
        }
            
        }

    private int detectSection(Vector2 move) {
        Vector2 sizeOfRect = scrollRect.GetComponent<RectTransform>().sizeDelta;
        Vector2 sizeOfContent = contentPanel.GetComponent<RectTransform>().sizeDelta;
        float halfView = sizeOfRect.y / 2.0f;

        int newSection = 0;

        if (move.y < halfView) {
            newSection = 1;
        } else if (move.y < sizeOfRect.y) {
            newSection = 2;
        } else if (move.y < (sizeOfRect.y + halfView)) {
            newSection = 3;
        } else if ((move.y < sizeOfContent.y)) { //just go straight to the last portion
            newSection = 4;
        }

        return newSection;
    }

    private int MyMod(int a, int b) {
        int rem = a % b;
        if (rem < 0) {
            rem = b + rem;
        }
        return rem;
    }
}
