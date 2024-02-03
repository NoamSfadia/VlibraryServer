using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace VlibraryServer
{
    internal class RSA
    {
        private string PrivateKey;
        private string PublicKey;
        private UnicodeEncoding Encoder;
        private RSACryptoServiceProvider Rsa;

        public RSA()
        {
            Encoder = new UnicodeEncoding();
            Rsa = new RSACryptoServiceProvider();

            PrivateKey = Rsa.ToXmlString(true);
            PublicKey = Rsa.ToXmlString(false);
        }
        /// <summary>
        /// return PrivateKey
        /// </summary>
        /// <returns>PrivateKey</returns>
        public string GetPrivateKey()
        {
            return this.PrivateKey;
        }
        /// <summary>
        /// return PublicKey
        /// </summary>
        /// <returns>PublicKey</returns>
        public string GetPublicKey()
        {
            return this.PublicKey;
        }
        /// <summary>
        /// decript data by privateKey
        /// </summary>
        /// <param name="data">data to decript</param>
        /// /// <param name="privateKey">privateKey</param>
        /// <returns>decripted data</returns>
        public string Decrypt(string data, string privateKey)
        {

            var dataArray = data.Split(new char[] { ',' });
            byte[] dataByte = new byte[dataArray.Length];
            for (int i = 0; i < dataArray.Length; i++)
            {
                dataByte[i] = Convert.ToByte(dataArray[i]);
            }

            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(privateKey);
            var decryptedByte = rsa.Decrypt(dataByte, false);
            return Encoder.GetString(decryptedByte);
        }
        /// <summary>
        /// Encrypt the data by public key
        /// </summary>
        /// <param name="data">data to encrypt</param>
        /// <param name="publicKey"></param>
        /// <returns>encripted data</returns>
        public string Encrypt(string data, string publicKey)
        {
            var rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(publicKey);
            var dataToEncrypt = Encoder.GetBytes(data);
            var encryptedByteArray = rsa.Encrypt(dataToEncrypt, false);
            var length = encryptedByteArray.Length;
            var item = 0;
            var sb = new StringBuilder();
            foreach (var x in encryptedByteArray)
            {
                item++;
                sb.Append(x);

                if (item < length)
                    sb.Append(",");
            }

            return sb.ToString();
        }

    }
}
