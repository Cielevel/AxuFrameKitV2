using UnityEngine;

namespace AxuFrameKit.Common.Utility.Static
{
    public static class EnumeratorWait
    {
        public static readonly WaitForFixedUpdate FixedUpdate = new();
        public static readonly WaitForEndOfFrame EndOfFrame = new();

        public static readonly WaitForSeconds Dot1 = new(.1f);
        public static readonly WaitForSeconds Dot2 = new(.2f);
        public static readonly WaitForSeconds Dot3 = new(.3f);
        public static readonly WaitForSeconds Dot4 = new(.4f);
        public static readonly WaitForSeconds Dot5 = new(.5f);
        public static readonly WaitForSeconds Dot6 = new(.6f);
        public static readonly WaitForSeconds Dot7 = new(.7f);
        public static readonly WaitForSeconds Dot8 = new(.8f);
        public static readonly WaitForSeconds Dot9 = new(.9f);

        public static readonly WaitForSeconds S1 = new(1f);
        public static readonly WaitForSeconds S2 = new(2f);
        public static readonly WaitForSeconds S3 = new(3f);
        public static readonly WaitForSeconds S4 = new(4f);
        public static readonly WaitForSeconds S5 = new(5f);
        public static readonly WaitForSeconds S6 = new(6f);
        public static readonly WaitForSeconds S7 = new(7f);
        public static readonly WaitForSeconds S8 = new(8f);
        public static readonly WaitForSeconds S9 = new(9f);
    }
}