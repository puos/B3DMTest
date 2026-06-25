using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

public class SIMMETADebug
{
	public static void Assert(bool comparison, string msg = null, int colorTag = -1)
	{
		if (!comparison)
		{
			LogService.LogError(null, msg, colorTag);
		}
	}

	public static void Error(string msg, int colorTag = -1)
	{
		LogService.LogError(null, msg, colorTag);
	}

	public static void Error(LogTag tag, string msg, int colorTag = -1)
	{
		LogService.LogError(tag, msg, colorTag);
	}

	public static void Warning(string msg, int colorTag = -1)
	{
		LogService.LogWarning(null, msg, colorTag);
	}

	public static void Warning(LogTag tag, string msg, int colorTag = -1)
	{
		LogService.LogWarning(tag, msg);
	}

	public static void Log(string msg, int colorTag = -1)
	{
		LogService.Log(null, msg, colorTag);
	}

	public static void Log(LogTag tag, string msg, int colorTag = -1)
	{
		LogService.Log(tag, msg, colorTag);
	}

}