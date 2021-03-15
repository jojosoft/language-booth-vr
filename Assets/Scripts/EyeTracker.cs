/*
 * Written by Johannes Schirm
 * Nara Institute of Science and Technology
 * Cybernetics and Reality Engineering Laboratory
 */

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ViveSR.anipal.Eye;
using SREye = ViveSR.anipal.Eye.SRanipal_Eye_API;
using SREyeData = ViveSR.anipal.Eye.EyeData_v2;

/// <summary>
/// Custom interface to access eye tracking data from SRanipal without callback.
/// The idea is to centralize as much specifics of the actual API as possible here.
/// </summary>
public class EyeTracker : MonoBehaviour
{
	/// <summary>
	/// Identifies different parts of the available eye tracking data.
	/// Left and right means the corresponding eye from the user's view.
	/// Combined is an API-provided combination of both eyes.
	/// </summary>
	public enum Source
	{
		Right, Left, Combined
	}

	/// <summary>
	/// Represents all possible states of winking.
	/// None means that both eyes are either equally closed or opened.
	/// </summary>
	public enum Wink
	{
		None, Right, Left
	}

	/// <summary>
	/// 0.0 for almost no sensitivity and 1.0 for highest sensitivity.
	/// Too high sensitivity might result in too fluctuating data.
	/// Lower sensitivity leads to increasingly more delays.
	/// </summary>
	[Range(0.0f, 1.0f)]
	public double sensitivity = 0.75;

	[Range(0.0f, 1.0f)]
	[Tooltip("The minimal difference in eye openness between the left and right eye required for a wink.")]
	public float winkThreshold = 0.3f;

	/// <summary>
	/// The time in seconds that eye openness values should be buffered.
	/// This implicitly controls the certainty with which winks are detected.
	/// Note that the buffer is only created once in Start.
	/// </summary>
	[Min(0.001f)]
	public float winkBufferSeconds = 1.0f;

	/// <summary>
	/// The place for the API to write new eye tracking data to.
	/// </summary>
	private SREyeData eyeData;

	/// <summary>
	/// Simple struct to hold one set of eye openness values.
	/// Only used for internal buffering, necessary for smoothing.
	/// </summary>
	private struct EOData
	{
		public float time, right, left;

		/// <summary>
		/// Calculate the difference in eye openness between the two eyes for this record.
		/// </summary>
		/// <returns>The difference in eye openness between right and left eye.</returns>
		public float GetDifference()
		{
			return Mathf.Abs(right - left);
		}

		/// <summary>
		/// Calculate the age of the values in this record.
		/// </summary>
		/// <returns>This data record's age in seconds.</returns>
		public float GetAge()
		{
			return Time.time - time;
		}
	}

	/// <summary>
	/// History of all recent eye openness values and their times.
	/// Used to internally calculate certainty values for wink detections.
	/// </summary>
	private Queue<EOData> EOHistory;

	void Start()
	{
		// Set the sensitivity once at startup.
		EyeParameter ep;
		ep.gaze_ray_parameter.sensitive_factor = sensitivity;
		SREye.SetEyeParameter(ep);
		// Initialize the eye openness history.
		EOHistory = new Queue<EOData>(Mathf.CeilToInt(winkBufferSeconds * 100));
	}

	void Update()
	{
		// Retrieve new eye tracking data each frame and store it in the member variable.
		// It is recommended to place the eye tracker at the top of the update order.
		ViveSR.Error e = SREye.GetEyeData_v2(ref eyeData);
		if (e != 0)
		{
			string eName = Enum.GetName(typeof(ViveSR.Error), e);
			Debug.LogError("[EyeTracker] Error " + eName + " occurred while retrieving data.");
		}
		else
		{
			// Add the current eye openness values to the history.
			EOHistory.Enqueue(GetEOData());
		}
		// Make sure to remove old eye openness values from the history.
		while (EOHistory.Count > 0 && EOHistory.Peek().GetAge() > winkBufferSeconds)
		{
			EOHistory.Dequeue();
		}
	}

	/// <summary>
	/// Builds a Unity ray in world space which represents the gaze ray of the given source.
	/// </summary>
	/// <param name="source">The eye data source to build the ray from.</param>
	/// <returns>A ray in Unity world space representing the gaze of the given source.</returns>
	public Ray GetRay(Source source = Source.Combined)
	{
		return GetWorldRay(ref GetEyeData(source));
	}

