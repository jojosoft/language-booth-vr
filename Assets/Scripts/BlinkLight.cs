using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlinkLight : MonoBehaviour
{
	public Light lightSource;
	public GameObject bulb;
	public GameObject indicator;

	public float blinkInterval = 1.0f;

	void Start()
	{
		TurnOff();
	}

	public void TurnOn()
	{
		lightSource.enabled = true;
		indicator.SetActive(true);
		bulb.GetComponent<MeshRenderer>().material.color = Color.green;
	}

	public void TurnOff()
	{
		lightSource.enabled = false;
		indicator.SetActive(false);
		bulb.GetComponent<MeshRenderer>().material.color = Color.white;
	}

	/// <summary>
	/// Sets the light indicator to represent a scalar between -1 and 1.
	/// </summary>
	public void SetIndicator(float scalar)
	{
		if (indicator.activeSelf)
		{
			float mapped = 0.25f + 0.15f * scalar;
			indicator.transform.localScale = new Vector3(mapped, mapped, mapped);
		}
	}
}
