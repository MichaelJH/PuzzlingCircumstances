using UnityEngine;
using System.Collections;

public class gravityHandler : MonoBehaviour {
    public enum GravityOrientation {
        Up,
        Right,
        Down,
        Left
    }

    private GravityOrientation gravityOr;
    private Vector2 gravityVec;
    private GameObject roomHandler;

    private float normalGravity;
    private float waterGravity;

    // set initial gravity (called by roomManager)
    public void Init (GravityOrientation initOr, float intensity) {
        gravityOr = initOr;
        SetGravityVector(intensity);
        roomHandler = GameObject.Find("roomManager");
        var roomScript = roomHandler.GetComponent<roomManagement>();
        int orient = roomScript.gravityDirection;
        if (orient == 1)
            SetGravityOrientation(GravityOrientation.Up);
        else if (orient == 2)
            SetGravityOrientation(GravityOrientation.Right);
        else if (orient == 3)
            SetGravityOrientation(GravityOrientation.Left);

        normalGravity = intensity;
        waterGravity = intensity / 4;
    }

    // other scripts call this to get the gravity vector
    public Vector2 GetGravityVector() {
        return gravityVec;
    }

    public GravityOrientation GetGravityOrientation() {
        return gravityOr;
    }

    // other scripts call this to change the gravity orientation
    public void SetGravityOrientation(GravityOrientation newOr) {
        gravityOr = newOr;
        SetGravityVector();
    }

    // other scripts call this to change the gravity intensity
    public void SetGravityIntensity(float intensity) {
        SetGravityVector(intensity);
    }

    // private function to change the gravity vector
    private void SetGravityVector(float intensity = -1) {
        if (intensity == -1) {
            intensity = gravityVec.magnitude;
        }

        // set the vector based on orientation
        if (gravityOr == GravityOrientation.Down) {
            gravityVec = new Vector2(0, -intensity);
        } else if (gravityOr == GravityOrientation.Up) {
            gravityVec = new Vector2(0, intensity);
        } else if (gravityOr == GravityOrientation.Right) {
            gravityVec = new Vector2(intensity, 0);
        } else {
            gravityVec = new Vector2(-intensity, 0);
        }
    }

    public void EnterWater() {
        SetGravityIntensity(waterGravity);
    }

    public void ExitWater() {
        SetGravityIntensity(normalGravity);
    }
}
