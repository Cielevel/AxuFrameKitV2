using System.Text;
using AxuFrameKit._Core.Extension;

namespace AxuFrameKit._Core.Utility.RandomX
{
    public static class RandomId
    {
        private const string Number = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
        public static string Get(int length)
        {
            StringBuilder builder = new();
            for (int i = 0; i < length; i++)
                builder.Append(Number.GetRandom());

            return builder.ToString();
        }
        public static string[] Get(int length, int count)
        {
            string[] result = new string[count];
            for (int i = 0; i < count; i++)
                result[i] = Get(length);
            return result;
        }
    }
}