
using System;
using System.IO;
using System.Security.Cryptography;

namespace RealmForge.FileSystem
{
    /// <summary>
    /// A Utility class for encrypting and decrypting streams, bytes, and strings
    /// </summary>
    public class Encrypter
    {
        #region Static Methods
        #region Encrypt
        /// <summary>
        /// Encrypt a byte array into a byte array using a key and an iv 
        /// </summary>
        /// <param name="clearData"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static byte[] Encrypt( byte[] clearData, byte[] key, byte[] iv )
        {
            // Create a MemoryStream to accept the encrypted bytes 
            MemoryStream ms = new MemoryStream();

            // Create a symmetric algorithm. 
            // We are going to use Rijndael because it is strong and
            // available on all platforms. 
            // You can use other algorithms, to do so substitute the
            // next line with something like 
            //      TripleDES alg = TripleDES.Create(); 
            Rijndael alg = Rijndael.Create();

            // Now set the key and the iv. 
            // We need the iv (Initialization Vector) because
            // the algorithm is operating in its default 
            // mode called CBC (Cipher Block Chaining).
            // The iv is XORed with the first block (8 byte) 
            // of the data before it is encrypted, and then each
            // encrypted block is XORed with the 
            // following block of plaintext.
            // This is done to make encryption more secure. 

            // There is also a mode called ECB which does not need an iv,
            // but it is much less secure. 
            alg.Key = key;
            alg.IV = iv;

            // Create a CryptoStream through which we are going to be
            // pumping our data. 
            // CryptoStreamMode.Write means that we are going to be
            // writing data to the stream and the output will be written
            // in the MemoryStream we have provided. 
            CryptoStream cs = new CryptoStream( ms,
                alg.CreateEncryptor(), CryptoStreamMode.Write );

            // Write the data and make it do the encryption 
            cs.Write( clearData, 0, clearData.Length );

            // Close the crypto stream (or do FlushFinalBlock). 
            // This will tell it that we have done our encryption and
            // there is no more data coming in, 
            // and it is now a good time to apply the padding and
            // finalize the encryption process. 
            cs.Close();

            // Now get the encrypted data from the MemoryStream.
            // Some people make a mistake of using GetBuffer() here,
            // which is not the right way. 
            byte[] encryptedData = ms.ToArray();

            return encryptedData;
        }


        /// <summary>
        /// Encrypt a string into a string using a password 
        /// </summary>
        /// <param name="clearText"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Encrypt( string clearText, string password )
        {
            // First we need to turn the input string into a byte array. 
            byte[] clearBytes =
                System.Text.Encoding.Unicode.GetBytes( clearText );

            // Then, we need to turn the password into key and iv 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
							   0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );

            // Now get the key/iv and do the encryption using the
            // function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the iv. 
            // iv should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the iv size. 
            // You can also read KeySize/BlockSize properties off
            // the algorithm to find out the sizes. 
            byte[] encryptedData = Encrypt( clearBytes,
                pdb.GetBytes( 32 ), pdb.GetBytes( 16 ) );

            // Now we need to turn the resulting byte array into a string. 
            // A common mistake would be to use an Encoding class for that.
            //It does not work because not all byte values can be
            // represented by characters. 
            // We are going to be using Base64 encoding that is designed
            //exactly for what we are trying to do. 
            return Convert.ToBase64String( encryptedData );

        }


        /// <summary>
        /// Encrypt bytes into bytes using a password 
        /// </summary>
        /// <param name="clearData"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] Encrypt( byte[] clearData, string password )
        {
            // We need to turn the password into key and iv. 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
							   0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );

