using System;

namespace RealmForge
{
    /// <summary>
    /// Static utility class providing a constant fields for the paths to the most common images, icons, and other resorces.
    /// </summary>
    public sealed class StandardResources
    {
        private StandardResources()
        {
        }
        public const string RealmForgeIcon = "RealmForge.ico";
        public const string RealmForgeTitle = MediaDirectory + "Logos/RealmForgeTitle.png";
        public const string RealmForgeTitleSmall = MediaDirectory + "Logos/RealmForgeTitleSmall.png";
        public const string RealmForgeIcon2 = MediaDirectory + "Icons/RealmForge2.ico";
        public const string AxiomIcon = MediaDirectory + "Icons/AxiomIcon.ico";
        public const string RealmForgeLogo = MediaDirectory + "Logos/RealmForge.png";
        public const string AxiomLogo = MediaDirectory + "Logos/AxiomLogo.png";
        public const string LauncherBackground = "RFDemoGameLauncher.png";
        public const string MediaDirectory = "../Media/";
        public const string ReadmeFile = "../Readme.txt";

    }
}
