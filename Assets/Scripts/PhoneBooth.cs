using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PhoneBooth : MonoBehaviour
{
	[Header("The user has arrived in the phone booth for some time.")]
	public UnityEvent userArrive;
	[Header("The user has just left the phone booth again.")]
	public UnityEvent userLeave;

	[Tooltip("Announces audio clip played, if not specified otherwise.")]
	public AudioClip dialSound;

	[Tooltip("Minimum time that the user needs to be in the phone booth to have arrived.")]
	public float arrivalSeconds = 1.0f;
	[Tooltip("Also play the full audio in debug builds. (Usually, only a sample is played.)")]
	public bool debugAudio = false;

	// Internal reference to the main speaker of the phone.
	private AudioSource speaker;
	// Internal reference to the blink light on top of the phone.
	private BlinkLight audioLight;

	// Check whether the user actually stays in the phone booth, wait for the ring to end and fire the arrive event.
	private Coroutine arrivalCheck;
	// Variable to store the current phone audio clip in. This does only include the actual calls!
	private AudioClip currentClip;
	private float currentClipStart;

	void Start()
	{
		speaker = this.GetComponentInChildren<AudioSource>();
		audioLight = this.GetComponentInChildren<BlinkLight>();
	}

	void Update()
	{
		if (currentClip != null)
		{
			// We know that the current clip will always be played through the main speaker.
			float[] sample = new float[2];
			float samplesPerSecond = currentClip.samples / currentClip.length;
			int start = Mathf.CeilToInt(samplesPerSecond * (Time.time - currentClipStart));
			currentClip.GetData(sample, Mathf.Clamp(start, 0, currentClip.samples - sample.Length));
			audioLight.SetIndicator(sample.Average());
		}
	}

	void OnTriggerEnter(Collider other)
	{
		arrivalCheck = StartCoroutine(ArrivalCheck());
	}

	void OnTriggerExit(Collider other)
	{
		if (arrivalCheck != null)
		{
			// The user has left again...
			StopCoroutine(arrivalCheck);
			arrivalCheck = null;
			// Also turn of the audio light, just to be sure...
			currentClip = null;
			audioLight.TurnOff();
		}
		userLeave.Invoke();
	}

	/// <summary>
	/// Wait for the ring to end, at least one second.
	/// Always execute as a subroutine.
	/// </summary>
	private IEnumerator ArrivalCheck()
	{
		yield return new WaitForSeconds(1);
		// Wait until the playback position is half a second before the clip's end.
		yield return new WaitUntil(() => speaker.time > speaker.clip.length - 0.5f);
		speaker.Stop();
		userArrive.Invoke();
	}

	/// <summary>
	/// Use the phone booth's speaker to play an audio clip.
	/// The audio clip will be preceeded by a dial-up sound unless enabled.
	/// For the light bulb to indicate audio, it is necessary to dial.
	/// Always execute as a subroutine.
	/// </summary>
	/// <param name="clip">Audio clip to play through the phone booth's speakers.</param>
	public IEnumerator PlayClip(AudioClip clip, bool dialup = true)
	{
		if (speaker.isPlaying)
		{
			speaker.Stop();
		}
		// Only play the dial-up sound if requested.
		if (dialup)
		{
			speaker.PlayOneShot(dialSound);
			yield return new WaitForSeconds(dialSound.length);
		}
		speaker.PlayOneShot(clip);
		if (dialup)
		{
			// Only activate the audio indicator after dialing.
			audioLight.TurnOn();
			currentClip = clip;
			currentClipStart = Time.time;
		}
		if (Debug.isDebugBuild && !debugAudio)
		{
			// For development, only play the first three seconds.
			yield return new WaitForSeconds(3);
		}
		else
		{
			yield return new WaitForSeconds(clip.length);
		}
		if (dialup)
		{
			// If it has been active, disable the audio indicator.
			currentClip = null;
			audioLight.TurnOff();
		}
		speaker.Stop();
	}

	/// <summary>
	/// Stop any running audio playback and switch back to ringing.
	/// </summary>
	public void StartRing()
	{
		if (speaker.isPlaying)
		{
			speaker.Stop();
		}
		speaker.Play();
	}
}
