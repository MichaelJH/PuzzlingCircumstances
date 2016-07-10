using UnityEngine;
using System.Collections;

public class torchFlicker : MonoBehaviour {

    private Light pointLight;
    private float lastRange;
    private float mag = 1f;

	void Start () {
	    pointLight = GetComponent<Light>();
        lastRange = pointLight.range;

        StartCoroutine(FlickerUpdate());
	}

    IEnumerator FlickerUpdate() {

        while (true) {
            float dur = Random.Range(0.07f, 0.09f);

            yield return new WaitForSeconds(dur);

            float rnd = Random.Range(-1f, 1f) * mag;
            yield return null;
            pointLight.range = lastRange + rnd;
            yield return null;

            yield return new WaitForEndOfFrame();
        }
    }
}
