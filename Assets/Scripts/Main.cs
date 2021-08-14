using System.Collections;
using System.Linq;
using UnityEngine;

/// <summary>
/// Controls the experiment procedure.
/// </summary>
public class Main : MonoBehaviour
{
	/// <summary>
	/// Easy access to the names of the data fields used by the logger.
	/// </summary>
	private static class Field
	{
		/// <summary>
		/// Determines the base name of fields which are part of a 3D point.
		/// Important: This assumes that the field naming is done with suffixes.
		/// Example: Fields "valueX", "valueY" and "valueZ" represent a 3D vector.
		/// The base name will be "value", the expected suffixes "X", "Y" and "Z".
		/// </summary>
		/// <param name="fieldName">Any of the three involved field names.</param>
		/// <returns>Base name which all fields for this 3D point have in common.</returns>
		public static string GetBase(string fieldName)
		{
			return fieldName.TrimEnd('X', 'Y', 'Z');
		}
		/// <summary>
		/// Number of the audio clip currently played.
		/// </summary>
		public static string Clip { get { return "clip"; } }
		/// <summary>
		/// Number of times the current clip has already been started.
		/// If the user steps out and in again, the current clips starts anew.
		/// </summary>
		public static string Try { get { return "try"; } }
		/// <summary>
		/// [X component of Unity world position in meters]
		/// Head position of the user as defined by the main camera.
		/// </summary>
		public static string HeadX { get { return "headX"; } }
		/// <summary>
		/// [Y component of Unity world position in meters]
		/// Head position of the user as defined by the main camera.
		/// </summary>
		public static string HeadY { get { return "headY"; } }
		/// <summary>
		/// [Z component of Unity world position in meters]
		/// Head position of the user as defined by the main camera.
		/// </summary>
		public static string HeadZ { get { return "headZ"; } }
		/// <summary>
		/// [X component of normalized Unity vector in meters]
		/// Vector indicating the up direction of the user's head.
		/// It is already normalized to 1 m.
		/// </summary>
		public static string UpX { get { return "upX"; } }
		/// <summary>
		/// [Y component of normalized Unity vector in meters]
		/// Vector indicating the up direction of the user's head.
		/// It is already normalized to 1 m.
		/// </summary>
		public static string UpY { get { return "upY"; } }
		/// <summary>
		/// [Z component of normalized Unity vector in meters]
		/// Vector indicating the up direction of the user's head.
		/// It is already normalized to 1 m.
		/// </summary>
		public static string UpZ { get { return "upZ"; } }
		/// <summary>
		/// [X component of Unity world position in meters]
		/// Position indicating the general view direction of the user.
		/// Determined using a probe object 1 m in front of the main camera.
		/// </summary>
		public static string ViewX { get { return "viewX"; } }
		/// <summary>
		/// [Y component of Unity world position in meters]
		/// Position indicating the general view direction of the user.
		/// Determined using a probe object 1 m in front of the main camera.
		/// </summary>
		public static string ViewY { get { return "viewY"; } }
		/// <summary>
		/// [Z component of Unity world position in meters]
		/// Position indicating the general view direction of the user.
		/// Determined using a probe object 1 m in front of the main camera.
		/// </summary>
		public static string ViewZ { get { return "viewZ"; } }
		/// <summary>
		/// [X component of Unity world position in meters]
		/// Closest hit point of the user's right eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitRightX { get { return "hitRightX"; } }
		/// <summary>
		/// [Y component of Unity world position in meters]
		/// Closest hit point of the user's right eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitRightY { get { return "hitRightY"; } }
		/// <summary>
		/// [Z component of Unity world position in meters]
		/// Closest hit point of the user's right eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitRightZ { get { return "hitRightZ"; } }
		/// <summary>
		/// [X component of Unity world position in meters]
		/// Closest hit point of the user's left eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitLeftX { get { return "hitLeftX"; } }
		/// <summary>
		/// [Y component of Unity world position in meters]
		/// Closest hit point of the user's left eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitLeftY { get { return "hitLeftY"; } }
		/// <summary>
		/// [Z component of Unity world position in meters]
		/// Closest hit point of the user's left eye's gaze ray.
		/// (Hit points only exist for actual collisions with scene objects.)
		/// </summary>
		public static string HitLeftZ { get { return "hitLeftZ"; } }
		/// <summary>
		/// Name of the game object whose collider was hit by the right gaze ray.
		/// (Only if a collision with a scene object was detected.)
		/// </summary>
		public static string ColliderRight { get { return "colliderRight"; } }
		/// <summary>
		/// Name of the game object whose collider was hit by the left gaze ray.
		/// (Only if a collision with a scene object was detected.)
		/// </summary>
		public static string ColliderLeft { get { return "colliderLeft"; } }
		/// <summary>
		/// [X component of Unity world position in meters]
		/// Focus point of the user as determined by the two gaze rays.
		/// This point is at the smallest distance between the two gaze rays.
		/// </summary>
		public static string FocusX { get { return "focusX"; } }
		/// <summary>
		/// [Y component of Unity world position in meters]
		/// Focus point of the user as determined by the two gaze rays.
		/// This point is at the smallest distance between the two gaze rays.
		/// </summary>
		public static string FocusY { get { return "focusY"; } }
		/// <summary>
		/// [Z component of Unity world position in meters]
		/// Focus point of the user as determined by the two gaze rays.
		/// This point is at the smallest distance between the two gaze rays.
		/// </summary>
		public static string FocusZ { get { return "focusZ"; } }
		public static string OpennessRight { get { return "opennessRight"; } }
		public static string OpennessLeft { get { return "opennessLeft"; } }
		public static string PupilRight { get { return "pupilRight"; } }
		public static string PupilLeft { get { return "pupilLeft"; } }
	}

