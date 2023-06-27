using System.Text;
using System.Security.Cryptography;
using VRBuilder.Core.Internationalization;

namespace VRBuilder.TextToSpeech
{
    public static class TextToSpeechUtils
    {
        /// <summary>
        /// Returns filename which uniquly identifies the audio by Backend, Language, Voice and also the text.
        /// </summary>
        public static string GetUniqueTextToSpeechFilename(this TextToSpeechConfiguration configuration, string text, string format = "wav")
        {
            string hash = string.Format("{0}_{1}", configuration.Voice, text);
            return string.Format(@"TTS_{0}_{1}_{2}.{3}", configuration.Provider, LanguageSettings.Instance.ActiveOrDefaultLanguage, GetMd5Hash(hash).Replace("-", ""), format);
        }
        
        /// <summary>
        /// The result comes in byte array, but there are actually short values inside (ranged from short.Min to short.Max).
        /// </summary>
        public static float[] ShortsInByteArrayToFloats(byte[] shorts)
        {
            float[] floats = new float[shorts.Length / 2];

            for (int i = 0; i < floats.Length; i++)
            {
                short restoredShort = (short) ((shorts[i * 2 + 1] << 8) | (shorts[i * 2]));
                floats[i] = restoredShort / (float) short.MaxValue;
            }

            return floats;
        }
        
        private static string GetMd5Hash(string input)
        {
            using (MD5 md5Hash = MD5.Create())
            {
                byte[] buffer = Encoding.UTF8.GetBytes(input);

                // Convert the input string to a byte array and compute the hash.
                byte[] data = md5Hash.ComputeHash(buffer);

                // Create a new StringBuilder to collect the bytes
                // and create a string.
                StringBuilder sBuilder = new StringBuilder();

                // Loop through each byte of the hashed data and format each one as a hexadecimal string.
                foreach (byte @byte in data)
                {
                    sBuilder.Append(@byte.ToString("x2"));
                }

                // Return the hexadecimal string.
                return sBuilder.ToString();
            }
        }
    }
}