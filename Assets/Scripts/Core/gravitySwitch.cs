using UnityEngine;
using System.Collections;

public class gravitySwitch : MonoBehaviour {

    public bool allowLeftGravity;
    public bool allowRightGravity;
    public bool allowDownGravity;
    public bool allowUpGravity;

    public bool counterClockwise;

    private gravityHandler.GravityOrientation currOrientation;
    private gravityHandler gravityScript;
    private GameObject player;
    private gravityHandler.GravityOrientation[] orientations;
    private int orIndex;

    // Use this for initialization
    void Start() {
        player = GameObject.Find("Player");
        gravityScript = player.GetComponent<gravityHandler>();
        //Set switch in the right direction for initial gravity
        currOrientation = gravityScript.GetGravityOrientation();
        SetIndexReference(currOrientation);
        transform.rotation = SetSwitchRotation(currOrientation);

        //Array of all orientations
        orientations = new gravityHandler.GravityOrientation[] {
            gravityHandler.GravityOrientation.Up,
            gravityHandler.GravityOrientation.Right,
            gravityHandler.GravityOrientation.Down,
            gravityHandler.GravityOrientation.Left };

    }

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("Fire1") && player.GetComponent<playerController>().move) {
            //Shoot raycast to detect if the player shot at the gravity switch
            int platform = LayerMask.GetMask("Platform", "PortalPlatform"); //the switch is on the platform layer
            Vector2 playerPos = player.transform.position;
            Vector2 target = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2 direction = target - playerPos;

            RaycastHit2D hit = Physics2D.Raycast(playerPos, direction, Mathf.Infinity, platform);

            if (hit && (hit.collider.name == "gravityBox" || hit.collider.name == "gravityBox - Ilum")) {
                currOrientation = gravityScript.GetGravityOrientation();
                UpdateOrientation();
            }
        }
    }

    void UpdateOrientation() {
        int count = 0;
        if (!counterClockwise) {
            count += 1;
            while (!ValidOrientation(MyMod((orIndex + count), 4)) && (count < orientations.Length)) {
                count += 1;
            }
        } else {
            count -= 1;
            while (!ValidOrientation(MyMod((orIndex + count), 4)) && (count > -orientations.Length)) {
                count -= 1;
            }
        }

        orIndex = MyMod(orIndex+count, 4);

        gravityScript.SetGravityOrientation(orientations[orIndex]);
        transform.rotation = SetSwitchRotation(orientations[orIndex]);
    }

    Quaternion SetSwitchRotation(gravityHandler.GravityOrientation Or) {
        Quaternion rot = transform.rotation;
        if (Or == gravityHandler.GravityOrientation.Up) {
            rot = Quaternion.Euler(0, 0, 0);
        } else if (Or == gravityHandler.GravityOrientation.Right) {
            rot = Quaternion.Euler(0, 0, 270);
        } else if (Or == gravityHandler.GravityOrientation.Down) {
            rot = Quaternion.Euler(0, 0, 180);
        } else if (Or == gravityHandler.GravityOrientation.Left) {
            rot = Quaternion.Euler(0, 0, 90);
        }
        return rot;
    }

    void SetIndexReference(gravityHandler.GravityOrientation Or) {
        if (Or == gravityHandler.GravityOrientation.Up) {
            orIndex = 0;
        } else if (Or == gravityHandler.GravityOrientation.Right) {
            orIndex = 1;
        } else if (Or == gravityHandler.GravityOrientation.Down) {
            orIndex = 2;
        } else if (Or == gravityHandler.GravityOrientation.Left) {
            orIndex = 3;
        }
    }

    bool ValidOrientation(int i) {
        bool valid = true;

        if ((!allowUpGravity && (i == 0)) || (!allowRightGravity && (i == 1)) || (!allowDownGravity && (i == 2)) || (!allowLeftGravity && (i == 3))) {
            valid = false;
        }
        return valid;
    }

    int MyMod (int a, int b) {
        int rem = a % b;
        if (rem < 0) {
            rem = b + rem;
        }
        return rem;
    }

}
