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
        [JsonIgnore]
        public string ClearTextPassword { get; set; }
        public string EncryptionIV { get; set; }
        private string Key
        {
            get
            {
                return System.Environment.MachineName + System.Environment.UserName + "453nfawehfaypg94#$#@%34wghvoawe[cwe45a3wtg";
            }
        }
        private readonly string salt = "$2a$04$qdxi1jNcjqWBlsviWGilx.Xxw0oMm0gZYx8ZsLq5ntsy5s4GFq3kq";
        public  bool Encrypt()
        {
            try
            {
                using (Aes myAes = Aes.Create())
                {
                    var derived =  new Rfc2898DeriveBytes(Encoding.ASCII.GetBytes(this.Key), Encoding.ASCII.GetBytes(this.salt), 100);
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
                }
                return true;
            }
            catch (Exception exe)
            {

                return false;
            }
        }
        public bool Decrypt()
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
                    var derived = new Rfc2898DeriveBytes(Encoding.ASCII.GetBytes(this.Key), Encoding.ASCII.GetBytes(this.salt), 100);
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
                }
                return true;
            }catch(Exception exe)
            {
                this.ClearTextPassword = this.Password;
                return false;
            }

        }
    }

    public class Filter
    {
        public int VideoCount { get; set; } = 10000;
        public DateTime? StartDateTime { get; set; }
        public DateTime? EndDateTime { get; set; } = DateTime.Today.AddDays(1).AddSeconds(-1);
        [JsonIgnore]
        public DateTime? StartDateTimeUtc { get; set; }
        [JsonIgnore]
        public DateTime? EndDateTimeUtc { get; set; }
        public string DownloadPath { get; set; }
        public string TimeZone { get; set; }
        public bool OnlyStarred { get; set; } = false;
        public bool SetDebug { get; set; } = false;

    }

    internal class Config
    {
        public Authentication Authentication { get; set; }
        public Filter Filter { get; set; }
    }
}

