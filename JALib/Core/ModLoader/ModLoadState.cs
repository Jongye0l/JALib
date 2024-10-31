namespace JALib.Core.ModLoader;

enum ModLoadState {
    None,
    Initializing,
    Downloading,
    Loading,
    Loaded,
    Failed,
    DependencyFailed,
    Disabled,
    DependencyDisabled,
    NeedRestart,
}