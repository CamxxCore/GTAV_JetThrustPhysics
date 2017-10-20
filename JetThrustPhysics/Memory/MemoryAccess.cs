namespace JetBlast.Memory
{
    public struct MemoryAccess
    {
        public static int ThrottleOffset { get; private set; }

        public static int GearOffset { get; private set; }

        public static bool Init()
        {
           var result = Pattern.Search("F3 0F 11 83 ?? ?? ?? ?? 75 09").First();

            if (result == null)
                return false;

            ThrottleOffset = result.GetInt(4);

            result = Pattern.Search("0F 59 D6 66 0F 70 CA ??").First();

            if (result == null)
                return false;

            GearOffset = result.GetInt(-15);

            GTA.UI.ShowSubtitle(ThrottleOffset.ToString("X"));

            return true;
        }
    }
}
