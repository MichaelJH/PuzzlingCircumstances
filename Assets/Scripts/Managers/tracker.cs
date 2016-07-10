using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;

public class tracker : MonoBehaviour {
    public int lastScene;
    // Use this for initialization
    void Awake () {
        DontDestroyOnLoad(transform.gameObject);
        lastScene = 0;
	}

}
