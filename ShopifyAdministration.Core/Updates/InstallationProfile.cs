using System;

namespace ShopifyAdministration.Core.Updates
{
    /// <summary>Defines the installed update channel, independently of Shopify configuration.</summary>
    public enum UpdateChannel
    {
        /// <summary>Qualified stable production releases.</summary>
        Stable,
        /// <summary>Qualified opt-in preview releases.</summary>
        Preview,
        /// <summary>Unqualified CI test packages without automatic updates.</summary>
        Candidate,
        /// <summary>Source/development builds without an enabled channel.</summary>
        Development
    }
    /// <summary>Defines which installer or package manager owns an installation.</summary>
    public enum DeploymentKind
    {
        /// <summary>A complete portable archive installed directly or by the per-user helper.</summary>
        Portable,
        /// <summary>A Windows ClickOnce deployment owned by the OS handler.</summary>
        ClickOnce,
        /// <summary>An installation whose package manager owns updates.</summary>
        PackageManager
    }
    /// <summary>Describes the qualified installation used to select update metadata.</summary>
    public sealed class InstallationProfile
    {
        /// <summary>Gets the public application identity shared by package and feed.</summary>
        public const string AppId = "shopify-administration";
        /// <summary>Creates a profile; only explicitly qualified stable/preview installations check automatically.</summary>
        public InstallationProfile(string version, string rid, UpdateChannel channel, DeploymentKind deployment, bool qualified)
        {
            if (!Enum.IsDefined(typeof(UpdateChannel), channel) || !Enum.IsDefined(typeof(DeploymentKind), deployment)) throw new ArgumentException("Unknown installation channel/deployment");
            if (!UpdateVersion.TryParse(version, out var parsed)) throw new ArgumentException("Invalid application version", nameof(version));
            Version = parsed; DisplayVersion = version; Rid = rid; Channel = channel; Deployment = deployment;
            CanCheck = qualified && (channel == UpdateChannel.Stable || channel == UpdateChannel.Preview) &&
                (rid == "win-x64" || rid == "linux-x64" || rid == "osx-arm64") && (deployment != DeploymentKind.ClickOnce || rid == "win-x64");
        }
        /// <summary>Gets the parsed current version.</summary>
        public UpdateVersion Version { get; }
        /// <summary>Gets the current operator-facing version.</summary>
        public string DisplayVersion { get; }
        /// <summary>Gets the exact OS/architecture runtime identifier.</summary>
        public string Rid { get; }
        /// <summary>Gets the selected release channel.</summary>
        public UpdateChannel Channel { get; }
        /// <summary>Gets which deployment mechanism owns updates.</summary>
        public DeploymentKind Deployment { get; }
        /// <summary>Gets whether this installation is qualified for a trusted update channel.</summary>
        public bool CanCheck { get; }
        /// <summary>Gets the non-secret preference scope for skipped versions.</summary>
        public string Scope => AppId + "/" + Channel + "/" + Rid + "/" + Deployment;
    }
}