            // Now get the key/iv and do the encryption using the function
            // that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the iv. 
            // iv should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is 8
            // bytes and so should be the iv size. 
            // You can also read KeySize/BlockSize properties off the
            // algorithm to find out the sizes. 
            return Encrypt( clearData, pdb.GetBytes( 32 ), pdb.GetBytes( 16 ) );

        }


        /// <summary>
        /// Encrypt a file into another file using a password 
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        public static void Encrypt( string fileIn, string fileOut, string password )
        {
            //Dont overwrite existing?
            Encrypt( File.OpenRead( fileIn ), File.Open( fileOut, FileMode.OpenOrCreate, FileAccess.Write ), password, true );
        }
        /// <summary>
        /// Encrypt a file into another file using a password 
        /// </summary>
        /// <param name="fsIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        /// <remarks>Be sure to move the stream position to 0 if wanted, closes fsOut</remarks>
        public static void Encrypt( Stream fsIn, Stream fsOut, string password, bool closeStreams )
        {

            // Then we are going to derive a key and an iv from the
            // password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
							   0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );

            Rijndael alg = Rijndael.Create();
            alg.Key = pdb.GetBytes( 32 );
            alg.IV = pdb.GetBytes( 16 );

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the encrypted bytes. 
            CryptoStream cs = new CryptoStream( fsOut,
                alg.CreateEncryptor(), CryptoStreamMode.Write );

            // Now will will initialize a buffer and will be processing
            // the input file in chunks. 
            // This is done to avoid reading the whole file (which can
            // be huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read( buffer, 0, bufferLen );

                // encrypt it 
                cs.Write( buffer, 0, bytesRead );
            } while ( bytesRead != 0 );

            // close everything 

            // this will also close the unrelying fsOut stream
            if ( closeStreams )
            {
                cs.Close();
                fsIn.Close();
            }
            else
                cs.FlushFinalBlock();
        }

        #endregion

        #region Decrypt

        /// <summary>
        /// Decrypt a byte array into a byte array using a key and an iv 
        /// </summary>
        /// <param name="cipherData"></param>
        /// <param name="key"></param>
        /// <param name="iv"></param>
        /// <returns></returns>
        public static byte[] Decrypt( byte[] cipherData, byte[] key, byte[] iv )
        {
            // Create a MemoryStream that is going to accept the
            // decrypted bytes 
            MemoryStream ms = new MemoryStream();

            // Create a symmetric algorithm. 
            // We are going to use Rijndael because it is strong and
            // available on all platforms. 
            // You can use other algorithms, to do so substitute the next
            // line with something like 
            //     TripleDES alg = TripleDES.Create(); 
            Rijndael alg = Rijndael.Create();

            // Now set the key and the iv. 
            // We need the iv (Initialization Vector) because the algorithm
            // is operating in its default 
            // mode called CBC (Cipher Block Chaining). The iv is XORed with
            // the first block (8 byte) 
            // of the data after it is decrypted, and then each decrypted
            // block is XORed with the previous 
            // cipher block. This is done to make encryption more secure. 
            // There is also a mode called ECB which does not need an iv,
            // but it is much less secure. 
            alg.Key = key;
            alg.IV = iv;

            // Create a CryptoStream through which we are going to be
            // pumping our data. 
            // CryptoStreamMode.Write means that we are going to be
            // writing data to the stream 
            // and the output will be written in the MemoryStream
            // we have provided. 
            CryptoStream cs = new CryptoStream( ms,
                alg.CreateDecryptor(), CryptoStreamMode.Write );

            // Write the data and make it do the decryption 
            cs.Write( cipherData, 0, cipherData.Length );

            // Close the crypto stream (or do FlushFinalBlock). 
            // This will tell it that we have done our decryption
            // and there is no more data coming in, 
            // and it is now a good time to remove the padding
            // and finalize the decryption process. 
            cs.Close();

            // Now get the decrypted data from the MemoryStream. 
            // Some people make a mistake of using GetBuffer() here,
            // which is not the right way. 
            byte[] decryptedData = ms.ToArray();

            return decryptedData;
        }


        /// <summary>
        /// Decrypt a string into a string using a password 
        /// </summary>
        /// <param name="cipherText"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static string Decrypt( string cipherText, string password )
        {
            // First we need to turn the input string into a byte array. 
            // We presume that Base64 encoding was used 
            byte[] cipherBytes = Convert.FromBase64String( cipherText );

            // Then, we need to turn the password into key and iv 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 
							   0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );

            // Now get the key/iv and do the decryption using
            // the function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first
            // getting 32 bytes for the key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the iv. 
            // iv should always be the block size, which is by
            // default 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the iv size. 
            // You can also read KeySize/BlockSize properties off
            // the algorithm to find out the sizes. 
            byte[] decryptedData = Decrypt( cipherBytes,
                pdb.GetBytes( 32 ), pdb.GetBytes( 16 ) );

            // Now we need to turn the resulting byte array into a string. 
            // A common mistake would be to use an Encoding class for that.
            // It does not work 
            // because not all byte values can be represented by characters. 
            // We are going to be using Base64 encoding that is 
            // designed exactly for what we are trying to do. 
            return System.Text.Encoding.Unicode.GetString( decryptedData );
        }


        /// <summary>
        /// Decrypt bytes into bytes using a password 
        /// </summary>
        /// <param name="cipherData"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static byte[] Decrypt( byte[] cipherData, string password )
        {
            // We need to turn the password into key and iv. 
            // We are using salt to make it harder to guess our key
            // using a dictionary attack - 
            // trying to guess a password by enumerating all possible words. 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
							   0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );

            // Now get the key/iv and do the Decryption using the 
            //function that accepts byte arrays. 
            // Using PasswordDeriveBytes object we are first getting
            // 32 bytes for the key 
            // (the default Rijndael key length is 256bit = 32bytes)
            // and then 16 bytes for the iv. 
            // iv should always be the block size, which is by default
            // 16 bytes (128 bit) for Rijndael. 
            // If you are using DES/TripleDES/RC2 the block size is
            // 8 bytes and so should be the iv size. 

            // You can also read KeySize/BlockSize properties off the
            // algorithm to find out the sizes. 
            return Decrypt( cipherData, pdb.GetBytes( 32 ), pdb.GetBytes( 16 ) );
        }


        /// <summary>
        /// Decrypt a file into another file using a password 
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        public static void Decrypt( string fileIn, string fileOut, string password )
        {
            Decrypt( File.OpenRead( fileIn ), File.Open( fileOut, FileMode.OpenOrCreate, FileAccess.Write ), password, true );
        }

        /// <summary>
        /// Decrypt a file into another file using a password 
        /// </summary>
        /// <param name="fileIn"></param>
        /// <param name="fileOut"></param>
        /// <param name="password"></param>
        /// <remarks>Be sure to move the stream position to 0 if wanted, closes fsOut</remarks>
        public static void Decrypt( Stream fsIn, Stream fsOut, string password, bool closeStreams )
        {

            // Then we are going to derive a key and an iv from
            // the password and create an algorithm 
            PasswordDeriveBytes pdb = new PasswordDeriveBytes( password,
                new byte[] {0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 
							   0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76} );
            Rijndael alg = Rijndael.Create();

            alg.Key = pdb.GetBytes( 32 );
            alg.IV = pdb.GetBytes( 16 );

            // Now create a crypto stream through which we are going
            // to be pumping data. 
            // Our fileOut is going to be receiving the Decrypted bytes. 
            CryptoStream cs = new CryptoStream( fsOut,
                alg.CreateDecryptor(), CryptoStreamMode.Write );

            // Now will will initialize a buffer and will be 
            // processing the input file in chunks. 
            // This is done to avoid reading the whole file (which can be
            // huge) into memory. 
            int bufferLen = 4096;
            byte[] buffer = new byte[bufferLen];
            int bytesRead;

            do
            {
                // read a chunk of data from the input file 
                bytesRead = fsIn.Read( buffer, 0, bufferLen );

                // Decrypt it 
                cs.Write( buffer, 0, bytesRead );

            } while ( bytesRead != 0 );

            // close everything 
            if ( closeStreams )
            {
                cs.Close(); // this will also close the unrelying fsOut stream 
                fsIn.Close();
            }
            else
            {
                cs.FlushFinalBlock();
            }
        }
        #endregion
        #endregion
    }
}
