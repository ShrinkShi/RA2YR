using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace RA2YR.Core.Content
{
    public static class RepositoryPathPolicy
    {
        private static readonly StringComparison PathComparison =
            Path.DirectorySeparatorChar == '\\'
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

        public static string NormalizeAbsolutePath(string path, string baseDirectory = null)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A non-empty path is required.", nameof(path));
            }

            if (IsWindowsDriveRelativePath(path))
            {
                throw new ArgumentException(
                    "Windows drive-relative paths are not accepted; use an absolute path or a path relative to the configuration directory.",
                    nameof(path));
            }

            string combined = Path.IsPathRooted(path)
                ? path
                : Path.Combine(
                    baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory)),
                    path);

            return TrimTrailingDirectorySeparators(Path.GetFullPath(combined));
        }

        public static bool IsInsideOrEqual(string candidatePath, string repositoryRoot)
        {
            string candidate = NormalizeAbsolutePath(candidatePath);
            string repository = NormalizeAbsolutePath(repositoryRoot);

            if (string.Equals(candidate, repository, PathComparison))
            {
                return true;
            }

            string repositoryPrefix = EndsWithDirectorySeparator(repository)
                ? repository
                : repository + Path.DirectorySeparatorChar;
            return candidate.StartsWith(repositoryPrefix, PathComparison);
        }

        public static bool OverlapsRepository(string candidatePath, string repositoryRoot)
        {
            return IsInsideOrEqual(candidatePath, repositoryRoot) ||
                   IsInsideOrEqual(repositoryRoot, candidatePath);
        }

        public static bool TryDetermineOverlap(
            string firstPath,
            string secondPath,
            out bool overlaps,
            out string failureReason)
        {
            if (OverlapsRepository(firstPath, secondPath))
            {
                overlaps = true;
                failureReason = null;
                return true;
            }

            string firstIdentity;
            string secondIdentity;
            if (!TryGetComparableIdentity(firstPath, out firstIdentity, out failureReason) ||
                !TryGetComparableIdentity(secondPath, out secondIdentity, out failureReason))
            {
                overlaps = false;
                return false;
            }

            overlaps = IsInsideOrEqualIdentity(firstIdentity, secondIdentity) ||
                       IsInsideOrEqualIdentity(secondIdentity, firstIdentity);
            failureReason = null;
            return true;
        }

        public static bool TryFindUnsupportedAlias(string path, out string reason)
        {
            string fullPath = NormalizeAbsolutePath(path);
            if (Path.DirectorySeparatorChar != '\\')
            {
                reason = null;
                return false;
            }

            if (UsesWindowsDeviceNamespace(fullPath))
            {
                reason = "Windows device-namespace paths are not accepted.";
                return true;
            }

            if (ContainsPotentialShortNameSegment(fullPath))
            {
                reason = "A possible Windows 8.3 short-name segment was detected; use the long path.";
                return true;
            }

            string root = Path.GetPathRoot(fullPath);
            if (string.IsNullOrEmpty(root))
            {
                reason = "The Windows path root could not be identified.";
                return true;
            }

            if (root.StartsWith("\\\\", StringComparison.Ordinal))
            {
                reason = "UNC paths are conservatively rejected because their final identity cannot be verified portably.";
                return true;
            }

            string drive = root.TrimEnd('\\');
            string target;
            if (!TryQueryDosDevice(drive, out target))
            {
                reason = "The Windows drive mapping could not be verified.";
                return true;
            }

            if (target.StartsWith("\\??\\", StringComparison.Ordinal))
            {
                reason = "SUBST and other DOS-device alias mappings are not accepted.";
                return true;
            }

            reason = null;
            return false;
        }

        public static bool ContainsExistingReparsePoint(string path, out string reparsePointPath)
        {
            return ContainsExistingReparsePoint(
                path,
                File.GetAttributes,
                out reparsePointPath);
        }

        internal static bool ContainsExistingReparsePoint(
            string path,
            Func<string, FileAttributes> getAttributes,
            out string reparsePointPath)
        {
            if (getAttributes == null)
            {
                throw new ArgumentNullException(nameof(getAttributes));
            }

            string currentPath = NormalizeAbsolutePath(path);
            while (!string.IsNullOrEmpty(currentPath))
            {
                try
                {
                    if ((getAttributes(currentPath) & FileAttributes.ReparsePoint) != 0)
                    {
                        reparsePointPath = currentPath;
                        return true;
                    }
                }
                catch (FileNotFoundException)
                {
                    // A missing leaf is allowed; existing ancestors still need inspection.
                }
                catch (DirectoryNotFoundException)
                {
                    // Walk upward until an existing ancestor is found.
                }

                string parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath) ||
                    string.Equals(parentPath, currentPath, PathComparison))
                {
                    break;
                }

                currentPath = parentPath;
            }

            reparsePointPath = null;
            return false;
        }

        private static bool TryGetComparableIdentity(
            string path,
            out string identity,
            out string failureReason)
        {
            string fullPath = NormalizeAbsolutePath(path);
            string aliasReason;
            if (TryFindUnsupportedAlias(fullPath, out aliasReason))
            {
                identity = null;
                failureReason = aliasReason;
                return false;
            }

            if (Path.DirectorySeparatorChar != '\\')
            {
                identity = fullPath;
                failureReason = null;
                return true;
            }

            string root = Path.GetPathRoot(fullPath);
            string target;
            if (!TryQueryDosDevice(root.TrimEnd('\\'), out target))
            {
                identity = null;
                failureReason = "The Windows drive identity could not be resolved.";
                return false;
            }

            string remainder = fullPath.Substring(root.Length).TrimStart('\\');
            identity = TrimTrailingDirectorySeparators(target) +
                       (remainder.Length == 0 ? string.Empty : "\\" + remainder);
            failureReason = null;
            return true;
        }

        private static bool IsInsideOrEqualIdentity(string candidate, string container)
        {
            if (string.Equals(candidate, container, PathComparison))
            {
                return true;
            }

            string prefix = EndsWithDirectorySeparator(container)
                ? container
                : container + Path.DirectorySeparatorChar;
            return candidate.StartsWith(prefix, PathComparison);
        }

        private static bool UsesWindowsDeviceNamespace(string path)
        {
            return path.StartsWith("\\\\?\\", StringComparison.Ordinal) ||
                   path.StartsWith("\\\\.\\", StringComparison.Ordinal) ||
                   path.StartsWith("\\??\\", StringComparison.Ordinal);
        }

        private static bool IsWindowsDriveRelativePath(string path)
        {
            return Path.DirectorySeparatorChar == '\\' &&
                   path.Length >= 2 &&
                   path[1] == ':' &&
                   ((path[0] >= 'A' && path[0] <= 'Z') ||
                    (path[0] >= 'a' && path[0] <= 'z')) &&
                   (path.Length == 2 || (path[2] != '\\' && path[2] != '/'));
        }

        private static bool ContainsPotentialShortNameSegment(string path)
        {
            string[] segments = path.Split('\\');
            foreach (string segment in segments)
            {
                int tilde = segment.LastIndexOf('~');
                if (tilde < 0 || tilde == segment.Length - 1)
                {
                    continue;
                }

                bool hasDigit = false;
                for (int index = tilde + 1; index < segment.Length; index++)
                {
                    char value = segment[index];
                    if (value == '.')
                    {
                        break;
                    }

                    if (value < '0' || value > '9')
                    {
                        hasDigit = false;
                        break;
                    }

                    hasDigit = true;
                }

                if (hasDigit)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool TryQueryDosDevice(string drive, out string target)
        {
            var buffer = new StringBuilder(32768);
            uint length = QueryDosDevice(drive, buffer, buffer.Capacity);
            if (length == 0)
            {
                target = null;
                return false;
            }

            int terminator = buffer.ToString().IndexOf('\0');
            target = terminator >= 0
                ? buffer.ToString(0, terminator)
                : buffer.ToString();
            return target.Length > 0;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern uint QueryDosDevice(
            string deviceName,
            StringBuilder targetPath,
            int maximumLength);

        private static string TrimTrailingDirectorySeparators(string path)
        {
            string root = Path.GetPathRoot(path);
            int minimumLength = string.IsNullOrEmpty(root) ? 0 : root.Length;
            int length = path.Length;

            while (length > minimumLength &&
                   (path[length - 1] == Path.DirectorySeparatorChar ||
                    path[length - 1] == Path.AltDirectorySeparatorChar))
            {
                length--;
            }

            return length == path.Length ? path : path.Substring(0, length);
        }

        private static bool EndsWithDirectorySeparator(string path)
        {
            return path.Length > 0 &&
                   (path[path.Length - 1] == Path.DirectorySeparatorChar ||
                    path[path.Length - 1] == Path.AltDirectorySeparatorChar);
        }
    }
}
