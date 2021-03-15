using UnityEngine;
using System.Collections;

public class RealtimeReflection1 : MonoBehaviour {

	ReflectionProbe probe;

	void Awake() {
		probe = GetComponent<ReflectionProbe>();
	}

	void Update () {
		probe.transform.position = new Vector3(
			Camera.main.transform.position.x, 
			Camera.main.transform.position.y, 
			-Camera.main.transform.position.z
		);

		probe.RenderProbe();
	}
}