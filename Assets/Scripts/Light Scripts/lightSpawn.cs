using UnityEngine;
using System.Collections;

public class lightSpawn : MonoBehaviour {

    public GameObject lightPrefab;
    private GameObject myLight;

	// Use this for initialization
	void Start () {
        myLight = Instantiate(lightPrefab);
        myLight.transform.position = transform.position;
	}
}
