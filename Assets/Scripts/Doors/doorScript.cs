using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class doorScript : MonoBehaviour {
    private bool completedLevel;
    private GameObject playerObject;
    private Transform frameTrans;
    private Transform gateTrans;
	private GameObject doorLight;
	public GameObject lightPrefab;

    private int gateHeight;
    public bool locked;
    public bool invert;
    public bool open;

    // Use this for initialization
    void Start() {
        completedLevel = false;
        //locked = true; //lock the door
        playerObject = GameObject.Find("Player");
		doorLight = GameObject.Instantiate(lightPrefab);

        //Get door frame's transform
        Transform[] childrenTrans = GetComponentsInChildren<Transform>();
        gateHeight = 0;

        foreach (Transform obj in childrenTrans) {
            if (obj.parent != null) {
                gateTrans = obj;
            }
        }

        if (invert) {
            doorLight.transform.position = transform.position + transform.right * 3;
        } else {
            doorLight.transform.position = transform.position + -transform.right * 3;
        }

        open = false;
        if (!locked) {
            ChangeLight("green");
        }
    }
	
	// Update is called once per frame
	void Update () {
        /*if (Input.GetKeyDown(KeyCode.E) && !locked && !completedLevel && player.GetComponent<playerCollisions>().inDoor) {
            completedLevel = true;
        }*/
        if (completedLevel) {
            // if the player is in the air, wait for them to land
            var collisionScript = playerObject.GetComponent<playerCollisions>();
            if (!collisionScript.onGround) {
                return;
            }

            // stop the player from moving and stop casting shadows
            var playerScript = playerObject.GetComponent<playerController>();
            playerScript.SetPlayerState(playerController.State.Inactive);

            // set the exit speed (used in moveTowards)
            float exitSpeed = playerScript.GetPlayerTopSpeed() * 30;

            // find the starting and ending points of the moveTowards
            // TODO: if this math is moved to Start(), only have to calculate end position once
            Vector2 start = playerObject.transform.position;
            Vector2 end = transform.position;
            if (transform.up == new Vector3(0, -1, 0) || transform.up == new Vector3(0, 1, 0)) {
                end.y = start.y;
            } else {
                end.x = start.x;
            }

            // move towards the center of the door
            Vector2 newPos = Vector2.MoveTowards(start, end, exitSpeed * Time.deltaTime);

            playerObject.transform.position = newPos;

            TransitionRoomCall();
        }
	}

    public void ExitRoom() {
        if (!completedLevel)
            completedLevel = true;
    }

    public void TransitionRoomCall(int next = 1) {
        StartCoroutine(TransitionRoom(next));
    }

    public void RaiseGate() {
        if (!locked) {
            open = true;
            StartCoroutine(RaiseGate(0.01f));
            ChangeLight("green");
        }
    }

    public void LowerGate() {
        open = false;
        StartCoroutine(LowerGate(0.01f));
        if (locked)
		    ChangeLight("red");
    }

	public void ChangeLight(string color) {
		var lightObject = doorLight.GetComponent<DynamicLight> ();
		var unityLight = doorLight.GetComponentInChildren<Light> ();
        var lightCenter = doorLight.GetComponentInChildren<SpriteRenderer>();
		if (color == "green") {
			Color greenLight = new Color (0.2f, 1f, 0, 0);
			lightObject.LightColor = greenLight;
			unityLight.color = greenLight;
			unityLight.intensity = 1f;

            Color centerGreen = new Color(0.95f, 1f, 0.9f, 1f);
            lightCenter.color = centerGreen;
		} 
		else {
			Color redLight = new Color (1f, 0, 0, 0);
			lightObject.LightColor = redLight;
			unityLight.color = redLight;
			unityLight.intensity = 5f;

            Color centerRed = new Color(1f, 0.9f, 0.9f, 1f);
            lightCenter.color = centerRed;
        }
	}

    IEnumerator RaiseGate(float duration) {
        Vector3 newPos = gateTrans.position;

        while (open && gateHeight < 16) {
            newPos += gateTrans.up / 4;
            gateTrans.position = newPos;
            gateHeight++;

            yield return GateTimer(duration);
        }
    }

    IEnumerator LowerGate(float duration) {
        Vector3 newPos = gateTrans.position;

        while (!open && gateHeight > 0) {
            newPos -= gateTrans.up / 4;
            gateTrans.position = newPos;
            gateHeight--;

            yield return GateTimer(duration);
        }
    }

    IEnumerator TransitionRoom(int next = 1) {
        // set z position of gate to cover player
        Vector3 newGatePos = gateTrans.position;
        newGatePos.z = -0.1f;
        gateTrans.position = newGatePos;
        // wait for a bit
        yield return new WaitForSeconds(0.5f);
        // close gate if not already closing from button
        if (open) {
            LowerGate();
        }
        // stop player shadows
        playerObject.layer = LayerMask.NameToLayer("Deadly"); // Deadly instead of Default so boxes don't collide
        // wait for a bit again
        yield return new WaitForSeconds(0.4f);
        // fade room
        var manager = GameObject.Find("roomManager").GetComponent<roomManagement>();
        float fadeSpeed = manager.BeginFade(1);
        yield return new WaitForSeconds(1 / fadeSpeed);
        // transition to next room
        int currentScene = SceneManager.GetActiveScene().buildIndex;

        if (currentScene < SceneManager.sceneCountInBuildSettings - 1) {
            SceneManager.LoadScene(currentScene + next);
        } else
            SceneManager.LoadScene(0);
    }

    IEnumerator GateTimer(float duration) {
        yield return new WaitForSeconds(duration);
    }
}
