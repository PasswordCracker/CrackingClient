using ClientCracking.User;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ClientCracking
{
    public class Cracking
    {
        /// <summary>
        /// The algorithm used for encryption.
        /// Must be exactly the same algorithm that was used to encrypt the passwords in the password file
        /// </summary>
        private readonly HashAlgorithm _messageDigest;

        public Cracking()
        {
            _messageDigest = new SHA1CryptoServiceProvider();
            //_messageDigest = new MD5CryptoServiceProvider();
            // seems to be same speed
        }
        //public static List<string> ReadDictionary()
        //{
        //    List<string> wordlist = new List<string>();

        //    using (FileStream fs = new FileStream(@"C:\Users\Radu\Source\Repos\CrackingClient\ClientCracking\Temp\webster-dictionary.txt", FileMode.Open, FileAccess.Read))
        //    using (StreamReader dictionary = new StreamReader(fs))
        //    {
        //        while (!dictionary.EndOfStream )
        //        {
        //            string dictionaryEntry = dictionary.ReadLine();
        //            wordlist.Add(dictionaryEntry);
        //        }
        //    }
        //    return wordlist;
        //}
        //public static Dictionary<string, string> ReadPasswords()
        //{
        //    Dictionary<string, string> credentialDictionary = new Dictionary<string, string>();

        //    using (FileStream fs = new FileStream(@"C:\Users\Radu\Source\Repos\CrackingClient\ClientCracking\Temp\passwords.txt", FileMode.Open, FileAccess.Read))
        //    using (StreamReader passwords = new StreamReader(fs))
        //    {
        //        while (!passwords.EndOfStream)
        //        {
        //            string passwordsEntry = passwords.ReadLine();
        //            string[] credentials = passwordsEntry.Split(":");
        //            credentialDictionary.Add(credentials[0].ToString(), credentials[1].ToString());

        //        }
        //    }
        //    return credentialDictionary;
        //}
        /// <summary>
        /// Runs the password cracking algorithm
        /// </summary>
        public Dictionary<string, string> RunCracking()
        {

            Dictionary<string, string> userInfos = Program.passwords;

            Dictionary<string, string> result = new Dictionary<string, string>();

            
            foreach(string word in Program.chunk)
            {
                 Dictionary<string, string> partialResult = CheckWordWithVariations(word, userInfos);
                 result = Merge(result, partialResult);
            }
            return result;
        }

        /// <summary>
        /// Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private Dictionary<string, string> CheckWordWithVariations(String dictionaryEntry, Dictionary<string, string> userInfos)
        {
            Dictionary<string, string> result = new Dictionary<string, string>(); //might be empty

            String possiblePassword = dictionaryEntry;
            Dictionary<string, string> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result = Merge(result, partialResult);

            String possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            Dictionary<string, string> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result = Merge(result, partialResultUpperCase);

            String possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            Dictionary<string, string> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result = Merge(result, partialResultCapitalized);

            String possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            Dictionary<string, string> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result = Merge(result, partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                Dictionary<string, string> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result = Merge(result, partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                Dictionary<string, string> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result = Merge(result, partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    Dictionary<string, string> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result = Merge(result, partialResultStartEndDigit);
                }
            }

            return result;
        }

        /// <summary>
        /// Checks a single word (or rather a variation of a word): Encrypts and compares to all entries in the password file
        /// </summary>
        /// <param name="userInfos"></param>
        /// <param name="possiblePassword">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private Dictionary<string, string> CheckSingleWord(Dictionary<string, string> userInfos, String possiblePassword)
        {
            char[] charArray = possiblePassword.ToCharArray();
            byte[] passwordAsBytes = Array.ConvertAll(charArray, PasswordFileHandler.GetConverter());

            byte[] encryptedPassword = _messageDigest.ComputeHash(passwordAsBytes);
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            Dictionary<string, string> results = new Dictionary<string, string>();

            foreach(KeyValuePair<string, string> userInfo in userInfos)
            {
                if (CompareBytes(Convert.FromBase64String(userInfo.Value), encryptedPassword))  //compares byte arrays
                {
                    results.Add(userInfo.Key, possiblePassword);
                    Console.WriteLine(userInfo.Key + " " + possiblePassword);
                }
            }
            return results;
        }

        /// <summary>
        /// Compares to byte arrays. Encrypted words are byte arrays
        /// </summary>
        /// <param name="firstArray"></param>
        /// <param name="secondArray"></param>
        /// <returns></returns>
        private static bool CompareBytes(IList<byte> firstArray, IList<byte> secondArray)
        {
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("firstArray");
            //}
            //if (secondArray == null)
            //{
            //    throw new ArgumentNullException("secondArray");
            //}
            if (firstArray.Count != secondArray.Count)
            {
                return false;
            }
            for (int i = 0; i < firstArray.Count; i++)
            {
                if (firstArray[i] != secondArray[i])
                    return false;
            }
            return true;
        }
        //adds 2 dictionaries together
        private static Dictionary<string, string> Merge(Dictionary<string, string> dict1, Dictionary<string, string> dict2)
        {
            foreach (KeyValuePair<string, string> pair in dict2)
            {
                dict1.Add(pair.Key, pair.Value);
            }
            return dict1;
        }

    }
}