	public EyeTracker eyeTracker;
	public PhoneBooth phoneBooth;
	[Tooltip("A probe 1 m in front of the user, its world position will indicate the user's viewing direction.")]
	public Transform viewProbe;
	[Tooltip("A probe 1 m above the user, its world position will indicate their head's up vector.")]
	public Transform upProbe;
	[Tooltip("An object for debugging which shows gaze information.")]
	public GameObject gazeIndicator;
	[Tooltip("Sphere around the main camera which can hide the scene.")]
	public GameObject viewBlocker;
	[Tooltip("Parent object with the intro elements as children.")]
	public Transform introParent;
	[Tooltip("Reference adjusted during setup, used to adjust the phone booth.")]
	public AudioSource audioReference;
	public TextMesh userIDMesh;

	[Tooltip("All audio samples that should be played in the right order.")]
	public AudioClip[] audioSamples;
	public AudioClip userIDSound;
	[Tooltip("Web URI that will receive a POST request with a copy of the log data.")]
	public string uploadURI = "";

	[Tooltip("Indicator for debugging specific collider meshes.")]
	public bool showGazeIndicator = false;
	[Tooltip("Go through the introduction (setup) even in debug builds?")]
	public bool debugSetup = false;
	public bool randomizeClips = true;
	public float winkSeconds = 2.0f;

	// Exact order of clip indices.
	// (Determined in Start().)
	private int[] clipOrder;
	// Index of the current trial.
	private int currentTrial = 0;
	// Number of try for the same clip.
	private int currentTry = 1;
	// ID of the current user.
	private uint userID;

