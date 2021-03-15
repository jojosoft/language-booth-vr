using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple component encapsulating the eye tracking calibration into step.
/// </summary>
public class Calibration : MonoBehaviour
{
	void Awake()
	{
		EyeTracker.RunCalibration();
	}
}
