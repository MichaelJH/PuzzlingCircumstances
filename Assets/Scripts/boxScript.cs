using UnityEngine;
using System.Collections;

public class boxScript : MonoBehaviour {
    private GameObject player;
    private Rigidbody2D rb2d;
    private Vector2 spawn;
    private bool fading;
    public float maxGravity = 80f;

    // angular drag
    private float normalAngDrag = 0.05f;
    private float carriedAngDrag = 5f;
    private float waterAngDrag = 3f;
    // linear drag
    //private float normalDrag = 0f;
    private float waterDrag = 10f;

    private float rotationSpeed = 8f; // speed at which a block rotates with mouse button
    private bool beingCarried; // true when player is carrying this box
    public bool underwater = false; // true when box is under water
    
    void Start() {
        player = GameObject.Find("Player");
        rb2d = GetComponent<Rigidbody2D>();
        spawn = transform.position;
        fading = false;
        beingCarried = false;
        rb2d.angularDrag = normalAngDrag;
    }

    void Update() {
        if (beingCarried) {
            if (Input.GetButton("Fire1")) {
                transform.Rotate(new Vector3(0, 0, rotationSpeed));
            } else if (Input.GetButton("Fire2")) {
                transform.Rotate(new Vector3(0, 0, -rotationSpeed));
            }
        }
        if (underwater) {
            if (rb2d.drag < 10f)
                rb2d.drag += 0.5f;
            if (rb2d.angularDrag < waterAngDrag)
                rb2d.angularDrag += 0.5f;
        }
    }

    void FixedUpdate() {
        // if carried, behave accordingly
        if (beingCarried) {
            CarryBehavior();
        }

        // cap out max speed
        Vector2 veloc = rb2d.velocity;
        if (Mathf.Abs(veloc.x) > maxGravity) {
            veloc.x = maxGravity * Mathf.Sign(veloc.x);
        } else if (Mathf.Abs(veloc.y) > maxGravity) {
            veloc.y = maxGravity * Mathf.Sign(veloc.y);
        }
        rb2d.velocity = veloc;
    }

    // public function to tell the box it's now being carried by the player
    public void StartCarrying() {
        beingCarried = true;
        rb2d.angularDrag = carriedAngDrag;
    }

    // public function to tell the box it's no longer being carried by the player
    public void StopCarrying() {
        // set a maximum on the throw speed
        float throwVelocity = 30f;
        Vector2 currVelocity = rb2d.velocity;
        if (currVelocity.magnitude > throwVelocity) {
            rb2d.velocity = currVelocity * throwVelocity / currVelocity.magnitude;
        }

        beingCarried = false;
        rb2d.angularDrag = normalAngDrag;
    }

    private void CarryBehavior() {
        float hoverDistance = 3f; // the max distance the box will hover from the player

        Vector2 mousePoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 playerPos = player.transform.position;
        Vector2 hoverDirection = mousePoint - playerPos;

        // modify the hover direction to have the magnitude of hoverDistance
        hoverDirection = hoverDirection * hoverDistance / hoverDirection.magnitude;
        // target is the point the box will be moving toward
        Vector2 target = playerPos + hoverDirection;

        // check whether the player is too far from the box
        float dropDistance = 12f;
        Vector2 distFromPlayer = (Vector2)transform.position - (Vector2)player.transform.position;

        if (distFromPlayer.magnitude > dropDistance) {
            player.GetComponent<playerController>().DropBox();
        }

        // Apply velocity to the box
        float speedFactor = 15f;
        Vector2 velocDirection = target - (Vector2)transform.position;

        velocDirection *= speedFactor;
        rb2d.velocity = velocDirection;

        // counter gravity
        rb2d.AddForce(-Physics2D.gravity);
    }

	void OnCollisionEnter2D(Collision2D coll) {
        if (coll.gameObject.tag == "Portal" && !beingCarried) {
            CollidedWithPortal(coll);
        } else if (coll.gameObject.tag == "Barrier") {
            if (!fading) {
                fading = true;
                StartCoroutine(FadeAndSpawn());
            }
        } else if (coll.gameObject.tag == "Water") {
            rb2d.angularDrag = 100;
            Debug.Log("hit water boo");
        }
    }

    void OnCollisionStay2D(Collision2D coll) {
        if (coll.gameObject.tag == "Portal" && !beingCarried) {
            CollidedWithPortal(coll);
        }
    }

    private void CollidedWithPortal(Collision2D coll) {
        var portalScript = player.GetComponent<PortalScript>();
        float offset = 1.5f;
        bool teleport = false;
        PortalScript.WallOrientation orientation = PortalScript.WallOrientation.Left;
        Vector2 newPos = Vector2.zero;
        Vector2 newVeloc = Vector2.zero;

        if (coll.gameObject == portalScript.Portal1) {
            if (portalScript.Portal2.activeSelf) {
                teleport = true;
                newPos = portalScript.PPos.p2;
                orientation = portalScript.PPos.p2Or;
            }
        } else {
            if (portalScript.Portal1.activeSelf) {
                teleport = true;
                newPos = portalScript.PPos.p1;
                orientation = portalScript.PPos.p1Or;
            }
        }

        if (teleport) {
            float speed = rb2d.velocity.magnitude;
            speed -= 1f;
            if (speed < 15f)
                speed = 15f;

            if (orientation == PortalScript.WallOrientation.Left) {
                newPos.x += offset;
                newVeloc = new Vector2(speed, 0);
            } else if (orientation == PortalScript.WallOrientation.Right) {
                newPos.x -= offset;
                newVeloc = new Vector2(-speed, 0);
            } else if (orientation == PortalScript.WallOrientation.Ceiling) {
                newPos.y -= offset;
                newVeloc = new Vector2(0, -speed);
            } else {
                newPos.y += offset;
                newVeloc = new Vector2(0, speed);
            }

            rb2d.velocity = newVeloc;
            transform.position = newPos;
        }
    }

    IEnumerator FadeAndSpawn() {
        var renderer = GetComponent<SpriteRenderer>();
        Color newColor = renderer.color;
        Color initColor = newColor;
        while (newColor.a > 0) {
            newColor.a -= 0.02f;
            renderer.color = newColor;
            yield return new WaitForSeconds(0.01f);
        }

        rb2d.velocity = Vector2.zero;
        transform.position = spawn;
        transform.rotation = Quaternion.identity;
        player.GetComponent<playerController>().SetPlayerState(playerController.State.Default);
        renderer.color = initColor;
        rb2d.isKinematic = false;
        fading = false;
    }
}