	/// <summary>
	/// Returns a value between 0.0f (closed) and 1.0f (opened) representing eye openness of the given source.
	/// The value returned for the combined source will be the average of the values for both eyes.
	/// </summary>
	/// <param name="source">The eye data source to determine eye openness from.</param>
	/// <returns>Eye openness of the given source on a smooth floating point scale.</returns>
	public float GetEyeOpenness(Source source = Source.Combined)
	{
		if (source == Source.Right)
		{
			return this.eyeData.verbose_data.right.eye_openness;
		}
		else if (source == Source.Left)
		{
			return this.eyeData.verbose_data.left.eye_openness;
		}
		else
		{
			// Just return the average for the combined source!
			return (GetEyeOpenness(Source.Right) + GetEyeOpenness(Source.Left)) / 2.0f;
		}
	}

	/// <summary>
	/// Returns the detected pupil diameter of the given source in millimeters.
	/// The value returned for the combined source will be the average of the values for both eyes.
	/// </summary>
	/// <param name="source">The eye data source to determine the pupil diameter from.</param>
	/// <returns>Pupil diameter of the given source as floating point number in millimeters.</returns>
	public float GetPupilDiameter(Source source = Source.Combined)
	{
		if (source == Source.Right)
		{
			return this.eyeData.verbose_data.right.pupil_diameter_mm;
		}
		else if (source == Source.Left)
		{
			return this.eyeData.verbose_data.left.pupil_diameter_mm;
		}
		else
		{
			// Just return the average for the combined source!
			return (GetPupilDiameter(Source.Right) + GetPupilDiameter(Source.Left)) / 2.0f;
		}
	}

	/// <summary>
	/// Calculates the user's current focus point in world space by intersecting the individual gaze rays.
	/// Please make sure to interpret the results of this function correctly, as its accuracy can fluctuate.
	/// Especially when focusing something more than 3 meters away, the focus point is difficult to determine.
	/// </summary>
	/// <returns>The current focus point of the user in Unity world space, based on the individual gaze rays.</returns>
	public Vector3 GetFocusPoint()
	{
		// Calculate the closest point to the other gaze ray for both of them.
		Ray rightEyeRay = GetRay(Source.Right);
		Ray leftEyeRay = GetRay(Source.Left);
		Vector3 closestPointRight, closestPointLeft;
		ClosestPointsOnTwoLines(out closestPointRight, out closestPointLeft, rightEyeRay, leftEyeRay);
		// They often do not actually intersect, so calculate the point inbetween.
		return Vector3.Lerp(closestPointLeft, closestPointRight, 0.5f);
	}

	/// <summary>
	/// Analyzes eye openness values to determine whether the user is currently performing a wink.
	/// A wink is assumed if the difference in eye openness between both eyes exceeds the defined threshold.
	/// </summary>
	/// <returns>The wink state currently occurring according to the defined threshold.</returns>
	public Wink GetWinkState()
	{
		return GetWinkState(GetEOData());
	}

	/// <summary>
	/// Analyzes eye openness values to determine whether the user is currently performing a wink.
	/// A wink is assumed if the difference in eye openness between both eyes exceeds the defined threshold.
	/// The certainty value can be used to apply custom thresholds in order to avoid strong fluctuations.
	/// </summary>
	/// <param name="certainty">Certainty from 0.0f (none) to 1.0f (full) about the returned wink state.</param>
	/// <returns>The wink state currently occurring according to the defined threshold.</returns>
	public Wink GetWinkState(out float certainty)
	{
		EOData data = GetEOData();
		Wink state = GetWinkState(data);
		// Try to calculate certainty from past values.
		float similarity = 0.0f, fluctuationCenter = 1.0f;
		// Of course, this is only possible with at least one "pair" of subsequent values.
		if (EOHistory.Count > 2)
		{
			// First, count the ratio of values that implied the same result.
			similarity = EOHistory.Count((EOData d) => {
				return GetWinkState(d) == state;
			}) / (float)EOHistory.Count;
			// Also, determine the center of fluctuation (previous value different from next) in the history.
			// 0.0 will be right at the front of the queue (now), 1.0 will be at the back (oldest value pair).
			float fluctTimeAvg = EOHistory.Skip(1).Zip(EOHistory, (EOData dPrev, EOData d) => {
				// Use negative times to "mark" values that should be skipped (since there is no change).
				return d.time * (GetWinkState(d) == GetWinkState(dPrev) ? -1 : 1);
			}).Where((float time) => time > 0).DefaultIfEmpty(winkBufferSeconds).Average();
			fluctuationCenter = 1 - (Time.time - fluctTimeAvg) / winkBufferSeconds;
		}
		// Then, decide how certain we can be about the current result.
		// TODO: Just a first idea of how to decide some rules manually.
		// This is very much preserving winks and avoiding short drops.
		// It also protects more agaist "recent" fluctuations than others.
		certainty = state != Wink.None && similarity > 0.1f ? 1.0f : Mathf.Clamp01(similarity - fluctuationCenter);
		return state;
	}