	IEnumerator Start()
	{
		gazeIndicator.SetActive(showGazeIndicator);
		userIDMesh.gameObject.SetActive(false);
		// Create the order of audio clips by just counting from zero.
		clipOrder = Enumerable.Range(0, audioSamples.Length).ToArray();
		if (randomizeClips)
		{
			// Mix the indices of the audio clips into a randomized order.
			// This is the easiest way to access them and also log their ID.
			// Source for slick Linq shuffle: https://stackoverflow.com/a/51606335
			clipOrder = clipOrder.Select(x => new {
				value = x,
				order = Random.value
			}).OrderBy(x => x.order).Select(x => x.value).ToArray();
		}
		Debug.Log("Order of audio clips: " + string.Join(", ", clipOrder.AsEnumerable()) + ".");
		// At the beginning of the procedure, participants receive instructions.
		// (For ease of development, only do it in real builds.)
		if (!Debug.isDebugBuild || debugSetup)
		{
			// Hide the virtual environment during the setup.
			viewBlocker.gameObject.SetActive(true);
			// Wait until there is actually some eye tracking data coming in.
			yield return new WaitUntil(() => eyeTracker.IsUserPresent());
			// Show all available intro elements from the parent.
			for (int i = 0; i < introParent.childCount; i++)
			{
				// Show the current intro element.
				introParent.GetChild(i).gameObject.SetActive(true);
				// Wait at least a few moments before accepting new winks.
				yield return new WaitForSeconds(winkSeconds);
				// Wait for the user to perform a longer wink.
				EyeTracker.Wink winkState;
				float winkCertainty;
				float winkTime = 0.0f;
				do
				{
					// Retrieve the current wink state and wink certainty:
					winkState = eyeTracker.GetWinkState(out winkCertainty);
					// Give feedback on the wink through text color.
					if (winkCertainty >= 0.25f)
					{
						Color text;
						if (winkState != EyeTracker.Wink.None)
						{
							winkTime += Time.deltaTime;
							text = Color.Lerp(Color.white, new Color(0.8f, 0, 0), winkTime / winkSeconds + 0.2f);
						}
						else
						{
							winkTime = 0.0f;
							text = Color.white;
						}
						introParent.GetChild(i).gameObject.GetComponentInChildren<TextMesh>().color = text;
					}
					yield return null;
				}
				while (winkTime < winkSeconds);
				// After the user confirmed to proceed, hide the intro element again.
				introParent.GetChild(i).gameObject.SetActive(false);
			}
			// Apply the user-adjusted volume to the phone booth's speaker!
			phoneBooth.GetComponentInChildren<AudioSource>().volume = audioReference.volume;
			// Show the virtual environment now.
			viewBlocker.gameObject.SetActive(false);
		}
		// Set up the data logger for the current user!
		Logger.RegisterField(Field.Clip, true);
		Logger.RegisterField(Field.Try, true);
		// The headset position and view probe should always be available.
		Logger.RegisterField(Field.HeadX, true);
		Logger.RegisterField(Field.HeadY, true);
		Logger.RegisterField(Field.HeadZ, true);
		Logger.RegisterField(Field.UpX, true);
		Logger.RegisterField(Field.UpY, true);
		Logger.RegisterField(Field.UpZ, true);
		Logger.RegisterField(Field.ViewX, true);
		Logger.RegisterField(Field.ViewY, true);
		Logger.RegisterField(Field.ViewZ, true);
		// Hit points for each gaze ray will only be available when looking at scene objects.
		Logger.RegisterField(Field.HitRightX);
		Logger.RegisterField(Field.HitRightY);
		Logger.RegisterField(Field.HitRightZ);
		Logger.RegisterField(Field.HitLeftX);
		Logger.RegisterField(Field.HitLeftY);
		Logger.RegisterField(Field.HitLeftZ);
		// For each hit point, log the name of the collider for a rough clue regarding the target.
		Logger.RegisterField(Field.ColliderRight);
		Logger.RegisterField(Field.ColliderLeft);
		// The focus point and metadata will only be available while the user is actually there.
		Logger.RegisterField(Field.FocusX);
		Logger.RegisterField(Field.FocusY);
		Logger.RegisterField(Field.FocusZ);
		Logger.RegisterField(Field.OpennessRight);
		Logger.RegisterField(Field.OpennessLeft);
		Logger.RegisterField(Field.PupilRight);
		Logger.RegisterField(Field.PupilLeft);
		userID = Logger.Begin();
		// Let the phone booth ring with a short delay!
		yield return new WaitForSeconds(5);
		phoneBooth.StartRing();
	}

	void Update()
	{
		Vector3 focusPoint = eyeTracker.GetFocusPoint();
		Vector3 headset = Camera.main.transform.position;
		Vector3 viewPoint = viewProbe.position;
		Vector3 upVector = upProbe.position - headset;
		// Perform two separate raycasts, just to be completely sure.
		// The "combined" gaze ray provided by SRanipal is not fully reliable.
		RaycastHit hitRight;
		RaycastHit hitLeft;
		bool right = Physics.Raycast(eyeTracker.GetRay(EyeTracker.Source.Right), out hitRight);
		bool left = Physics.Raycast(eyeTracker.GetRay(EyeTracker.Source.Left), out hitLeft);
		if (showGazeIndicator && right && left)
		{
			// For debugging, put the reference sphere at the point of collision.
			float focusDistance = Vector3.Distance(headset, focusPoint);
			float collisionDistance = Mathf.Lerp(hitRight.distance, hitLeft.distance, 0.5f);
			gazeIndicator.transform.position = Vector3.Lerp(hitRight.point, hitLeft.point, 0.5f);
			gazeIndicator.transform.LookAt(Camera.main.transform);
			gazeIndicator.GetComponentsInChildren<TextMesh>()[0].text = (focusDistance - collisionDistance).ToString("F2");
			string collider = "";
			if (hitRight.collider == hitLeft.collider)
			{
				// Only display the collider's name if both gaze rays hit the same collider.
				collider = hitRight.collider.name;
			}
			gazeIndicator.GetComponentsInChildren<TextMesh>()[1].text = collider;
		}
		else
		{
			// If not visualized anyway, draw the current gaze as a debug ray.
			Debug.DrawRay(eyeTracker.GetRay().origin, eyeTracker.GetRay().direction, Color.red, 0, true);
		}
		if (Logger.IsLogging())
		{
			UpdateVector3(Field.GetBase(Field.HeadX), headset);
			UpdateVector3(Field.GetBase(Field.UpX), upVector);
			UpdateVector3(Field.GetBase(Field.ViewX), viewPoint);
			// Only log the hit points if the rays actually hit a scene object.
			if (right)
			{
				UpdateVector3(Field.GetBase(Field.HitRightX), hitRight.point);
				Logger.UpdateField(Field.ColliderRight, hitRight.collider.name);
			}
			if (left)
			{
				UpdateVector3(Field.GetBase(Field.HitLeftX), hitLeft.point);
				Logger.UpdateField(Field.ColliderLeft, hitLeft.collider.name);
			}
			// Only log the focus point and metadata if a user is actually wearing the headset.
			if (eyeTracker.IsUserPresent())
			{
				UpdateVector3(Field.GetBase(Field.FocusX), focusPoint);
				Logger.UpdateField(Field.OpennessRight, eyeTracker.GetEyeOpenness(EyeTracker.Source.Right).ToString());
				Logger.UpdateField(Field.OpennessLeft, eyeTracker.GetEyeOpenness(EyeTracker.Source.Left).ToString());
				Logger.UpdateField(Field.PupilRight, eyeTracker.GetPupilDiameter(EyeTracker.Source.Right).ToString());
				Logger.UpdateField(Field.PupilLeft, eyeTracker.GetPupilDiameter(EyeTracker.Source.Left).ToString());
			}
		}
	}

