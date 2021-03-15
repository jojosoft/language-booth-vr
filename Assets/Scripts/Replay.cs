using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Plays back the log data of an already recorded session in real-time.
/// (Very simple implementation with hard coded column indices!)
/// </summary>
public class Replay : MonoBehaviour
{
	[Tooltip("The experiment manager that should be used for simulation.")]
	public Main main;

	public string logFilePath;

	IEnumerator Start()
	{
		if (!File.Exists(logFilePath))
		{
			try
			{
				var logs = new DirectoryInfo(FindObjectOfType<Logger>().directory).GetFiles();
				logFilePath = logs.OrderByDescending(f => f.LastWriteTime).First().FullName;
			}
			catch
			{
				Debug.Log("Replay stopped, no output files yet.");
				yield break;
			}
		}
		float startTime = Time.time;
		Debug.Log("Replaying " + logFilePath + ".");
		// Simulate what the experiment manager would do.
		main.gameObject.SetActive(false);
		StartCoroutine(DelayedRing());
		using (StreamReader log = new StreamReader(logFilePath))
		{
			string line;
			int previousClip = -1;
			main.phoneBooth.debugAudio = true;
			// Discard the first line, it contains the header.
			log.ReadLine();
			while ((line = log.ReadLine()) != null)
			{
				string[] values = line.Split('\t');
				// For correct timing, wait whenever we are ahead of time.
				while (float.Parse(values[0]) > Time.time - startTime)
				{
					yield return null;
				}
				// Head and view points are always there. (No NAs.)
				// For the rest, only "try" updating their position.
				UpdateMarker("Head", values, 3, 4, 5);
				UpdateMarker("View", values, 6, 7, 8);
				TryUpdateMarker("HitRight", values, 9, 10, 11);
				TryUpdateMarker("HitLeft", values, 12, 13, 14);
				TryUpdateMarker("Focus", values, 15, 16, 17);
				// Transfer eye openness to the y-scale values of the rings.
				if (values[18] != "NA")
				{
					transform.Find("Head").Find("RightEyeAnchor").localScale = new Vector3(1, float.Parse(values[18]), 1);
				}
				if (values[19] != "NA")
				{
					transform.Find("Head").Find("LeftEyeAnchor").localScale = new Vector3(1, float.Parse(values[19]), 1);
				}
				// Rotate the head according to the view point.
				transform.Find("Head").LookAt(transform.Find("View"));
				// Also play the correct clip when the clip ID changes.
				int currentClip;
				if (int.TryParse(values[1], out currentClip))
				{
					if (currentClip != previousClip)
					{
						StartCoroutine(main.phoneBooth.PlayClip(main.audioSamples[currentClip]));
						previousClip = currentClip;
					}
				}
			}
		}
		Debug.Log("Replay of file " + logFilePath + " ended.");
	}

	private IEnumerator DelayedRing()
	{
		// Needed to simulate the delayed start of phone ringing.
		yield return new WaitForSeconds(5);
		main.phoneBooth.StartRing();
	}

	private void TryUpdateMarker(string name, string[] values, int indexX, int indexY, int indexZ)
	{
		// Only invoke UpdateMarker if none of the contained values are undefined.
		// (This will otherwise lead to an exception when trying to parse floats.)
		if (values[indexX] != "NA" && values[indexY] != "NA" && values[indexZ] != "NA")
		{
			UpdateMarker(name, values, indexX, indexY, indexZ);
		}
	}

	private void UpdateMarker(string name, string[] values, int indexX, int indexY, int indexZ)
	{
		// Simply place the marker object at the logged position.
		transform.Find(name).position = ParseVector3(values, indexX, indexY, indexZ);
	}

	/// <summary>
	/// Parses the specified float values from a string array and returns a 3D vector.
	/// </summary>
	/// <param name="values">Main string array with float values.</param>
	/// <param name="indexX">Index of the x-value in the values array.</param>
	/// <param name="indexY">Index of the y-value in the values array.</param>
	/// <param name="indexZ">Index of the z-value in the values array.</param>
	/// <returns>Resulting 3D vector with the specified values.</returns>
	private Vector3 ParseVector3(string[] values, int indexX, int indexY, int indexZ)
	{
		return new Vector3(float.Parse(values[indexX]), float.Parse(values[indexY]), float.Parse(values[indexZ]));
	}
}
