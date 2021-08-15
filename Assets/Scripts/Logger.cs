/*
 * Written by Johannes Schirm
 * Reutlingen University
 */

using System;
using System.Linq;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Logger : MonoBehaviour
{
	// Only for internal use - all fields are public.
	// The constructor and update methods allow easy handling.
	private struct DataField
	{
		public string name;
		public string data;
		public float age;
		public bool alwaysUpToDate;

		public DataField(string name, string data, bool alwaysUpToDate)
		{
			this.name = name;
			this.data = data;
			this.age = 0.0f;
			this.alwaysUpToDate = alwaysUpToDate;
		}
	}

	public string directory = "/sdcard/Documents/";
	public string undefinedValue = "NA";
	public string fileNamingPattern = "yyyy-MM-dd-HH-mm-ss";

	// Current values for each output column, accessible by name.
	// Each value also has its age (since the last update) stored.
	private List<DataField> fields = new List<DataField>();

	// Flag indicating if there are unwritten changes.
	private bool writeNecessary = false;

	// Flag indicating whether logging has been started.
	// Fields cannot be changed while a log is being recorded.
	private bool active = false;

	// Name of the file that we are currently writing to.
	private string filename = null;

	// The serial (ID) of the file that we are currently writing to.
	private uint serial;

	// Point in time when the logging begun, as a reference.
	private DateTime startTime;

	void Awake()
	{
		// Make sure that the logging directory exists.
		Directory.CreateDirectory(directory);
	}

	public void RegisterField(string name, bool alwaysUpToDate = false, string startValue = null)
	{
		if (!active)
		{
			if (startValue == null)
			{
				startValue = undefinedValue;
			}
			int index = GetFieldIndex(name);
			if (index < 0)
			{
				fields.Add(new DataField(name, startValue, alwaysUpToDate));
			}
			else
			{
				// The log file should not contain two columns with the same name.
				Debug.LogWarning("A logging field named " + name + " already exists.");
			}
		}
		else
		{
			throw new Exception("A logging field was registered without ending the current log.");
		}
	}

	public void UnregisterField(string name)
	{
		if (!active)
		{
			fields.RemoveAt(GetFieldIndex(name));
		}
		else
		{
			throw new Exception("A logging field was unregistered without ending the current log.");
		}
	}

	public uint Begin()
	{
		if (!active)
		{
			startTime = DateTime.Now;
			// Determine the next free serial in the log directory.
			// This allows us to indicate a clear order through log file names.
			this.serial = Directory.GetFiles(directory, "*.txt").Select(path => {
				uint serial = 0;
				string fname = Path.GetFileName(path);
				uint.TryParse(fname.Substring(0, fname.IndexOf('-')), out serial);
				return serial;
			}).Append<uint>(0).Max() + 1;
			// Activate logging, set the filename for this logging period and write the header!
			active = true;
			filename = this.serial.ToString("D3") + "-" + DateTime.Now.ToString(fileNamingPattern) + ".txt";
			WriteHeader();
		}
		return this.serial;
	}

	/// <summary>
	/// Returns whether logging is currently active.
	/// Fields can only be changed while logging is inactive.
	/// Fields can only be updated while logging is active.
	/// </summary>
	public bool IsLogging()
	{
		return active;
	}

	/// <summary>
	/// Returns the full path to the current log file or the last log file if currently inactive.
	/// </summary>
	public string GetFilePath()
	{
		return Path.Combine(Path.GetFullPath(directory), filename);
	}

	public void UpdateField(string name, string data)
	{
		if (active)
		{
			// Sadly, this is how list items are updated in C#...
			DataField field = fields[GetFieldIndex(name)];
			field.data = data;
			field.age = 0.0f;
			fields[GetFieldIndex(name)] = field;
			writeNecessary = true;
		}
		else
		{
			throw new Exception("A logging field was updated without having begun to log.");
		}
	}

	void LateUpdate()
	{
		if (active && writeNecessary)
		{
			WriteFields();
		}
		// Update the age of all fields after (potential) writing took place.
		// Sadly, this is how list items are updated in C#...
		for (int i = 0; i < fields.Count; i++)
		{
			DataField field = fields[i];
			field.age += Time.deltaTime;
			fields[i] = field;
		}
	}

	/// <summary>
	/// Ends logging if active and immediately writes the last line if there are any unwritten updates.
	/// When logging has to end unexpectedly, the given reason will be appended to the file as another line.
	/// This allows to easily identify incomplete logs. (Most statistics software is likely to reject these files.)
	/// </summary>
	/// <param name="unexpectedReason">The reason why logging ended unexpectedly and the log file is not considered complete.</param>
	public void End(string unexpectedReason = null)
	{
		if (active)
		{
			active = false;
			if (writeNecessary)
			{
				WriteFields();
			}
			if (unexpectedReason != null)
			{
				File.AppendAllText(GetFilePath(), unexpectedReason.Replace('\t', '_') + "\n");
			}
		}
	}

	private int GetFieldIndex(string name)
	{
		return fields.FindIndex(field => field.name.Equals(name));
	}

	// Appends a new line (usually the first) with all field names to the log file.
	private void WriteHeader()
	{
		string header = fields.Select(field => field.name).Aggregate((whole, next) => whole + "\t" + next);
		File.AppendAllText(GetFilePath(), "time\t" + header + "\n");
	}

	// Appends a new line with the current field values to the log file.
	// Fields that are not always up to date and have already aged are considered undefined.
	private void WriteFields()
	{
		string secondsPassed = (DateTime.Now - startTime).TotalSeconds.ToString("F3");
		string line = fields.Select(field => {
			return field.alwaysUpToDate || field.age == 0.0f ? field.data : undefinedValue;
		}).Aggregate((whole, next) => whole + "\t" + next);
		File.AppendAllText(GetFilePath(), secondsPassed + "\t" + line + "\n");
		writeNecessary = false;
	}
}
