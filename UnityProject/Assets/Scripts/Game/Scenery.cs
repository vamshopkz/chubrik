namespace Chubrik
{
    /// <summary>18.11: each world dressed like its concept art (the scenery files in Game/Scenery/).</summary>
    public static class Scenery
    {
        public static void Build(int w)
        {
            switch (w)
            {
                case 2: World2Scenery.Build(); break;
                case 3: World3Scenery.Build(); break;
                case 4: World4Scenery.Build(); break;
                case 5: World5Scenery.Build(); break;
                case 6: World6Scenery.Build(); break;
                case 7: World67Scenery.Build(); break;
            }
        }
    }
}
