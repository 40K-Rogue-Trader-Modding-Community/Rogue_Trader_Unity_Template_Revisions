using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR_WIN
using System.IO;
using Microsoft.Win32;
#endif

#nullable enable

namespace Code.Framework.Editor.VersionControl
{
	public class Svn : Vcs
	{
		private const string TortoiseSvnSubKey = @"Software\TortoiseSVN";
		private const string HooksValueName = "hooks";
		private const string PreCommitHookName = "pre_commit_hook";

		private const string PreCommitCmdRel =
			@"Infrastructure\Services\ValidationHook\ValidationHook\ValidationHook\bin\Debug\ValidationHook.exe";

		protected override string Executable
			=> "svn";

		private class RegistryHook
		{
			public const int EntriesCount = 6;

			public readonly string HookType;
			public readonly string RepoPath;
			private readonly string HookCmd;
			private readonly string Wait;
			private readonly string Hide;
			private readonly string Enforce;

			public RegistryHook(string hookType, string repoPath, string hookCmd, string wait, string hide, string enforce)
			{
				HookType = hookType;
				RepoPath = repoPath.TrimEnd('\\');
				HookCmd = hookCmd;
				Wait = wait;
				Hide = hide;
				Enforce = enforce;
			}

			public static RegistryHook? Create(string[] lines)
			{
				return lines.Length == EntriesCount
					? new RegistryHook(
						hookType:lines[0],
						repoPath:lines[1],
						hookCmd:lines[2],
						wait:lines[3],
						hide:lines[4],
						enforce:lines[5])
					: null;
			}

			public string RegistryValue
				=> $"{HookType}\n{RepoPath}\n{HookCmd}\n{Wait}\n{Hide}\n{Enforce}";
		}

		protected override bool CheckIsDead()
		{
			bool isSuccess = Execute("status ../.gitignore", out _);
			if (!isSuccess)
			{
				return true;
			}
			Debug.Log("Svn vcs found!");
			return false;
		}

		protected override bool IsTracked(out bool isTracked, string path)
		{
			isTracked = false;
			if (IsDead)
			{
				return false;
			}

			bool isSuccess = Execute($"status \"{path}\"", out string[]? stdOut);
			if (!isSuccess || stdOut?.Length != 1)
			{
				return false;
			}

			return !stdOut[0].StartsWith("?");
		}

		public override bool ActivateHooks()
		{
			if (IsDead)
			{
				return false;
			}

			if (HookRegistryUpdate(doActivateHook: true))
			{
				Debug.Log("Svn hooks activated.");
				return true;
			}
			Debug.LogError("Failed to activate svn hooks");
			return false;
		}

		public override bool DeactivateHooks()
		{
			if (IsDead)
			{
				return false;
			}
			if (HookRegistryUpdate(doActivateHook: false))
			{
				Debug.Log("Svn hooks removed.");
				return true;
			}
			Debug.LogError("Failed to remove svn hooks");
			return false;
		}

		public override bool Move(string srcPath, string dstPath)
		{
			if (IsDead || !IsTracked(out bool isTracked, srcPath))
			{
				return false;
			}

			if (!isTracked)
			{
				Debug.LogError($"File is not under svn version control yet: {srcPath}");
				return false;
			}

			MakeSureDirExists(dstPath);
			return Execute($"mv \"{srcPath}\" \"{dstPath}\"", out _);
		}

		private static List<RegistryHook>? CreateHooksFromRegistryValue(string? value)
		{
			if (value == null)
			{
				return null;
			}

			string[] lines = value.Split('\n', StringSplitOptions.RemoveEmptyEntries);
			if (lines.Length % RegistryHook.EntriesCount != 0)
			{
				Debug.LogError("Registry hook value is malformed.");
				return null;
			}

			var hooks = new List<RegistryHook>();
			int hooksCount = lines.Length / RegistryHook.EntriesCount;
			string[] hookLines = new string[RegistryHook.EntriesCount];
			for (int hookId = 0; hookId < hooksCount; hookId++)
			{
				for (int lineId = 0; lineId < RegistryHook.EntriesCount; lineId++)
				{
					hookLines[lineId] = lines[hookId * RegistryHook.EntriesCount + lineId];
				}

				var hook = RegistryHook.Create(hookLines);
				if (hook != null)
				{
					hooks.Add(hook);
				}
			}
			return hooks;
		}

		private static string CreateRegistryValueFromHooks(IEnumerable<RegistryHook> hooks)
		{
			return string.Join('\n', hooks
				.Select(h => h.RegistryValue));
		}

		private static bool HookRegistryUpdate(bool doActivateHook)
		{
#if UNITY_EDITOR_WIN
			string repoPath = Path.GetFullPath("../").TrimEnd('\\');
			string hookCmd = Path.GetFullPath(@$"../{PreCommitCmdRel}");

			string? currentHooksValue;
			using (RegistryKey? key = Registry.CurrentUser.OpenSubKey(TortoiseSvnSubKey))
			{
				if (key == null)
				{
					Debug.LogError("Cannot get TortoiseSVN registry key to read. Is TortoiseSVN installed?");
					return false;
				}

				// Get any existing hooks
				currentHooksValue = key.GetValue(HooksValueName)?.ToString();
			}

			// In case some hooks from other repos are exist we have to replace only the one from our repo
			var hooks = currentHooksValue == null ? new List<RegistryHook>() : CreateHooksFromRegistryValue(currentHooksValue);
			if (hooks == null)
			{
				return false;
			}

			// Remove any old hook
			hooks.RemoveAll(hook => hook.HookType == PreCommitHookName && (hook.RepoPath == repoPath));
			if (doActivateHook)
			{
				// Add the new one
				hooks.Add(new RegistryHook(PreCommitHookName, repoPath, hookCmd, "true", "hide", "true"));
			}

			// Write updated value back to registry
			using (RegistryKey? key = Registry.CurrentUser.CreateSubKey(TortoiseSvnSubKey))
			{
				if (key == null)
				{
					Debug.LogError("Cannot get TortoiseSVN registry key to write.");
					return false;
				}
				string newValue = CreateRegistryValueFromHooks(hooks);
				key.SetValue(HooksValueName, newValue, RegistryValueKind.String);
			}
			return true;
#else
			return false;
#endif
		}
	}
}