	IEnumerator RunExperiment()
	{
		// The user has arrived (again?), start the main procedure!
		for (; currentTrial < clipOrder.Length; currentTrial++)
		{
			yield return new WaitForSeconds(2);
			UpdateStatusFields();
			yield return phoneBooth.PlayClip(audioSamples[CurrentClip]);
			// The clip successfully completed, reset the tries.
			currentTry = 1;
		}
		UpdateStatusFields(true);
		userIDMesh.text += userID.ToString();
		userIDMesh.gameObject.SetActive(true);
		// End the logging and try to upload the file.
		Logger.End();
		if (!Debug.isDebugBuild && uploadURI.Length > 0)
		{
			// Upload asynchronously to avoid unpredictable delays!
			Upload.Async(Logger.GetFilePath(), uploadURI);
		}
		yield return phoneBooth.PlayClip(userIDSound, false);
		Application.Quit();
	}

	void PauseExperiment()
	{
		// The user has left, "reset" the current clip if there is still one playing.
		if (currentTrial < clipOrder.Length)
		{
			StopAllCoroutines();
			phoneBooth.StartRing();
			currentTry++;
			UpdateStatusFields(true);
		}
	}

	/// <summary>
	/// Updates the "always up to date" fields in the logger according to the private variables.
	/// This function can optionally also set those logging fields to the undefined value.
	/// (Without touching the private variables, it's just to account for pauses in the procedure.)
	/// </summary>
	/// <param name="undefined">Whether the "always up to date" variables should be logged as undefined.</param>
	private void UpdateStatusFields(bool undefined = false)
	{
		Logger.UpdateField(Field.Clip, undefined ? Logger.undefinedValue : CurrentClip.ToString());
		Logger.UpdateField(Field.Try, undefined ? Logger.undefinedValue : currentTry.ToString());
	}

	/// <summary>
	/// Updates the logger fields for all components of a 3D vector in one go.
	/// Important: This assumes that the field naming is done with suffixes.
	/// Example: Fields "valueX", "valueY" and "valueZ" represent a 3D vector.
	/// The base name will be "value", the expected suffixes "X", "Y" and "Z".
	/// </summary>
	/// <param name="fieldBaseName">Base name which all three fields have in common.</param>
	/// <param name="v">Vector holding the new values that should be used for the update.</param>
	private void UpdateVector3(string fieldBaseName, Vector3 v)
	{
		Logger.UpdateField(fieldBaseName + "X", v.x.ToString("F4"));
		Logger.UpdateField(fieldBaseName + "Y", v.y.ToString("F4"));
		Logger.UpdateField(fieldBaseName + "Z", v.z.ToString("F4"));
	}

	private Logger Logger
	{
		get
		{
			return GetComponent<Logger>();
		}
	}

	/// <summary>
	/// Easy access to the actual index of the current clip.
	/// Since clips might be randomized, the trial number is needed to look it up.
	/// </summary>
	private int CurrentClip
	{
		get
		{
			return clipOrder[currentTrial];
		}
	}
}
