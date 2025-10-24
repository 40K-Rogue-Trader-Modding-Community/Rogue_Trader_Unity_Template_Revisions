using System;
using System.IO;
using System.Text;
using UnityEditor;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Code.Framework.Editor.VersionControl
{
	public abstract class Vcs
	{
		private static readonly string[] Eols = {"\r\n", "\r", "\n"};

		private readonly OsCommand osCommand;

		private bool? isDead;

		public bool IsDead
		{
			get
			{
				isDead ??= CheckIsDead();
				if (isDead.HasValue && isDead.Value)
				{
					Debug.LogError("Version control system is not detected!");
				}
				return isDead.Value;
			}
		}

		protected abstract bool CheckIsDead();

		protected abstract string Executable { get; }
		public abstract bool ActivateHooks();
		public abstract bool DeactivateHooks();
		protected abstract bool IsTracked(out bool isTracked, string path);
		public abstract bool Move(string srcPath, string dstPath);

		static Vcs()
		{
			Instance = new Git();
			Debug.Log("Looking for git...");
			if (Instance.CheckIsDead())
			{
				Debug.Log("Looking for svn...");
				Instance = new Svn();
			}
		}
		public static Vcs Instance { get; }

		protected Vcs()
		{
			osCommand = new OsCommand(Encoding.Default);
		}

		protected bool Execute(string vcsCommand, out string[]? stdOut)
		{
			stdOut = null;
			if (isDead.HasValue && IsDead)
			{
				return false;
			}

			bool isSuccess = osCommand.Execute(Executable, vcsCommand);
			string? errors = osCommand.StdErr;
			stdOut = osCommand.StdOut?.Split(Eols, StringSplitOptions.RemoveEmptyEntries);
			if (!string.IsNullOrEmpty(errors))
			{
				Debug.LogError(errors);
				isSuccess = false;
			}
			return isSuccess;
		}

		protected void MakeSureDirExists(string filePath)
		{
			string? dirPath = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath))
			{
				Directory.CreateDirectory(dirPath);
			}
		}

		[MenuItem("Tools/Validates/Validation hooks/Enable")]
		private static void EnableHooks()
		{
			if (Instance.ActivateHooks())
			{
				EditorUtility.DisplayDialog("Success!", "Validation hooks activated!", "Ok");
				return;
			}

			EditorUtility.DisplayDialog("Error",
				"Failed to activate validation hooks.\n" +
				"See editor log for details", "Ok");
		}

		[MenuItem("Tools/Validates/Validation hooks/Disable")]
		private static void DisableHooks()
		{
			if (Instance.DeactivateHooks())
			{
				EditorUtility.DisplayDialog("Success!", "Validation hooks removed!", "Ok");
				return;
			}
			EditorUtility.DisplayDialog("Error",
				"Failed to remove validation hooks\n" +
				"See editor log for details", "Ok");
		}
	}
}