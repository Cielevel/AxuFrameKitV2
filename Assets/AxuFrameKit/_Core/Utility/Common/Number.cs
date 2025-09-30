
namespace AxuFrameKit._Core.Utility.Common
{
    public static class Number
    {
        private static readonly System.Random Rd = new System.Random();
        
        public const int Int0 = 0;
        public const int Int1 = 1;
        public const int Int2 = 2;
        public const int Int3 = 3;
        public const int Int4 = 4;
        public const int Int5 = 5;
        public const int Int6 = 6;
        public const int Int7 = 7;
        public const int Int8 = 8;
        public const int Int9 = 9;
        public const int Int10 = 10;

        public const float Half = 0.5f;
        
        public const float FloatDot0 = 0.0f;
        public const float FloatDot1 = 0.1f;
        public const float FloatDot2 = 0.2f;
        public const float FloatDot3 = 0.3f;
        public const float FloatDot4 = 0.4f;
        public const float FloatDot5 = 0.5f;
        public const float FloatDot6 = 0.6f;
        public const float FloatDot7 = 0.7f;
        public const float FloatDot8 = 0.8f;
        public const float FloatDot9 = 0.9f;

        public static float Random(float min, float max) => min + (float)Rd.NextDouble() * (max - min);
        
        public static int Random(int min, int max) => Rd.Next(min, max);

        public static float Random0(float max) => Random(0f, max);
        
        public static int Random0(int max) => Rd.Next(max);
    }
}