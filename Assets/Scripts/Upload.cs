using System;
using System.IO;
using System.IO.Compression;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using DateTime = System.DateTime;

public static class Upload
{
	/// <summary>
	/// Starts a new thread to first compress the given file and then upload it to the given URI.
	/// </summary>
	/// <param name="filepath">Path of the file that should be compressed and uploaded.</param>
	/// <param name="uri">Web address (URI) that will accept the file as a POST request.</param>
	/// <param name="errorCallback">Optional callback which is only called if the upload failed.</param>
	public static void Async(string filepath, string uri, Action<string> errorCallback = null)
	{
		// Start a new thread for the upload.
		new Thread(() => {
			if (!Upload.Sync(filepath, uri) && errorCallback != null)
			{
				errorCallback(filepath);
			}
		}).Start();
	}

	/// <summary>
	/// First compresses the given file and then uploads it to the given URI.
	/// </summary>
	/// <param name="filepath">Path of the file that should be compressed and uploaded.</param>
	/// <param name="uri">Web address (URI) that will accept the file as a POST request.</param>
	/// <returns>Whether the file was successfully uploaded.</returns>
	public static bool Sync(string filepath, string uri)
	{
		// Compress the file before uploading.
		string path = Compress(filepath);
		bool success = TryUpload(path, uri);
		File.Delete(path);
		return success;
	}

	/// <summary>
	/// Compresses the given file using GZip.
	/// The compressed file (with ".gz" attached to its name) will be put next to the original file.
	/// </summary>
	/// <param name="filename">Path of the file to compress.</param>
	/// <returns>Path of the compressed file.</returns>
	private static string Compress(string filename)
	{
		string compressedPath = filename + ".gz";
		using (FileStream output = File.Create(compressedPath))
		{
			using (GZipStream zip = new GZipStream(output, CompressionMode.Compress))
			{
				using (FileStream input = File.OpenRead(filename))
				{
					input.CopyTo(zip);
				}
			}
		}
		return compressedPath;
	}

	/// <summary>
	///	Tries to upload the given file to the given URI using a POST request.
	///	Make sure to use HTTPS to have an encrypted connection and transfer the files safely!
	/// </summary>
	/// <param name="filename">Path of the file that should be uploaded.</param>
	/// <param name="uri">Web address (URI) that will accept the file as a POST request.</param>
	private static bool TryUpload(string filename, string uri)
	{
		WebResponse response = null;
		try
		{
			// Source: https://stackoverflow.com/a/42363001
			string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
			byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");
			HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(uri);
			wr.ProtocolVersion = HttpVersion.Version10;
			wr.ContentType = "multipart/form-data; boundary=" + boundary;
			wr.Method = "POST";
			wr.KeepAlive = true;
			Stream stream = wr.GetRequestStream();

			stream.Write(boundarybytes, 0, boundarybytes.Length);
			byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(filename);
			stream.Write(formitembytes, 0, formitembytes.Length);
			stream.Write(boundarybytes, 0, boundarybytes.Length);
			string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
			// Name the field using a specific access key, this allows the server to only accept requests from this application.
			string header = string.Format(headerTemplate, "PleaseChangeTheSecurityKey", Path.GetFileName(filename), Path.GetExtension(filename));
			byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
			stream.Write(headerbytes, 0, headerbytes.Length);

			FileStream fileStream = new FileStream(filename, FileMode.Open, FileAccess.Read);
			byte[] buffer = new byte[4096];
			int bytesRead = 0;
			while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
				stream.Write(buffer, 0, bytesRead);
			fileStream.Close();

			byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
			stream.Write(trailer, 0, trailer.Length);
			stream.Close();

			response = wr.GetResponse();
			// We are currently not interested in the response, just put the file there.
			/*Stream responseStream = response.GetResponseStream();
			StreamReader streamReader = new StreamReader(responseStream);
			string responseData = streamReader.ReadToEnd();*/
		}
		catch
		{
			return false;
		}
		finally
		{
			if (response != null)
			{
				response.Close();
			}
		}
		return true;
	}
}
