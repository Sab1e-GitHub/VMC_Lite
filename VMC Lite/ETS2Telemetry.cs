namespace VMC_Lite
{
    class ETS2Telemetry
    {
        public const string SharedMemoryName = @"Local\SCSTelemetry";
        public const int SharedMemorySize = 32 * 1024;
        public ETS2Telemetry() { }
        private static readonly Dictionary<string, long> offsets = new Dictionary<string, long>
        {
            { "SdkActive", 0 },
            { "Paused", 4 },
            { "BlinkerLeftActive", 1578 },
            { "BlinkerRightActive", 1579 },
            { "Wipers", 1577 },
            { "LightsParking", 1582 },
            { "LightsBeamLow", 1583 },
            { "LightsBeamHigh", 1584 },
            { "ElectricEnabled", 1575 },
            { "EngineRpm", 952},
            { "EngineRpmMax", 740}
        };
        public static long GetOffset(string memberName)
        {
            if (offsets.TryGetValue(memberName, out long offset))
            {
                return offset;
            }
            else
            {
                throw new ArgumentException($"Member name '{memberName}' not found.");
            }
        }
        public static bool IsSdkActived()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("SdkActive"));
        }
        public static bool IsPaused()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("Paused"));
        }
        public static bool IsBlinkerLeftActived()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("BlinkerLeftActive"));
        }
        public static bool IsBlinkerRightActived()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("BlinkerRightActive"));
        }
        public static bool IsElectricEnabled()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("ElectricEnabled"));
        }
        public static bool IsLightsParking()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset("LightsParking"));
        }
        /*
         public static bool Is()
        {
            return SharedMemoryReader.ReadBoolByOffset(SharedMemoryName, GetOffset(""));
        }
        */
        public static float GetEngineRpm()
        {
            return SharedMemoryReader.ReadFloatByOffset(SharedMemoryName, GetOffset("EngineRpm"));
        }
        public static float GetEngineRpmMax()
        {
            return SharedMemoryReader.ReadFloatByOffset(SharedMemoryName, GetOffset("EngineRpmMax"));
        }
    }
}
