using LibHac.Tools.FsSystem;
using Ryujinx.Audio.Integration;
using Ryujinx.Common.Configuration;
using Ryujinx.Common.Configuration.Multiplayer;
using Ryujinx.Graphics.GAL;
using Ryujinx.HLE.FileSystem;
using Ryujinx.HLE.HOS;
using Ryujinx.HLE.HOS.Services.Account.Acc;
using Ryujinx.HLE.HOS.SystemState;
using Ryujinx.HLE.UI;
using System;

namespace Ryujinx.HLE
{
    /// <summary>
    /// HLE configuration.
    /// </summary>
    public class HleConfiguration
    {
        /// <summary>
        /// The virtual file system used by the FS service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal VirtualFileSystem VirtualFileSystem { get; private set; }

        /// <summary>
        /// The manager for handling a LibHac Horizon instance.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal LibHacHorizonManager LibHacHorizonManager { get; private set; }

        /// <summary>
        /// The account manager used by the account service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal AccountManager AccountManager { get; private set; }

        /// <summary>
        /// The content manager used by the NCM service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal ContentManager ContentManager { get; private set; }

        /// <summary>
        /// The persistent information between run for multi-application capabilities.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        public UserChannelPersistence UserChannelPersistence { get; private set; }

        /// <summary>
        /// The GPU renderer to use for all GPU operations.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal IRenderer GpuRenderer { get; private set; }

        /// <summary>
        /// The audio device driver to use for all audio operations.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal IHardwareDeviceDriver AudioDeviceDriver { get; private set; }

        /// <summary>
        /// The handler for various UI related operations needed outside of HLE.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal IHostUIHandler HostUIHandler { get; private set; }

        /// <summary>
        /// Control the memory configuration used by the emulation context.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly MemoryConfiguration MemoryConfiguration;

        /// <summary>
        /// The system language to use in the settings service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly SystemLanguage SystemLanguage;

        /// <summary>
        /// The system region to use in the settings service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly RegionCode Region;

        /// <summary>
        /// Control the initial state of the present interval in the SurfaceFlinger service (previously Vsync).
        /// </summary>
        internal readonly VSyncMode VSyncMode;

        /// <summary>
        /// Control the custom VSync interval, if enabled and active.
        /// </summary>
        internal readonly int CustomVSyncInterval;

        /// <summary>
        /// Control the initial state of the docked mode.
        /// </summary>
        internal readonly bool EnableDockedMode;

        /// <summary>
        /// Control if the Profiled Translation Cache (PTC) should be used.
        /// </summary>
        internal readonly bool EnablePtc;
        
        /// <summary>
        /// Control the arbitrary scalar applied to emulated CPU tick timing.
        /// </summary>
        public long TickScalar { get; set; }

        /// <summary>
        /// Control if the guest application should be told that there is a Internet connection available.
        /// </summary>
        public bool EnableInternetAccess { internal get; set; }

        /// <summary>
        /// Control LibHac's integrity check level.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly IntegrityCheckLevel FsIntegrityCheckLevel;

        /// <summary>
        /// Control LibHac's global access logging level. Value must be between 0 and 3.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly int FsGlobalAccessLogMode;

        /// <summary>
        /// The system time offset to apply to the time service steady and local clocks.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly long SystemTimeOffset;

        /// <summary>
        /// The system timezone used by the time service.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        internal readonly string TimeZone;

        /// <summary>
        /// Type of the memory manager used on CPU emulation.
        /// </summary>
        public MemoryManagerMode MemoryManagerMode { internal get; set; }

        /// <summary>
        /// Control the initial state of the ignore missing services setting.
        /// If this is set to true, when a missing service is encountered, it will try to automatically handle it instead of throwing an exception.
        /// </summary>
        /// TODO: Update this again.
        public bool IgnoreMissingServices { internal get; set; }

        /// <summary>
        /// Aspect Ratio applied to the renderer window by the SurfaceFlinger service.
        /// </summary>
        public AspectRatio AspectRatio { get; set; }

        /// <summary>
        /// The audio volume level.
        /// </summary>
        public float AudioVolume { get; set; }

        /// <summary>
        /// Use Hypervisor over JIT if available.
        /// </summary>
        internal readonly bool UseHypervisor;

