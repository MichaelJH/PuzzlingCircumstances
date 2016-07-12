using UnityEngine;
using System.Collections;

public class WaterDetector : MonoBehaviour {
    // place on objects that need to detect water collisions
	
    void OnTriggerEnter2D(Collider2D coll) {
        if (coll.gameObject.tag == "Box") {
            Rigidbody2D rb2d = coll.GetComponent<Rigidbody2D>();
            coll.GetComponent<boxScript>().underwater = true;
            float veloc = rb2d.velocity.y * rb2d.mass / 40f;
            float maxDisturb = 0.3f;
            if (Mathf.Abs(veloc) > maxDisturb)
                veloc = maxDisturb * Mathf.Sign(veloc);
            transform.parent.GetComponent<WaterBehaviour>().Splash(transform.position.x, veloc);
        }
        else if (coll.gameObject.tag == "Droplet") {
            Rigidbody2D rb2d = coll.GetComponent<Rigidbody2D>();
            transform.parent.GetComponent<WaterBehaviour>().Splash(transform.position.x, rb2d.velocity.y * rb2d.mass / 40f);
            if (coll.gameObject.activeSelf)
                coll.gameObject.GetComponent<Droplet>().Kill();
        }
    }

    public void PlayerCollide(float xPos, float yVeloc) {
        transform.parent.GetComponent<WaterBehaviour>().Splash(xPos, yVeloc);
    }
}
