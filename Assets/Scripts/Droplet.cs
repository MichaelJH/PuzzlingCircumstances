using UnityEngine;
using System.Collections;

public class Droplet : MonoBehaviour {

    private bool dying = false;

	public void Kill() {
        if (!dying) {
            dying = true;
            StartCoroutine(Dying());
        }
    }

    IEnumerator Dying() {
        yield return new WaitForSeconds(0.2f);
        Destroy(gameObject);
    }
}
