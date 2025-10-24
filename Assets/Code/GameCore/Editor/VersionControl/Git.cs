using System.IO;
using Debug = UnityEngine.Debug;

#nullable enable

namespace Code.Framework.Editor.VersionControl
{
    public class Git : Vcs
    {
        protected override string Executable => "git";

        private const string PreCommitHookPath = "../.git/hooks/pre-commit";
        private const string PreCommitHook = "#!/bin/sh\n" +
                                             "./Infrastructure/Services/ValidationHook/ValidationHook/ValidationHook/bin/Debug/ValidationHook.exe git commit\n";

        protected override bool CheckIsDead()
        {
            bool isSuccess = Execute("ls-files ../.gitignore", out string[]? stdOut);
            if (isSuccess && stdOut is {Length: > 0})
            {
                Debug.Log("Git vcs found!");
                return false;
            }
            return true;
        }

        public override bool ActivateHooks()
        {
            if (IsDead)
            {
                return false;
            }

            if (!DeactivateHooks())
            {
                return false;
            }

            File.WriteAllText(PreCommitHookPath, PreCommitHook);
            if (File.Exists(PreCommitHookPath))
            {
                Debug.Log("Git hooks activated");
                return true;
            }
            Debug.LogError($"Failed to install hook file at: {PreCommitHookPath}");
            return false;
        }

        public override bool DeactivateHooks()
        {
            if (IsDead)
            {
                return false;
            }

            if (File.Exists(PreCommitHookPath))
            {
                File.Delete(PreCommitHookPath);
            }
            if (File.Exists(PreCommitHookPath))
            {
                Debug.LogError($"Failed to remove hook file at: {PreCommitHookPath}");
                return false;
            }
            Debug.Log("Git hooks removed.");
            return true;
        }

        public override bool Move(string srcPath, string dstPath)
        {
            if (IsDead || !IsTracked(out bool isTracked, srcPath))
            {
                return false;
            }

            if (!isTracked)
            {
                Debug.LogError($"File is not under git version control yet: {srcPath}");
                return false;
            }

            MakeSureDirExists(dstPath);
            return Execute($"mv \"{srcPath}\" \"{dstPath}\"", out _);
        }

        protected override bool IsTracked(out bool isTracked, string path)
        {
            isTracked = false;
            if (IsDead)
            {
                return false;
            }

            bool isSuccess = Execute($"ls-files \"{path}\"", out string[]? stdOut);
            if (isSuccess)
            {
                isTracked = stdOut is {Length: > 0};
            }
            return isSuccess;
        }
    }
}