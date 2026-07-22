using UnityModManagerNet;

namespace BrickLoco
{
    /// <summary>
    /// Thin adapter over UMM's ModLogger that keeps the LogInfo/LogWarning/LogError vocabulary
    /// the codebase already uses. Output lands in the UMM in-game console (Ctrl+F10) and in
    /// DerailValley_Data/Managed/UnityModManager/Log.txt, prefixed with the mod id.
    /// </summary>
    internal sealed class ModLog
    {
        private readonly UnityModManager.ModEntry.ModLogger logger;

        public ModLog(UnityModManager.ModEntry.ModLogger logger)
        {
            this.logger = logger;
        }

        public void LogInfo(string message)
        {
            logger.Log(message);
        }

        public void LogWarning(string message)
        {
            logger.Warning(message);
        }

        public void LogError(string message)
        {
            logger.Error(message);
        }
    }
}
