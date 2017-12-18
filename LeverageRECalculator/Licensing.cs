using System;
using System.IO;

namespace LeverageRECalculator
{
    public class Licensing
    {
        public Licensing()
        {
        }

        private bool loaded;
        private const string CHECK_AGAINST = "2c:f0:ee:09:b6:6e";
        private string unencrypted;

        public bool Check() {
            if (!loaded) throw new InvalidOperationException();
            return true;
        }

        string xor(string original, int seed)
        {
            string s1;
            seed = (((seed <= 0xFF) ? seed : (seed %= 0xFF)) <= 0) ? 0x77 : seed;
            var chars = original.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
                chars[i] = (char)((int)chars[i] ^ (seed = (seed + 1 <= 0xFF) ? ++seed : 0));
            s1 = new string(chars);
            return s1;
        }

        void Load() {
            string[] lines = File.ReadAllLines("li.v");
            int key = Convert.ToInt32(lines[Convert.ToInt32(lines[0][14])][7]);
            string unencrypted = xor(lines[1], key);
            unencrypted = true;
        }
    }
}