	/// <summary>
	/// Analyzes eye openness values to determine whether the user is currently performing a wink.
	/// A wink is assumed if the difference in eye openness between both eyes exceeds the defined threshold.
	/// </summary>
	/// <param name="data">The eye openness data that should be analyzed.</param>
	/// <returns>The wink state currently occurring according to the defined threshold.</returns>
	private Wink GetWinkState(EOData data)
	{
		if (data.GetDifference() > winkThreshold)
		{
			// If the right eye openness is larger during a wink, this is a LEFT wink!
			return data.right > data.left ? Wink.Left : Wink.Right;
		}
		else
		{
			return Wink.None;
		}
	}

	/// <summary>
	/// Determine from eye tracking data whether the headset is currently in use.
	/// </summary>
	/// <returns>Whether the headset is currently worn by a user.</returns>
	public bool IsUserPresent()
	{
		// This is the most senseless variable name in SRanipal...
		return eyeData.no_user;
	}

	/// <summary>
	/// Run the integrated eye tracking calibration procedure.
	/// This function will block until the procedure has finished.
	/// </summary>
	/// <returns>Whether the calibration was performed successfully.</returns>
	public static bool RunCalibration()
	{
		return SREye.LaunchEyeCalibration(IntPtr.Zero) == (int)ViveSR.Error.WORK;
	}

	/// <summary>
	/// Returns the corresponding (single) eye data from the current eye data.
	/// Eye data is updated once each frame through the Update method.
	/// </summary>
	/// <param name="source">The eye data source to return the data from.</param>
	/// <returns>A reference to the (single) eye data indicated by the given source.</returns>
	private ref SingleEyeData GetEyeData(Source source)
	{
		if (source == Source.Right)
		{
			return ref this.eyeData.verbose_data.right;
		}
		else if (source == Source.Left)
		{
			return ref this.eyeData.verbose_data.left;
		}
		else
		{
			return ref this.eyeData.verbose_data.combined.eye_data;
		}
	}

	/// <summary>
	/// Assembles a new eye openness data object with the current values.
	/// </summary>
	/// <returns>An eye openness data object based on the most recent values.</returns>
	private EOData GetEOData()
	{
		return new EOData{
			time = Time.time,
			right = GetEyeOpenness(Source.Right),
			left = GetEyeOpenness(Source.Left)
		};
	}

	/// <summary>
	/// Extracts a Unity ray from the eye data for a single eye, representing its gaze ray.
	/// </summary>
	/// <param name="singleEyeData">The (single) eye data to build the ray from.</param>
	/// <returns>A ray in Unity coordinates, starting at the gaze origin, pointing into the gaze direction.</returns>
	private static Ray GetWorldRay(ref SingleEyeData singleEyeData)
	{
		// We receive the origin and direction in a right-handed coordinate system, but Unity is left-handed.
		Vector3 origin = singleEyeData.gaze_origin_mm * 0.001f;
		origin.x *= -1;
		Vector3 direction = singleEyeData.gaze_direction_normalized;
		direction.x *= -1;
		return new Ray(Camera.main.transform.TransformPoint(origin), Camera.main.transform.TransformDirection(direction));
	}

	/// <summary>
	/// Two non-parallel rays which may or may not touch each other each have a point on them at which the other ray is closest.
	/// This function finds those two points. If the rays are not parallel, the function outputs true, otherwise false.
	/// Source: http://wiki.unity3d.com/index.php/3d_Math_functions
	/// </summary>
	/// <param name="closestPointRay1">An out parameter to store the resulting point on the first ray in.</param>
	/// <param name="closestPointRay2">An out parameter to store the resulting point on the second ray in.</param>
	/// <param name="ray1">The first input ray.</param>
	/// <param name="ray2">The second input ray.</param>
	/// <returns>Whether the two input rays are parallel.</returns>
	private static bool ClosestPointsOnTwoLines(out Vector3 closestPointRay1, out Vector3 closestPointRay2, Ray ray1, Ray ray2)
	{
		// TODO: Maybe move to a central place more related to general Unity vector math..?
		float a = Vector3.Dot(ray1.direction, ray1.direction);
		float b = Vector3.Dot(ray1.direction, ray2.direction);
		float e = Vector3.Dot(ray2.direction, ray2.direction);
		float d = a * e - b * b;

		if (d != 0.0f)
		{
			// The two lines are not parallel.
			Vector3 r = ray1.origin - ray2.origin;
			float c = Vector3.Dot(ray1.direction, r);
			float f = Vector3.Dot(ray2.direction, r);

			float s = (b * f - c * e) / d;
			float t = (a * f - c * b) / d;

			closestPointRay1 = ray1.origin + ray1.direction * s;
			closestPointRay2 = ray2.origin + ray2.direction * t;

			return true;
		}
		else
		{
			closestPointRay1 = Vector3.zero;
			closestPointRay2 = Vector3.zero;

			return false;
		}
	}
}
