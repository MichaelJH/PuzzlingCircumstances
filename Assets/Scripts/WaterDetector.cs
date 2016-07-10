using UnityEngine;
using System.Collections;

public class WaterDetector : MonoBehaviour {
    // place on objects that need to detect water collisions
	
    void OnTriggerEnter2D(Collider2D coll) {
        if (coll.gameObject.tag == "Box") {
            Rigidbody2D rb2d = coll.GetComponent<Rigidbody2D>();
            coll.GetComponent<boxScript>().underwater = true;
            transform.parent.GetComponent<WaterBehaviour>().Splash(transform.position.x, rb2d.velocity.y * rb2d.mass / 40f);
        }
    }
}
