using System;
using System.Text.Json.Serialization;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace RingVideos.Models
{
   public class Authentication
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string RefreshToken { get; set; }
        [JsonIgnore]
        public string ClearTextPassword { get; set; }
        [JsonIgnore]
        public string ClearTextRefreshToken { get; set; }
        public string EncryptionIV { get; set; }
        private string Key
        {
            get
            {
                return System.Environment.MachineName + System.Environment.UserName + "453nfawehfaypg94#$#@%34wghvoawe[cwe45a3wtg";
            }
        }
        private readonly string salt = "$2a$04$qdxi1jNcjqWBlsviWGilx.Xxw0oMm0gZYx8ZsLq5ntsy5s4GFq3kq";
        public  Authentication Encrypt()
        {
            try
            {
                using (Aes myAes = Aes.Create())
                {
#pragma warning disable SYSLIB0041 // Type or member is obsolete
               var derived =  new Rfc2898DeriveBytes(Encoding.ASCII.GetBytes(this.Key), Encoding.ASCII.GetBytes(this.salt), 100);
#pragma warning restore SYSLIB0041 // Type or member is obsolete
               myAes.GenerateIV();
                    myAes.Key = derived.GetBytes(32);
                    this.EncryptionIV =  Convert.ToBase64String(myAes.IV);

                    ICryptoTransform encryptor = myAes.CreateEncryptor(myAes.Key, myAes.IV);

                    var clearTextBytes = Encoding.ASCII.GetBytes(this.ClearTextPassword);
                    // Create the streams used for decryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {

                            csEncrypt.Write(clearTextBytes, 0, clearTextBytes.Length);
                            csEncrypt.Close();
                            var encrBytes = msEncrypt.ToArray();
                            this.Password = Convert.ToBase64String(encrBytes);
                        }
                    }

                    clearTextBytes = Encoding.ASCII.GetBytes(this.ClearTextRefreshToken);
                    // Create the streams used for decryption.
                    using (MemoryStream msEncrypt = new MemoryStream())
                    {
                        using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                        {

                            csEncrypt.Write(clearTextBytes, 0, clearTextBytes.Length);
                            csEncrypt.Close();
                            var encrBytes = msEncrypt.ToArray();
                            this.RefreshToken = Convert.ToBase64String(encrBytes);
                        }
                    }
                }
                return this;
            }
            catch (Exception)
            {

                return this;
            }
        }
        public Authentication Decrypt()
        {
            try
            {
                using (Aes myAes = Aes.Create())
                {
                    if (string.IsNullOrWhiteSpace(this.EncryptionIV))
                    {
                        myAes.GenerateIV();
                        this.EncryptionIV = Convert.ToBase64String(myAes.IV);
                    }
                    else
                    {
                        myAes.IV = Convert.FromBase64String(this.EncryptionIV);
                    }
#pragma warning disable SYSLIB0041 // Type or member is obsolete
               var derived = new Rfc2898DeriveBytes(Encoding.ASCII.GetBytes(this.Key), Encoding.ASCII.GetBytes(this.salt), 100);
#pragma warning restore SYSLIB0041 // Type or member is obsolete
               myAes.Key = derived.GetBytes(32);
                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform decryptor = myAes.CreateDecryptor(myAes.Key, myAes.IV);
                    var passwordBytes = Convert.FromBase64String(this.Password);
                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream())
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                        {
                            csDecrypt.Write(passwordBytes, 0, passwordBytes.Length);
                            csDecrypt.Close();
                            var clearBytes = msDecrypt.ToArray();
                            this.ClearTextPassword = System.Text.Encoding.UTF8.GetString(clearBytes);
                        }
                    }

                    var refreshBytes = Convert.FromBase64String(this.RefreshToken);
                    // Create the streams used for decryption.
                    using (MemoryStream msDecrypt = new MemoryStream())
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Write))
                        {
                            csDecrypt.Write(refreshBytes, 0, refreshBytes.Length);
                            csDecrypt.Close();
                            var clearBytes = msDecrypt.ToArray();
                            this.ClearTextRefreshToken = System.Text.Encoding.UTF8.GetString(clearBytes);
                        }
                    }
                }
                return this;
            }
            catch(Exception)
            {
                this.ClearTextPassword = this.Password;
                return this;
            }

        }
    }
}

