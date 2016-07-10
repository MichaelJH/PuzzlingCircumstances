using UnityEngine;
using System.Collections;

public class buttonScript: MonoBehaviour {
    public float buttonDuration;
    private float buttonPressDistance;
    private float startTime;
    private float timeRemaining;
    private Vector3 initialPosition;
    
    private GameObject doorObject;
    public GameObject buttonLightPrefab;
    private GameObject buttonLight;
    private GameObject player;
    private TextMesh timerGUI;

    private bool timerRunning = false;
    private bool buttonPressed = false;

    // This is the coroutine used for timing the button
    private IEnumerator timer;

	// Use this for initialization
	void Awake () {
        initialPosition = transform.position;
        buttonPressDistance = .275f;
        doorObject = GameObject.Find("Door");
        buttonLight = GameObject.Instantiate(buttonLightPrefab);
        buttonLight.transform.position = transform.position;
        buttonLight.transform.rotation = transform.rotation;
        if (doorObject == null)
            Debug.Log("No Door found by button."); // keep this debug line for error checking
        player = GameObject.Find("Player");

        timerGUI = GameObject.Find("Timer").GetComponent<TextMesh>();
        timeRemaining = 0;
    }
	
    void Update() {
        if (player.GetComponent<playerCollisions>().enteredButton) {
            ButtonPushed();
        } else if (player.GetComponent<playerCollisions>().exitedButton) {
            JumpedOffButton();
        }

        if (timerRunning) {
            timeRemaining -= Time.deltaTime;
            if (timeRemaining <= 0) {
                timeRemaining = 0;
                timerRunning = false;
            }
            timerGUI.text = timeRemaining.ToString("F2");
        }
    }

	private void TimeUp () {
        transform.position = initialPosition;
        var doorScript = doorObject.GetComponent<doorScript>();
        doorScript.locked = true;
        if (doorScript.open)
            doorScript.LowerGate();
        else
            doorScript.ChangeLight("red");

        // change the color of both the button light and the door light
        Color redLight = new Color(1f, 0, 0, 0);
        var lightScript = buttonLight.GetComponent<DynamicLight>();
        var light = buttonLight.GetComponentInChildren<Light>();
        lightScript.LightColor = redLight;
        light.color = redLight;
        light.intensity = 8f;
    }

    void ButtonPushed() {
        Vector3 up = transform.up;
        Vector3 newPosition = initialPosition;

        if (Mathf.Round(up.x) == 1.0) { // right facing button
            newPosition.x -= buttonPressDistance;
        } else if (Mathf.Round(up.x) == -1) { //left facing
            newPosition.x += buttonPressDistance;
        } else if (Mathf.Round(up.y) == 1) { //facing up
            newPosition.y -= buttonPressDistance;
        } else if (Mathf.Round(up.y) == -1) { //facing down
            newPosition.y += buttonPressDistance;
        }
        transform.position = newPosition;


        var doorScript = doorObject.GetComponent<doorScript>();
        if (doorScript.locked) {
            doorScript.locked = false;
            doorScript.ChangeLight("green");
        }
        

        // change the button light to be green
        Color greenLight = new Color(0.2f, 1f, 0, 0);
        var lightScript = buttonLight.GetComponent<DynamicLight>();
        var light = buttonLight.GetComponentInChildren<Light>();
        lightScript.LightColor = greenLight;
        light.color = greenLight;
        light.intensity = 3f;

        buttonPressed = true;
        player.GetComponent<playerCollisions>().enteredButton = false;

        timeRemaining = buttonDuration;
        timerGUI.text = timeRemaining.ToString("F2");
        timerRunning = false;
    }

    void JumpedOffButton() {
        if (timer != null)
            StopCoroutine(timer);
        timer = ButtonTimer(buttonDuration);
        StartCoroutine(timer);
        buttonPressed = false;
        timerRunning = true;
        player.GetComponent<playerCollisions>().exitedButton = false;
    }

    void OnCollisionStay2D(Collision2D coll) {
        if (coll.gameObject.tag == "Box") {
            ButtonPushed();
        }
    }

    void OnCollisionExit2D(Collision2D coll) {
        if (coll.gameObject.tag == "Box" && player.GetComponent<playerCollisions>().pushedButton == false) {

            JumpedOffButton();
        }
    }

    IEnumerator ButtonTimer(float duration) {
        if (duration == 0) {
            duration = 0.1f;
        }
        yield return new WaitForSeconds(duration);
        if (!buttonPressed)
            TimeUp();
    }
}
