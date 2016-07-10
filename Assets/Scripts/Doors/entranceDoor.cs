using UnityEngine;
using System.Collections;

public class entranceDoor : MonoBehaviour {

    private Transform gateTrans;
    private int gateHeight;

    void Start () {
        gateHeight = 0;
        Transform[] childrenTrans = GetComponentsInChildren<Transform>();
        foreach (Transform obj in childrenTrans) {
            if (obj.parent != null) {
                if (obj.name == "Gate")
                    gateTrans = obj;
            }
        }

        StartCoroutine(EntranceAnimation());
	}

    IEnumerator EntranceAnimation() {
        yield return Timer(0.5f);
        yield return RaiseGate(0.01f);

        // change the z position of the door to be behind the player
        Vector3 newGateTrans = gateTrans.position;
        newGateTrans.z = transform.position.z - 0.1f;
        gateTrans.position = newGateTrans;
        // allow the player to move and cast shadows
        GameObject player = GameObject.Find("Player");
        player.layer = LayerMask.NameToLayer("Player");
        var playerScript = player.GetComponent<playerController>();
        playerScript.SetPlayerState(playerController.State.Default);

        yield return Timer(0.5f);
        yield return LowerGate(0.01f);
    }

    IEnumerator RaiseGate(float duration) {
        Vector3 newPos = gateTrans.position;

        while (gateHeight < 16) {
            newPos += gateTrans.up / 4;
            gateTrans.position = newPos;
            gateHeight++;

            yield return Timer(duration);
        }
    }

    IEnumerator LowerGate(float duration) {
        Vector3 newPos = gateTrans.position;

        while (gateHeight > 0) {
            newPos -= gateTrans.up / 4;
            gateTrans.position = newPos;
            gateHeight--;

            yield return Timer(duration);
        }
    }

    IEnumerator Timer(float duration) {
        yield return new WaitForSeconds(duration);
    }
}
