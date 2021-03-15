using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSetup : MonoBehaviour
{
	public EyeTracker eyeTracker;

	public TextMesh plus;
	public TextMesh minus;
	public GameObject indicator;

	public float stepPerSecond = 0.1f;

	void Update()
	{
		// Reset both colors first.
		plus.color = Color.white;
		minus.color = Color.white;
		RaycastHit hit;
		if (Physics.Raycast(eyeTracker.GetRay(), out hit, 5.0f, LayerMask.GetMask("UI")))
		{
			if (hit.collider.gameObject == plus.gameObject)
			{
				AdjustVolume(stepPerSecond * Time.deltaTime);
				plus.color = Color.yellow;
			}
			else if (hit.collider.gameObject == minus.gameObject)
			{
				AdjustVolume(-stepPerSecond * Time.deltaTime);
				minus.color = Color.yellow;
			}
		}
	}

	private void AdjustVolume(float difference)
	{
		AudioSource audio = this.GetComponent<AudioSource>();
		audio.volume += difference;
		// TODO: Maybe restructure transforms to avoid hard-coded offsets?
		indicator.transform.localPosition = new Vector3((audio.volume - 0.5f) * 0.3f, 0, 0.3f);
	}
}
