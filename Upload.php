<?php
	// PHP script for accepting data files of a remote experiment.
	// Adjust it for the server setup in use.

	// Using a '.' in the key resulted in problems.
	// It was received as '_', who knows why...)
	$key = 'PleaseChangeTheSecurityKey';
	if (count($_FILES) > 0 && isset($_FILES[$key])) {
		// Move the file and set appropriate permissions.
		// (Change './' to a dedicated directory PHP can write to.)
		// (Make sure that the files WILL BE INACCESSIBLE from the outside!)
		$target = './' . $_FILES[$key]['name'];
		move_uploaded_file($_FILES[$key]['tmp_name'], $target);
		chmod($target, 0640);
		// Already decompress the file for later.
		system("gzip -d " . $target);
	}
?>
