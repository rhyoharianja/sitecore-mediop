using System;
using log4net;
using Sitecore;
using Sitecore.Diagnostics;

namespace Mediop
{
	/// <summary>
	/// Dedicated logger for the optimizer. Writes to the "Mediop" log4net logger so the
	/// optimization chatter stays out of the main Sitecore log (see Mediop.Log.config).
	/// </summary>
	public static class MediopLog
	{
		private static readonly ILog Log;

		static MediopLog()
		{
			Log = LogManager.GetLogger("Mediop") ?? LoggerFactory.GetLogger(typeof(MediopLog));
		}

		public static bool IsDebugEnabled => Log.IsDebugEnabled;

		public static void Audit(string message)
		{
			Assert.ArgumentNotNull(message, nameof(message));
			Log.Info($"AUDIT ({Context.User.Name}) {message}");
		}

		public static void Debug(string message, Exception exception = null)
		{
			if (exception == null) Log.Debug(message);
			else Log.Debug(message, exception);
		}

		public static void Info(string message, Exception exception = null)
		{
			if (exception == null) Log.Info(message);
			else Log.Info(message, exception);
		}

		public static void Warn(string message, Exception exception = null)
		{
			if (exception == null) Log.Warn(message);
			else Log.Warn(message, exception);
		}

		public static void Error(string message, Exception exception = null)
		{
			if (exception == null) Log.Error(message);
			else Log.Error(message, exception);
		}

		public static void Fatal(string message, Exception exception = null)
		{
			if (exception == null) Log.Fatal(message);
			else Log.Fatal(message, exception);
		}
	}
}
