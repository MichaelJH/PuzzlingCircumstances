using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class roomManagement : MonoBehaviour {
    public int gravityDirection = 0; // 0 = down, 1 = up, 2 = right, 3 = left
    public float gravityIntensity = 0.02f;

    // variables for fade GUI texture
    public float fadeSpeed = 0.6f;
    public Texture2D fadeTexture;
    private int drawDepth = -100;
    private float alpha = 1.0f;
    private int fadeDir = -1;

    private IEnumerator blurSequence;

    void Awake() {
        // set gravity settings for the player
        GameObject player = GameObject.Find("Player");
        var gravityScript = player.GetComponent<gravityHandler>();
        if (gravityDirection == 0) {
            gravityScript.Init(gravityHandler.GravityOrientation.Down, gravityIntensity);
        } else if (gravityDirection == 1) {
            gravityScript.Init(gravityHandler.GravityOrientation.Up, gravityIntensity);
        } else if(gravityDirection == 2) {
            gravityScript.Init(gravityHandler.GravityOrientation.Right, gravityIntensity);
        } else {
            gravityScript.Init(gravityHandler.GravityOrientation.Left, gravityIntensity);
        }
        // gravity for boxes
        Physics2D.gravity = new Vector2(0, -50);

        // get layers to ignore
        int _boxMask = LayerMask.NameToLayer("Box");
        int _playerMask = LayerMask.NameToLayer("Player");
        int _laserMask = LayerMask.NameToLayer("Deadly");
        int _lightMask = LayerMask.NameToLayer("PortalLight");
        int _doorMask = LayerMask.NameToLayer("Door");
        int _entranceMask = LayerMask.NameToLayer("Entrance");
        // ignore collisions between boxes and players and lasers and portals and doors
        Physics2D.IgnoreLayerCollision(_boxMask, _playerMask);
        Physics2D.IgnoreLayerCollision(_boxMask, _laserMask);
        Physics2D.IgnoreLayerCollision(_boxMask, _lightMask);
        Physics2D.IgnoreLayerCollision(_boxMask, _doorMask);
        Physics2D.IgnoreLayerCollision(_boxMask, _entranceMask);

    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.Backspace)) {
            RestartLevel();
        }
        if (Input.GetKeyDown(KeyCode.Alpha0)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        }
        if (Input.GetKeyDown(KeyCode.Alpha9)) {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex - 1);
        }
        //if (Input.GetKeyDown(KeyCode.Escape)) {
        //    Application.Quit();
        //}
    }

    void OnGUI() {
        alpha += fadeDir * fadeSpeed * Time.deltaTime;
        // clamp alpha between 0 and 1
        alpha = Mathf.Clamp01(alpha);

        // set the color of the GUI (only changing the alpha)
        GUI.color = new Color(0, 0, 0, alpha);
        GUI.depth = drawDepth;

        // draw the GUI over the screeen
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), fadeTexture);
    }

    void OnLevelWasLoaded() {
        alpha = 1;
        BeginFade(-1);
        var uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
        uiManager.restarting = false;
    }

    public float BeginFade(int direction) {
        fadeDir = direction;
        return fadeSpeed;
    }

    public void RestartLevel() {
        if (blurSequence == null) {
            var uiManager = GameObject.Find("UIManager").GetComponent<UIManager>();
            uiManager.restarting = true;
            blurSequence = blurTransition();
            var playerScript = GameObject.Find("Player").GetComponent<playerController>();
            playerScript.move = false;
            StartCoroutine(blurSequence);
        }
    }

    IEnumerator blurTransition() {
        GameObject cam = GameObject.Find("Main Camera");
        GameObject[] lights = GameObject.FindGameObjectsWithTag("RoomLight");
        var blur = cam.GetComponent<Blur>();
        blur.enabled = true;
        blur.blurSize = 0;
        blur.downsample = 2;
        Color lightColor = lights[0].GetComponent<DynamicLight>().LightColor;
        float lightColFac = 0.02f;
        float lightFac = 0.2f;

        while (blur.blurIterations < 4) {
            blur.blurIterations++;
            yield return new WaitForSeconds(0.05f);
        }
        while (blur.blurSize < 15f) {
            lightColor = new Color(lightColor.r + lightColFac, lightColor.g + lightColFac, lightColor.b + lightColFac, lightColor.a);
            foreach(GameObject go in lights) {
                go.GetComponent<DynamicLight>().LightColor = lightColor;
                go.GetComponentInChildren<Light>().intensity += lightFac;
            }
            blur.blurSize += 0.2f;
            yield return new WaitForSeconds(0.01f);
        }
        BeginFade(1);
        yield return new WaitForSeconds(1 / fadeSpeed);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