        /// <summary>
        /// Multiplayer LAN Interface ID (device GUID)
        /// </summary>
        public string MultiplayerLanInterfaceId { internal get; set; }

        /// <summary>
        /// Multiplayer Mode
        /// </summary>
        public MultiplayerMode MultiplayerMode { internal get; set; }

        /// <summary>
        /// Disable P2P mode
        /// </summary>
        public bool MultiplayerDisableP2p { internal get; set; }

        /// <summary>
        /// Multiplayer Passphrase
        /// </summary>
        public string MultiplayerLdnPassphrase { internal get; set; }

        /// <summary>
        /// LDN Server
        /// </summary>
        public string MultiplayerLdnServer { internal get; set; }

        /// <summary>
        /// An action called when HLE force a refresh of output after docked mode changed.
        /// </summary>
        public Action RefreshInputConfig { internal get; set; }
        
        /// <summary>
        ///     The desired hacky workarounds.
        /// </summary>
        /// <remarks>This cannot be changed after <see cref="Switch"/> instantiation.</remarks>
        public EnabledDirtyHack[] Hacks { internal get; set; }

        public HleConfiguration(MemoryConfiguration memoryConfiguration,
                                SystemLanguage systemLanguage,
                                RegionCode region,
                                VSyncMode vSyncMode,
                                bool enableDockedMode,
                                bool enablePtc,
                                long tickScalar,
                                bool enableInternetAccess,
                                IntegrityCheckLevel fsIntegrityCheckLevel,
                                int fsGlobalAccessLogMode,
                                long systemTimeOffset,
                                string timeZone,
                                MemoryManagerMode memoryManagerMode,
                                bool ignoreMissingServices,
                                AspectRatio aspectRatio,
                                float audioVolume,
                                bool useHypervisor,
                                string multiplayerLanInterfaceId,
                                MultiplayerMode multiplayerMode,
                                bool multiplayerDisableP2p,
                                string multiplayerLdnPassphrase,
                                string multiplayerLdnServer,
                                int customVSyncInterval,
                                EnabledDirtyHack[] dirtyHacks = null)
        {
            MemoryConfiguration = memoryConfiguration;
            SystemLanguage = systemLanguage;
            Region = region;
            VSyncMode = vSyncMode;
            CustomVSyncInterval = customVSyncInterval;
            EnableDockedMode = enableDockedMode;
            EnablePtc = enablePtc;
            TickScalar = tickScalar;
            EnableInternetAccess = enableInternetAccess;
            FsIntegrityCheckLevel = fsIntegrityCheckLevel;
            FsGlobalAccessLogMode = fsGlobalAccessLogMode;
            SystemTimeOffset = systemTimeOffset;
            TimeZone = timeZone;
            MemoryManagerMode = memoryManagerMode;
            IgnoreMissingServices = ignoreMissingServices;
            AspectRatio = aspectRatio;
            AudioVolume = audioVolume;
            UseHypervisor = useHypervisor;
            MultiplayerLanInterfaceId = multiplayerLanInterfaceId;
            MultiplayerMode = multiplayerMode;
            MultiplayerDisableP2p = multiplayerDisableP2p;
            MultiplayerLdnPassphrase = multiplayerLdnPassphrase;
            MultiplayerLdnServer = multiplayerLdnServer;
            Hacks = dirtyHacks ?? [];
        }

        /// <summary>
        /// Set the pre-configured services to use for this <see cref="HleConfiguration"/> instance.
        /// </summary>
        public HleConfiguration Configure(
            VirtualFileSystem virtualFileSystem,
            LibHacHorizonManager libHacHorizonManager,
            ContentManager contentManager,
            AccountManager accountManager,
            UserChannelPersistence userChannelPersistence,
            IRenderer gpuRenderer,
            IHardwareDeviceDriver audioDeviceDriver,
            IHostUIHandler hostUIHandler
        )
        {
            VirtualFileSystem = virtualFileSystem;
            LibHacHorizonManager = libHacHorizonManager;
            AccountManager = accountManager;
            ContentManager = contentManager;
            UserChannelPersistence = userChannelPersistence;
            GpuRenderer = gpuRenderer;
            AudioDeviceDriver = audioDeviceDriver;
            HostUIHandler = hostUIHandler;
            return this;
        }
    }
}
