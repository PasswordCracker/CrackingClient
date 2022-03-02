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
        public static List<string> ReadDictionary()
        {
            List<string> wordlist = new List<string>();

            using (FileStream fs = new FileStream("webster-dictionary.txt", FileMode.Open, FileAccess.Read))
            using (StreamReader dictionary = new StreamReader(fs))
            {
                while (!dictionary.EndOfStream && wordlist.Count <= 1000)
                {
                    string dictionaryEntry = dictionary.ReadLine();
                    wordlist.Add(dictionaryEntry);
                }
            }
            return wordlist;
        }
        /// <summary>
        /// Runs the password cracking algorithm
        /// </summary>
        public Dictionary<string, string> RunCracking()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();

            List<UserInfo> userInfos = new List<UserInfo>();
            foreach (var pair in Program.passwords)
            {
                UserInfo user = new UserInfo();
                user.Username = pair.Key;
                user.HashedPassword = pair.Value;
                userInfos.Add(user);
            }
            Console.WriteLine("passwords opened");

            List<UserInfo> result = new List<UserInfo>();

            List<string> testchunk = ReadDictionary();
            foreach(string word in testchunk)
            {
                string dictionaryEntry = word;
                IEnumerable<UserInfo> partialResult = CheckWordWithVariations(dictionaryEntry, userInfos);
                result.AddRange(partialResult);
            }

            stopwatch.Stop();
            Console.WriteLine(string.Join(", ", result));
            Console.WriteLine("Out of {0} password {1} was found ", userInfos.Count, result.Count);
            Console.WriteLine();
            Console.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);

            Dictionary<string, string> FinalResult = new Dictionary<string, string>();
            foreach(var userinfo in result)
            {
                FinalResult.Add(userinfo.Username, userinfo.ClearTextPassword);
            }
            return FinalResult;
        }

        /// <summary>
        /// Generates a lot of variations, encrypts each of the and compares it to all entries in the password file
        /// </summary>
        /// <param name="dictionaryEntry">A single word from the dictionary</param>
        /// <param name="userInfos">List of (username, encrypted password) pairs from the password file</param>
        /// <returns>A list of (username, readable password) pairs. The list might be empty</returns>
        private IEnumerable<UserInfo> CheckWordWithVariations(string dictionaryEntry, List<UserInfo> userInfos)
        {
            List<UserInfo> result = new List<UserInfo>(); //might be empty

            string possiblePassword = dictionaryEntry;
            IEnumerable<UserInfo> partialResult = CheckSingleWord(userInfos, possiblePassword);
            result.AddRange(partialResult);

            string possiblePasswordUpperCase = dictionaryEntry.ToUpper();
            IEnumerable<UserInfo> partialResultUpperCase = CheckSingleWord(userInfos, possiblePasswordUpperCase);
            result.AddRange(partialResultUpperCase);

            string possiblePasswordCapitalized = StringUtilities.Capitalize(dictionaryEntry);
            IEnumerable<UserInfo> partialResultCapitalized = CheckSingleWord(userInfos, possiblePasswordCapitalized);
            result.AddRange(partialResultCapitalized);

            string possiblePasswordReverse = StringUtilities.Reverse(dictionaryEntry);
            IEnumerable<UserInfo> partialResultReverse = CheckSingleWord(userInfos, possiblePasswordReverse);
            result.AddRange(partialResultReverse);

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordEndDigit = dictionaryEntry + i;
                IEnumerable<UserInfo> partialResultEndDigit = CheckSingleWord(userInfos, possiblePasswordEndDigit);
                result.AddRange(partialResultEndDigit);
            }

            for (int i = 0; i < 100; i++)
            {
                String possiblePasswordStartDigit = i + dictionaryEntry;
                IEnumerable<UserInfo> partialResultStartDigit = CheckSingleWord(userInfos, possiblePasswordStartDigit);
                result.AddRange(partialResultStartDigit);
            }

            for (int i = 0; i < 10; i++)
            {
                for (int j = 0; j < 10; j++)
                {
                    String possiblePasswordStartEndDigit = i + dictionaryEntry + j;
                    IEnumerable<UserInfo> partialResultStartEndDigit = CheckSingleWord(userInfos, possiblePasswordStartEndDigit);
                    result.AddRange(partialResultStartEndDigit);
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
        private IEnumerable<UserInfo> CheckSingleWord(IEnumerable<UserInfo> userInfos, string possiblePassword)
        {
            char[] charArrayPossible = possiblePassword.ToCharArray();
            byte[] passwordAsBytesPossible = Array.ConvertAll(charArrayPossible, PasswordFileHandler.GetConverter());

            byte[] encryptedPasswordPossible = _messageDigest.ComputeHash(passwordAsBytesPossible);
            //string encryptedPasswordBase64 = System.Convert.ToBase64String(encryptedPassword);

            List<UserInfo> results = new List<UserInfo>();

            foreach (UserInfo userInfo in userInfos)
            {
                char[] charArrayUser = userInfo.HashedPassword.ToCharArray();
                byte[] passwordAsBytesUser = Array.ConvertAll(charArrayUser, PasswordFileHandler.GetConverter());

                if (CompareBytes(passwordAsBytesUser, encryptedPasswordPossible))  //compares byte arrays
                {
                    userInfo.ClearTextPassword = possiblePassword;
                    results.Add(userInfo);
                    Console.WriteLine(userInfo.Username + " " + userInfo.ClearTextPassword);
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
    }
}
