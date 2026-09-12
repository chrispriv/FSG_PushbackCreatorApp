namespace FSG_PushbackCreator.Services;

/// <summary>
/// Aerofly TSC/WAD: a line that starts with '&lt;' and does not end with '&gt;' opens a section
/// and needs a later line that is only '&gt;'. Leaf fields already end with '&gt;' on the same line.
/// </summary>
static class AeroflySectionBalance
{
	public static void Ensure(string text, string label)
	{
		var depth = 0;
		foreach (var raw in text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n'))
		{
			var line = raw.Trim();
			if (line.Length == 0)
				continue;

			if (line == ">")
			{
				depth--;
				if (depth < 0)
					throw new InvalidOperationException($"{label}: extra '>' closer.");
				continue;
			}

			if (!line.StartsWith('<'))
				throw new InvalidOperationException($"{label}: unexpected line '{line}'.");

			if (!line.EndsWith('>'))
				depth++;
		}

		if (depth != 0)
			throw new InvalidOperationException($"{label}: {depth} section(s) not closed with '>'.");
	}
}
