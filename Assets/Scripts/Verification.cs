using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class Verification : MonoBehaviour
{
	public EyeTracker eyeTracker;

	public Transform crosshair;
	public Transform indicator;

	[Tooltip("Local target positions for the crosshair in order. Use the last component for movement time in seconds.")]
	public Vector4[] movementPath;
	[Tooltip("Only scales the (more difficult to manually estimate) x-values and y-values.")]
	public float pathScale = 1.0f;

	public bool alwaysShowIndicator = false;

	private float lastCrosshairMovement = 0.0f;

	IEnumerator Start()
	{
		// Only visualize the relative gaze point when debugging or specified.
		if (Debug.isDebugBuild || alwaysShowIndicator)
		{
			indicator.gameObject.SetActive(true);
		}
		// Set up the separate logger for the verification data.
		Logger.RegisterField("hitX");
		Logger.RegisterField("hitY");
		Logger.RegisterField("deltaMovement");
		Logger.RegisterField("positionOffset");
		Logger.RegisterField("depthOffset");
		Logger.Begin();
		// Make the crosshair move along a fixed path.
		foreach (Vector4 pos in movementPath)
		{
			yield return MoveCrosshair(new Vector3(pos.x * pathScale, pos.y * pathScale, pos.z), pos.w);
		}
		Logger.End();
		// Move the file to the main output directory and rename it in one go.
		string filePath = Logger.GetFilePath();
		string fileName = Path.GetFileName(filePath);
		string parentDir = Path.Combine(Directory.GetParent(filePath).FullName, "..");
		Debug.Log(Path.Combine(parentDir, fileName.Split('-')[0] + "-EyeTrackingVerification.txt"));
		File.Move(filePath, Path.Combine(parentDir, fileName.Split('-')[0] + "-EyeTrackingVerification.txt"));
		// After the movement finished, tell the user to wink to proceed.
		crosshair.gameObject.SetActive(false);
		indicator.gameObject.SetActive(false);
		// Not rendering this text until now has implicitly prevented winks to be accepted by the main procedure!
		this.transform.GetChild(0).GetComponent<MeshRenderer>().enabled = true;
	}

	void Update()
	{
		RaycastHit hit;
		if (Physics.Raycast(eyeTracker.GetRay(), out hit, 5.0f, LayerMask.GetMask("UI")))
		{
			Vector3 localHit = crosshair.worldToLocalMatrix.MultiplyPoint(hit.point);
			if (Logger.IsLogging())
			{
				Logger.UpdateField("hitX", localHit.x.ToString());
				Logger.UpdateField("hitY", localHit.y.ToString());
			}
			// Update the position of the indicator using the calculated relative offset.
			if (indicator.gameObject.activeSelf)
			{
				Vector3 iPos = indicator.localPosition;
				iPos.x = localHit.x;
				iPos.y = localHit.y;
				indicator.localPosition = iPos;
			}
		}
		// Log movement data about the crosshair independently.
		if (Logger.IsLogging())
		{
			Logger.UpdateField("deltaMovement", lastCrosshairMovement.ToString());
			float positionOffset = new Vector2(crosshair.localPosition.x, crosshair.localPosition.y).magnitude;
			Logger.UpdateField("positionOffset", positionOffset.ToString());
			Logger.UpdateField("depthOffset", crosshair.localPosition.z.ToString());
		}
	}

	private IEnumerator MoveCrosshair(Vector3 worldPos, float time)
	{
		if (time > 0)
		{
			float startTime = Time.time;
			Vector3 startPos = this.crosshair.localPosition;
			// Offer the opportunity to animate the movement using coroutines.
			yield return new WaitUntil(() => {
				float interpolation = Mathf.SmoothStep(0, 1, (Time.time - startTime) / time);
				Vector3 newPos = Vector3.Lerp(startPos, worldPos, interpolation);
				this.lastCrosshairMovement = Vector3.Distance(this.crosshair.localPosition, newPos);
				this.crosshair.localPosition = newPos;
				return interpolation >= 1;
			});
		}
		else
		{
			this.crosshair.localPosition = worldPos;
		}
	}

	private Logger Logger
	{
		get
		{
			return GetComponent<Logger>();
		}
	}
}
