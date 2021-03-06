﻿/*
 *     Copyright 2016 IBM Corp.
 *     Licensed under the Apache License, Version 2.0 (the "License");
 *     you may not use this file except in compliance with the License.
 *     You may obtain a copy of the License at
 *     http://www.apache.org/licenses/LICENSE-2.0
 *     Unless required by applicable law or agreed to in writing, software
 *     distributed under the License is distributed on an "AS IS" BASIS,
 *     WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 *     See the License for the specific language governing permissions and
 *     limitations under the License.
 */

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using Windows.Security.Credentials;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace JSONStoreWin8Lib.JSONStore
{
    public class JSONStoreSecurityManager
    {   
        PasswordVault vault;
        private string vaultID = "WL_JSONStore";
        private static JSONStoreSecurityManager sharedManagerSingleton;

        public JSONStoreSecurityManager()
        {
            vault = new PasswordVault();
        }

        public static JSONStoreSecurityManager sharedManager()
        {
            if (sharedManagerSingleton == null)
            {
                sharedManagerSingleton = new JSONStoreSecurityManager();
            }
            return sharedManagerSingleton;
        }

        public static IBuffer generateRandom(int length)
        {
            IBuffer randomBuffer = CryptographicBuffer.GenerateRandom((uint)length);
            return randomBuffer;
        }

        public string encryptWithKey(IBuffer cpk, IBuffer dpk, IBuffer iv) 
        {
            // Setup an AES key, using AES in CBC mode and applying PKCS#7 padding on the input            
            SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
            CryptographicKey cpkKey = aesProvider.CreateSymmetricKey(cpk);

            // Encrypt the data and convert it to a Base64 string
            IBuffer encrypted = CryptographicEngine.Encrypt(cpkKey, dpk, iv);

            string cipher = CryptographicBuffer.EncodeToBase64String(encrypted);

            return cipher;
        }

        public string decryptWithKey(IBuffer cpk, string cipher, IBuffer iv)
        {
            IBuffer encryptedDPK = CryptographicBuffer.DecodeFromBase64String(cipher);

            // Setup an AES key, using AES in CBC mode and applying PKCS#7 padding on the input            
            SymmetricKeyAlgorithmProvider aesProvider = SymmetricKeyAlgorithmProvider.OpenAlgorithm("AES_CBC_PKCS7");
            CryptographicKey cpkKey = aesProvider.CreateSymmetricKey(cpk);

            // Decrypt the data
            IBuffer decrypted = CryptographicEngine.Decrypt(cpkKey, encryptedDPK, iv);

            string dpk = CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8, decrypted);

            return dpk;
        }

        public bool isKeyStored(string username)
        {           
            try
            {
                PasswordCredential credential = vault.Retrieve(vaultID, username);
                if (credential != null)
                {
                    return true;
                }
            }
            catch (Exception)
            {
                //log error message
            }
           
            return false;
        }

        public string getDPK(string username, string password)
        {
            string dpk = "";
            
            try
            {
                PasswordCredential credential = vault.Retrieve(vaultID, username);

                if (credential != null)
                {
                    credential.RetrievePassword();
                    JObject jsonEntries = JObject.Parse(credential.Password);

                    string saltString = jsonEntries.GetValue(JSONStoreConstants.JSON_STORE_KEY_SALT).ToString();
                    string ivString = jsonEntries.GetValue(JSONStoreConstants.JSON_STORE_KEY_IV).ToString();

                    IBuffer salt = CryptographicBuffer.DecodeFromHexString(saltString);
                    IBuffer iv = CryptographicBuffer.DecodeFromHexString(ivString);
                    IBuffer pwKey = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
                    IBuffer cpk = passwordToKey(pwKey, salt);

                    // decrypt the dpk
                    dpk = decryptWithKey(cpk, jsonEntries.GetValue(JSONStoreConstants.JSON_STORE_KEY_DPK).ToString(), iv);
                }
            }
            catch (Exception)
            {
                //log error message
            }

            return dpk;
        }

        public IBuffer getSalt(string username)
        {
            IBuffer saltBuffer = null;
            
            try
            {
                PasswordCredential credential = vault.Retrieve(vaultID, username);

                if (credential != null)
                {
                    credential.RetrievePassword();
                    JObject jsonEntries = JObject.Parse(credential.Password);
                    string saltString = jsonEntries.GetValue(JSONStoreConstants.JSON_STORE_KEY_SALT).ToString();

                    saltBuffer = CryptographicBuffer.DecodeFromHexString(saltString);
                }
            }
            catch (Exception)
            {
                //log error message
            }

            return saltBuffer;
        }

        public bool storeDPK(string username, string password, string clearDPK, IBuffer salt, bool isUpdate)
        {

            IBuffer dpk;
            IBuffer secureRandom;

            try
            {
                if (!String.IsNullOrEmpty(clearDPK))
                {
                    secureRandom = CryptographicBuffer.ConvertStringToBinary(clearDPK, BinaryStringEncoding.Utf8);
                }
                else
                {
                    secureRandom = generateRandom(32);
                }

                if (!isUpdate)
                {
                    IBuffer dpkBuffer = passwordToKey(secureRandom, salt);

                    //the dpk as Hex is the key we use to secure the database and the key that we encrypt
                    string dpkAsHex = CryptographicBuffer.EncodeToHexString(dpkBuffer);
                    dpk = CryptographicBuffer.ConvertStringToBinary(dpkAsHex, BinaryStringEncoding.Utf8);
                }
                else
                {
                    dpk = secureRandom;
                }

                // create the client key - 32 byte
                IBuffer pwKey = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
                IBuffer cpk = passwordToKey(pwKey, salt);

                // create the IV - 16 byte
                IBuffer iv = generateRandom(JSONStoreConstants.JSON_STORE_DEFAULT_IV_SIZE);

                // encrypt the dpk
                string cipher = encryptWithKey(cpk, dpk, iv);

                // form the JSON object
                JObject jsonEntries = new JObject();
                jsonEntries.Add(JSONStoreConstants.JSON_STORE_KEY_IV, CryptographicBuffer.EncodeToHexString(iv));
                jsonEntries.Add(JSONStoreConstants.JSON_STORE_KEY_SALT, CryptographicBuffer.EncodeToHexString(salt));
                jsonEntries.Add(JSONStoreConstants.JSON_STORE_KEY_DPK, cipher);
                jsonEntries.Add(JSONStoreConstants.JSON_STORE_KEY_ITERATIONS, JSONStoreConstants.JSON_STORE_DEFAULT_PBKDF2_ITERATIONS);
                jsonEntries.Add(JSONStoreConstants.JSON_STORE_KEY_VERSION, JSONStoreConstants.JSON_STORE_KEY_VERSION_NUMBER);

                string jsonStr = JsonConvert.SerializeObject(jsonEntries);

                vault.Add(new PasswordCredential(vaultID, username, jsonStr));
            }
            catch (Exception)
            {
                //log error message
                return false;
            }
          
            return true;
        }

        public bool changeOldPassword(string username, string oldPassword, string newPassword)
        {
            String oldDPK = getDPK(username, oldPassword);
            IBuffer salt = getSalt(username);

            if (String.IsNullOrEmpty(oldDPK) || salt == null)
                return false;

            return storeDPK(username, newPassword, oldDPK, salt, true);
        }

        public bool clearKeys()
        {
            try
            {
                var credentialList = vault.FindAllByResource(vaultID);
                foreach (PasswordCredential credential in credentialList)
                {
                    vault.Remove(credential);
                }
            }
            catch (Exception)
            {
                //log error message
                return false;
            }

            return true;
        }

        public bool clearKey(string username) 
        {
            try 
            {
                PasswordCredential credential = vault.Retrieve(vaultID, username);
                vault.Remove(credential);
            } catch (Exception)
            {
                return false;
            }

            return true;
        } 

        private IBuffer passwordToKey(IBuffer password, IBuffer salt)
        {
            KeyDerivationParameters kdfParameters = KeyDerivationParameters.BuildForPbkdf2(salt, (uint)JSONStoreConstants.JSON_STORE_DEFAULT_PBKDF2_ITERATIONS);

            // Get a KDF provider for PBKDF2, and store the source password in a Cryptographic Key
            KeyDerivationAlgorithmProvider kdf = KeyDerivationAlgorithmProvider.OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha256);

            CryptographicKey passwordKey = kdf.CreateKey(password);

            IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(passwordKey, kdfParameters, (uint)32);

            return keyMaterial;
        }

    }
}

