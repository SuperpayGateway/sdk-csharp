namespace gateway
{
    using System;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Security.Cryptography;
    using System.Collections.Generic;
    using System.Net;

    /// <summary>
    /// gateway sdk
    /// </summary>
    public static class gatewaySdk
    {
        /// <summary>
        /// rsa algorithm
        /// </summary>
        const String ALGORITHM = "AES/CBC/PKCS7PADDING";

        /// <summary>
        /// aes algorithm
        /// </summary>
        const String HASH_ALGORITHM = "SHA256withRSA";

        /// <summary>
        /// encrypt auth info
        /// </summary>
        static String EncryptAuthInfo = "";

        /// <summary>
        /// user deposit
        /// </summary>
        /// <param name="orderId">order number - maxlength(40)</param>
        /// <param name="amount">order amount - maxlength(20)</param>
        /// <param name="currency">Empty default: MYR - maxlength(16)</param>
        /// <param name="payMethod">FPX, TNG_MY, ALIPAY_CN, GRABPAY_MY, BOOST_MY - maxlength(16)</param>
        /// <param name="customerName">customer name - maxlength(64)</param>
        /// <param name="customerEmail">customer email - maxlength(64)</param>
        /// <param name="customerPhone">customer phone - maxlength(20)</param>
        /// <returns>code,message,paymentUrl,transactionId</returns>
        public static Dictionary<String, String> deposit(String orderId, Decimal amount, String currency,
            String payMethod, String customerName, String customerEmail, String customerPhone)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();
            try
            {
                String token = getToken();
                if (String.IsNullOrEmpty(token)) return result;
                String requestUrl = "gateway/" + gatewayCfg.VERSION_NO + "/createPayment";
                Dictionary<String, String> cnst = generateConstant(requestUrl);
                // If callbackUrl and redirectUrl are empty, take the values ​​of [curl] and [rurl] in the developer center.
                // Remember, the format of json and the order of json attributes must be the same as the SDK specifications.
                // The sorting rules of Json attribute data are arranged from [a-z]
                String bodyJson = "{\"customer\":{\"email\":\"" + customerEmail + "\",\"name\":\"" + customerName + "\",\"phone\":\"" + customerPhone + "\"},\"method\":\"" + payMethod + "\",\"order\":{\"additionalData\":\"\",\"amount\":\"" + amount.ToString() + "\",\"currencyType\":\"" + (String.IsNullOrEmpty(currency) ? "MYR" : currency) + "\",\"id\":\"" + orderId + "\",\"title\":\"Payment\"}}";
                //String bodyJson = "{\"callbackUrl\":\"https://www.google.com\",\"customer\":{\"email\":\"" + customerEmail + "\",\"name\":\"" + customerName + "\",\"phone\":\"" + customerPhone + "\"},\"method\":\"" + payMethod + "\",\"order\":{\"additionalData\":\"\",\"amount\":\"" + amount.ToString() + "\",\"currencyType\":\"" + (String.IsNullOrEmpty(currency) ? "MYR" : currency) + "\",\"id\":\"" + orderId + "\",\"title\":\"Payment\"},\"redirectUrl\":\"https://www.google.com\"}";
                String base64ReqBody = sortedAfterToBased64(bodyJson);
                String signature = createSignature(cnst, base64ReqBody);
                String encryptData = symEncrypt(base64ReqBody);
                String json = "{\"data\":\"" + encryptData + "\"}";
                String[] keys = new String[] { "code", "message", "encryptedData" };
                Dictionary<String, String> dict = post(requestUrl, token, signature, json, cnst["nonceStr"], cnst["timestamp"], keys);
                if (!String.IsNullOrEmpty(dict["code"]) && dict["code"] == "1" && !String.IsNullOrEmpty(dict["encryptedData"]))
                {
                    String decryptedData = symDecrypt(dict["encryptedData"]);
                    keys = new String[] { "paymentUrl", "transactionId" };
                    dict = findJosnValue(keys, decryptedData);
                    if (!String.IsNullOrEmpty(dict["paymentUrl"]) && !String.IsNullOrEmpty(dict["transactionId"]))
                    {
                        result.Add("code", "1");
                        result.Add("message", "");
                        result.Add("paymentUrl", dict["paymentUrl"]);
                        result.Add("transactionId", dict["transactionId"]);
                        return result;
                    }
                }
                result = new Dictionary<string, string>();
                result.Add("code", "0");
                result.Add("message", dict["message"]);
                return result;
            }
            catch (Exception e)
            {
                result = new Dictionary<string, string>();
                result.Add("code", "0");
                result.Add("message", e.Message);
                return result;
            }
        }

        /// <summary>
        /// user withdraw
        /// </summary>
        /// <param name="orderId">order number - maxlength(40)</param>
        /// <param name="amount">order amount - maxlength(20)</param>
        /// <param name="currency">Empty default: MYR - maxlength(16)</param>
        /// <param name="bankCode">MayBank=MBB,Public Bank=PBB,CIMB Bank=CIMB,Hong Leong Bank=HLB,RHB Bank=RHB,AmBank=AMMB
        ///  United Overseas Bank=UOB,Bank Rakyat=BRB,OCBC Bank=OCBC,HSBC Bank=HSBC  - maxlength(16)
        /// </param>
        /// <param name="cardholder">cardholder - maxlength(64)</param>
        /// <param name="accountNumber">account number - maxlength(20)</param>
        /// <param name="refName">recipient refName - maxlength(64)</param>
        /// <param name="recipientEmail">recipient email - maxlength(64)</param>
        /// <param name="recipientPhone">recipient phone - maxlength(20)</param>
        /// <returns>code,message,transactionId</returns>
        public static Dictionary<String, String> withdraw(String orderId, Decimal amount, String currency,
            String bankCode, String cardholder, String accountNumber,String refName, String recipientEmail, String recipientPhone)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();
            try
            {
                String token = getToken();
                if (String.IsNullOrEmpty(token)) return result;
                String requestUrl = "gateway/" + gatewayCfg.VERSION_NO + "/withdrawRequest";
                Dictionary<String, String> cnst = generateConstant(requestUrl);
                // payoutspeed contain "fast", "normal", "slow" ,default is : "fast"
                // Remember, the format of json and the order of json attributes must be the same as the SDK specifications.
                // The sorting rules of Json attribute data are arranged from [a-z]
                String bodyJson = "{\"order\":{\"amount\":\"" + amount.ToString() + "\",\"currencyType\":\"" + (String.IsNullOrEmpty(currency) ? "MYR" : currency) + "\",\"id\":\"" + orderId + "\"},\"recipient\":{\"email\":\"" + recipientEmail + "\",\"methodRef\":\"" + refName + "\",\"methodType\":\"" + bankCode + "\",\"methodValue\":\"" + accountNumber + "\",\"name\":\"" + cardholder + "\",\"phone\":\"" + recipientPhone + "\"}}";
                //String bodyJson = "{\"callbackUrl\":\"https://www.google.com\",\"order\":{\"amount\":\"" + amount.ToString() + "\",\"currencyType\":\"" + (String.IsNullOrEmpty(currency) ? "MYR" : currency) + "\",\"id\":\"" + orderId + "\"},\"payoutspeed\":\"normal\",\"recipient\":{\"email\":\"" + recipientEmail + "\",\"methodRef\":\"" + refName + "\",\"methodType\":\"" + bankCode + "\",\"methodValue\":\"" + accountNumber + "\",\"name\":\"" + cardholder + "\",\"phone\":\"" + recipientPhone + "\"}}";
                String base64ReqBody = sortedAfterToBased64(bodyJson);
                String signature = createSignature(cnst, base64ReqBody);
                String encryptData = symEncrypt(base64ReqBody);
                String json = "{\"data\":\"" + encryptData + "\"}";
                String[] keys = new String[] { "code", "message", "encryptedData" };
                Dictionary<String, String> dict = post(requestUrl, token, signature, json, cnst["nonceStr"], cnst["timestamp"], keys);
                if (!String.IsNullOrEmpty(dict["code"]) && dict["code"] == "1" && !String.IsNullOrEmpty(dict["encryptedData"]))
                {
                    String decryptedData = symDecrypt(dict["encryptedData"]);
                    keys = new String[] { "transactionId" };
                    dict = findJosnValue(keys, decryptedData);
                    if (!String.IsNullOrEmpty(dict["transactionId"]))
                    {
                        result.Add("code", "1");
                        result.Add("message", "");
                        result.Add("transactionId", dict["transactionId"]);
                        return result;
                    }
                }
                result = new Dictionary<string, string>();
                result.Add("code", dict["code"]);
                result.Add("message", dict["message"]);
                return result;
            }
            catch (Exception e)
            {
                result = new Dictionary<string, string>();
                result.Add("code", "0");
                result.Add("message", e.Message);
                return result;
            }
        }

        /// <summary>
        /// User deposit and withdrawal details
        /// </summary>
        /// <param name="orderId">transaction id</param>
        /// <param name="type">1 deposit,2 withdrawal</param>
        /// <returns>code,message,transactionId,amount,fee</returns>
        public static Dictionary<String, String> detail(String orderId, int type)
        {
            Dictionary<String, String> result = new Dictionary<String, String>();
            try
            {
                String token = getToken();
                if (String.IsNullOrEmpty(token)) return result;
                String requestUrl = "gateway/" + gatewayCfg.VERSION_NO + "/getTransactionStatusById";
                Dictionary<String, String> cnst = generateConstant(requestUrl);
                // Remember, the format of json and the order of json attributes must be the same as the SDK specifications.
                // The sorting rules of Json attribute data are arranged from [a-z]
                // type : 1 deposit,2 withdrawal
                String bodyJson = "{\"transactionId\":\"" + orderId + "\",\"type\":" + type + "}";
                String base64ReqBody = sortedAfterToBased64(bodyJson);
                String signature = createSignature(cnst, base64ReqBody);
                String encryptData = symEncrypt(base64ReqBody);
                String json = "{\"data\":\"" + encryptData + "\"}";
                String[] keys = new String[] { "code", "message", "encryptedData" };
                Dictionary<String, String> dict = post(requestUrl, token, signature, json, cnst["nonceStr"], cnst["timestamp"], keys);
                if (!String.IsNullOrEmpty(dict["code"]) && dict["code"] == "1" && !String.IsNullOrEmpty(dict["encryptedData"]))
                {
                    String decryptedData = symDecrypt(dict["encryptedData"]);
                    result = new Dictionary<String, String>();
                    result.Add("code", "1");
                    result.Add("message", decryptedData);
                    return result;
                }
                result = new Dictionary<String, String>();
                result.Add("code", dict["code"]);
                result.Add("message", dict["message"]);
                return result;
            }
            catch (Exception e)
            {
                result = new Dictionary<String, String>();
                result.Add("code", "0");
                result.Add("message", e.Message);
                return result;
            }
        }

        /// <summary>
        /// get server token
        /// </summary>
        /// <returns>token</returns>
        private static String getToken()
        {
            if (String.IsNullOrEmpty(EncryptAuthInfo))
            {
                String authString = stringToBase64(gatewayCfg.CLIENT_ID + ":" + gatewayCfg.CLIENT_SECRET);
                EncryptAuthInfo = publicEncrypt(authString);
            }
            String json = "{\"data\":\"" + EncryptAuthInfo + "\"}";
            String[] keys = new String[] { "code", "encryptedToken" };
            Dictionary<String, String> dict = post("gateway/" + gatewayCfg.VERSION_NO + "/createToken", "", "", json, "", "", keys);
            String token = "";
            if (!String.IsNullOrEmpty(dict["code"]) && !String.IsNullOrEmpty(dict["encryptedToken"]) && dict["code"] == "1")
            {
                token = symDecrypt(dict["encryptedToken"]);
            }
            return token;
        }

        /// <summary>
        /// A simple http request method
        /// </summary>
        /// <param name="url">url</param>
        /// <param name="parameters">parameters</param>
        /// <returns></returns>
        private static Dictionary<String, String> post(String url, String token, String signature, String json, String nonceStr, String timestamp, String[] keys)
        {
            if (gatewayCfg.BASE_URL.EndsWith("/"))
            {
                url = gatewayCfg.BASE_URL + url;
            }
            else
            {
                url = gatewayCfg.BASE_URL + "/" + url;
            }
            WebRequest webRequest = WebRequest.Create(url);
            webRequest.Credentials = CredentialCache.DefaultCredentials;
            webRequest.Method = "POST";
            if (!String.IsNullOrEmpty(token) && !String.IsNullOrEmpty(signature) && !String.IsNullOrEmpty(nonceStr) && !String.IsNullOrEmpty(timestamp))
            {
                webRequest.Headers.Add("Authorization", token);
                //webRequest.Headers.Add("Content-Type", "application/json");
                webRequest.Headers.Add("X-Nonce-Str", nonceStr);
                webRequest.Headers.Add("X-Signature", signature);
                webRequest.Headers.Add("X-Timestamp", timestamp);
            }
            byte[] bytes = stringToBytes(json);
            webRequest.ContentLength = bytes.Length;
            webRequest.ContentType = "application/json";
            Stream dataStream = webRequest.GetRequestStream();
            dataStream.Write(bytes, 0, bytes.Length);
            dataStream.Close();
            WebResponse response = webRequest.GetResponse();
            Stream data = response.GetResponseStream();
            StreamReader reader = new StreamReader(data, Encoding.UTF8);
            String result = reader.ReadToEnd();
            response.Close();
            Dictionary<String, String> dict = findJosnValue(keys, result);
            return dict;
        }

        /// <summary>
        /// find json value
        /// </summary>
        /// <param name="keys">keys</param>
        /// <param name="josn">json string</param>
        /// <returns></returns>
        private static Dictionary<String, String> findJosnValue(String[] keys, String josn)
        {
            Dictionary<String, String> dict = new Dictionary<String, String>();
            foreach (String key in keys)
            {
                String value = "";
                String pattern = "\"" + key + "\":((\"(.*?)\")|(\\d*))";
                MatchCollection matches = Regex.Matches(josn, pattern, RegexOptions.IgnoreCase);
                if (matches.Count > 0)
                {
                    foreach (Match item in matches)
                    {
                        value = item.Groups[1].Value.Trim(new char[] { '"' });
                    }
                }
                dict.Add(key, value);
            }
            return dict;
        }

        /// <summary>
        /// create a signature
        /// </summary>
        /// <param name="cnst">constant</param>
        /// <param name="base64ReqBody">base64ReqBody</param>
        /// <returns>signature info</returns>
        private static String createSignature(Dictionary<String, String> cnst, String base64ReqBody)
        {
            String dataString = String.Format("data={0}&method={1}&nonceStr={2}&requestUrl={3}&signType={4}&timestamp={5}",
               base64ReqBody, cnst["method"], cnst["nonceStr"], cnst["requestUrl"], cnst["signType"], cnst["timestamp"]);
            String signature = sign(dataString);
            return String.Format("{0} {1}", cnst["signType"], signature);
        }

        /// <summary>
        /// generate constant
        /// </summary>
        /// <param name="requestUrl">request url</param>
        /// <returns>constant</returns>
        private static Dictionary<String, String> generateConstant(String requestUrl)
        {
            Dictionary<String, String> constant = new Dictionary<String, String>();
            constant.Add("method", "post");
            constant.Add("nonceStr", randomNonceStr());
            constant.Add("requestUrl", requestUrl);
            constant.Add("signType", "sha256");
            constant.Add("timestamp", (DateTimeOffset.UtcNow.Ticks / 1000).ToString());
            return constant;
        }

        /// <summary>
        /// random nonceStr
        /// </summary>
        /// <returns>nonceStr</returns>
        private static String randomNonceStr()
        {
            StringBuilder sb = new StringBuilder();
            char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789".ToCharArray();
            String[] stringChars = new String[8];
            Random random = new Random();

            for (int i = 0; i < stringChars.Length; i++)
            {
                sb.Append(chars[random.Next(chars.Length)]);
            }
            byte[] bytes = stringToBytes(sb.ToString());
            String hex = bytesToHex(bytes);
            return hex;
        }

        /// <summary>
        /// Encrypt data based on the server's public key
        /// </summary>
        /// <param name="data">data to be encrypted</param>
        /// <returns>encrypted data</returns>
        private static String publicEncrypt(String data)
        {
            byte[] bytesToEncrypt = stringToBytes(data);
            RSACryptoServiceProvider rsa = getServerPublicKey();
            byte[] dataByte = rsa.Encrypt(bytesToEncrypt, false);
            String encryptText = bytesToHex(dataByte);
            return encryptText;
        }

        /// <summary>
        /// Decrypt data according to the interface private key
        /// </summary>
        /// <param name="encryptData">data to be decrypted</param>
        /// <returns>return decrypted data</returns>
        private static String privateDecrypt(String encryptData)
        {
            byte[] bytesToDecrypt = hexToBytes(encryptData);
            RSACryptoServiceProvider rsa = getPrivateKey();
            byte[] dataByte = rsa.Decrypt(bytesToDecrypt, false);
            String decryptedText = bytesToString(dataByte);
            return decryptedText;
        }

        /// <summary>
        /// Payment interface data encryption method
        /// </summary>
        /// <param name="message">data to be encrypted</param>
        /// <returns>The encrypted data is returned in hexadecimal</returns>
        private static String symEncrypt(String message)
        {
            RijndaelManaged rijndael = new RijndaelManaged();
            byte[] key = stringToBytes(gatewayCfg.CLIENT_SYMMETRIC_KEY);
            byte[] iv = generateIv(gatewayCfg.CLIENT_SYMMETRIC_KEY);
            rijndael.Key = key;
            rijndael.IV = iv;
            rijndael.Mode = CipherMode.CBC;
            rijndael.Padding = PaddingMode.PKCS7;
            ICryptoTransform cryptoTransform = rijndael.CreateEncryptor();
            byte[] plainTextData = stringToBytes(message);
            byte[] cipherText = cryptoTransform.TransformFinalBlock(plainTextData, 0, plainTextData.Length);
            String encrypted = bytesToHex(cipherText);
            return encrypted;
        }

        /// <summary>
        /// Payment interface data decryption method
        /// </summary>
        /// <param name="encryptedMessage">symmetricKey Encrypted key (from Key property field in dev settings)</param>
        /// <returns>Return the data content of utf-8 after decryption</returns>
        public static String symDecrypt(String encryptedMessage)
        {
            RijndaelManaged rijndael = new RijndaelManaged();
            byte[] key = stringToBytes(gatewayCfg.CLIENT_SYMMETRIC_KEY);
            byte[] iv = generateIv(gatewayCfg.CLIENT_SYMMETRIC_KEY);
            rijndael.Key = key;
            rijndael.IV = iv;
            rijndael.Mode = CipherMode.CBC;
            rijndael.Padding = PaddingMode.PKCS7;
            ICryptoTransform cryptoTransform = rijndael.CreateDecryptor();
            byte[] plainTextData = hexToBytes(encryptedMessage);
            byte[] cipherText = cryptoTransform.TransformFinalBlock(plainTextData, 0, plainTextData.Length);
            String decryptedText = bytesToString(cipherText);
            return decryptedText;
        }

        /// <summary>
        /// private key signature
        /// </summary>
        /// <param name="data">data</param>
        /// <returns>return base64 signature</returns>
        private static String sign(String data)
        {
            byte[] dataByte = stringToBytes(data);
            RSACryptoServiceProvider rsa = getPrivateKey();
            byte[] sign = rsa.SignData(dataByte, 0, dataByte.Length, SHA256.Create());
            String base64 = bytesToBase64(sign);
            return base64;
        }

        /// <summary>
        /// Public key verification signature information
        /// </summary>
        /// <param name="data">data</param>
        /// <param name="signature">signature</param>
        /// <returns>verification results</returns>
        private static Boolean verify(String data, String signature)
        {
            byte[] dataByte = stringToBytes(data);
            RSACryptoServiceProvider rsa = getPrivateKey();
            byte[] signatureByte = base64ToBytes(signature);
            return rsa.VerifyData(dataByte, SHA256.Create(), signatureByte);
        }


        /// <summary>
        /// get server public key
        /// </summary>
        /// <returns></returns>
        private static RSACryptoServiceProvider getServerPublicKey()
        {
            String key = gatewayCfg.SERVER_PUB_KEY.Replace("-----BEGIN PUBLIC KEY-----", "");
            key = key.Replace("-----END PUBLIC KEY-----", "");
            key = key.Replace("\\n", "");
            key = key.Replace(" ", "");
            byte[] keyByte = Convert.FromBase64String(key);
            byte[] seqOid = { 0x30, 0x0D, 0x06, 0x09, 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01, 0x05, 0x00 };
            using (MemoryStream mem = new MemoryStream(keyByte))
            {
                using (BinaryReader binr = new BinaryReader(mem))
                {
                    try
                    {
                        ushort twobytes = binr.ReadUInt16();
                        switch (twobytes)
                        {
                            case 0x8130:
                                binr.ReadByte();
                                break;
                            case 0x8230:
                                binr.ReadInt16();
                                break;
                            default:
                                return null;
                        }
                        byte[] seq = binr.ReadBytes(15);
                        if (!CompareByteArrays(seq, seqOid))
                        {
                            return null;
                        }
                        twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8103)
                        {
                            binr.ReadByte();
                        }
                        else if (twobytes == 0x8203)
                        {
                            binr.ReadInt16();
                        }
                        else
                        {
                            return null;
                        }
                        byte bt = binr.ReadByte();
                        if (bt != 0x00)
                        {
                            return null;
                        }
                        twobytes = binr.ReadUInt16();
                        if (twobytes == 0x8130)
                        {
                            binr.ReadByte();
                        }
                        else if (twobytes == 0x8230)
                        {
                            binr.ReadInt16();
                        }
                        else
                        {
                            return null;
                        }
                        twobytes = binr.ReadUInt16();
                        byte lowbyte = 0x00;
                        byte highbyte = 0x00;
                        if (twobytes == 0x8102)
                        {
                            lowbyte = binr.ReadByte();
                        }
                        else if (twobytes == 0x8202)
                        {
                            highbyte = binr.ReadByte();
                            lowbyte = binr.ReadByte();
                        }
                        else
                        {
                            return null;
                        }
                        byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                        int modsize = BitConverter.ToInt32(modint, 0);
                        byte firstbyte = binr.ReadByte();
                        binr.BaseStream.Seek(-1, SeekOrigin.Current);
                        if (firstbyte == 0x00)
                        {
                            binr.ReadByte();
                            modsize -= 1;
                        }
                        byte[] modulus = binr.ReadBytes(modsize);
                        if (binr.ReadByte() != 0x02)
                        {
                            return null;
                        }
                        int expbytes = binr.ReadByte();
                        byte[] exponent = binr.ReadBytes(expbytes);
                        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
                        RSAParameters rsaKeyInfo = new RSAParameters
                        {
                            Modulus = modulus,
                            Exponent = exponent
                        };
                        rsa.ImportParameters(rsaKeyInfo);
                        return rsa;
                    }
                    catch (Exception)
                    {
                        return null;
                    }
                }
            }
        }

        /// <summary>
        /// get private key
        /// </summary>
        /// <returns></returns>
        private static RSACryptoServiceProvider getPrivateKey()
        {
            String key = gatewayCfg.PRIVATE_KEY.Replace("-----BEGIN RSA PRIVATE KEY-----", "");
            key = key.Replace("-----END RSA PRIVATE KEY-----", "");
            key = key.Replace("\\n", "");
            key = key.Replace(" ", "");
            byte[] keyByte = Convert.FromBase64String(key);
            byte[] MODULUS, E, D, P, Q, DP, DQ, IQ;
            MemoryStream mem = new MemoryStream(keyByte);
            BinaryReader binr = new BinaryReader(mem);
            byte bt = 0;
            ushort twobytes = 0;
            int elems = 0;
            try
            {
                twobytes = binr.ReadUInt16();
                if (twobytes == 0x8130)
                {
                    binr.ReadByte();
                }
                else if (twobytes == 0x8230)
                {
                    binr.ReadInt16();
                }
                else
                {
                    return null;
                }
                twobytes = binr.ReadUInt16();
                if (twobytes != 0x0102)
                {
                    return null;
                }
                bt = binr.ReadByte();
                if (bt != 0x00)
                {
                    return null;
                }
                elems = GetIntegerSize(binr);
                MODULUS = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                E = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                D = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                P = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                Q = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                DP = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                DQ = binr.ReadBytes(elems);
                elems = GetIntegerSize(binr);
                IQ = binr.ReadBytes(elems);
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSAParameters RSAparams = new RSAParameters();
                RSAparams.Modulus = MODULUS;
                RSAparams.Exponent = E;
                RSAparams.D = D;
                RSAparams.P = P;
                RSAparams.Q = Q;
                RSAparams.DP = DP;
                RSAparams.DQ = DQ;
                RSAparams.InverseQ = IQ;
                RSA.ImportParameters(RSAparams);
                return RSA;
            }
            catch (Exception)
            {
                return null;
            }
            finally
            {
                binr.Close();
            }
        }

        /// <summary>
        /// compare byte arrays
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;
            int i = 0;
            foreach (byte c in a)
            {
                if (c != b[i])
                    return false;
                i++;
            }
            return true;
        }

        /// <summary>
        /// get interger size
        /// </summary>
        /// <param name="binr"></param>
        /// <returns></returns>
        static int GetIntegerSize(BinaryReader binr)
        {
            byte bt = 0;
            byte lowbyte = 0x00;
            byte highbyte = 0x00;
            int count = 0;
            bt = binr.ReadByte();
            if (bt != 0x02)
            {
                return 0;
            }
            bt = binr.ReadByte();
            if (bt == 0x81)
            {
                count = binr.ReadByte();
            }
            else if (bt == 0x82)
            {
                highbyte = binr.ReadByte();
                lowbyte = binr.ReadByte();
                byte[] modint = { lowbyte, highbyte, 0x00, 0x00 };
                count = BitConverter.ToInt32(modint, 0);
            }
            else
            {
                count = bt;
            }
            while (binr.ReadByte() == 0x00)
            {
                count -= 1;
            }
            binr.BaseStream.Seek(-1, SeekOrigin.Current);
            return count;
        }

        /// <summary>
        /// Return base64 after sorting arguments
        /// </summary>
        /// <param name="param">param</param>
        /// <returns>return base64 params</returns>
        private static String sortedAfterToBased64(String json)
        {
            byte[] jsonBytes = stringToBytes(json);
            String jsonBase64 = bytesToBase64(jsonBytes);
            return jsonBase64;
        }

        /// <summary>
        /// Generate an IV based on the data encryption key
        /// </summary>
        /// <param name="symmetricKey">key</param>
        /// <returns>return iv</returns>
        private static byte[] generateIv(String symmetricKey)
        {
            byte[] data = stringToBytes(symmetricKey);
            MD5 md5 = MD5.Create();
            byte[] result = md5.ComputeHash(data);
            return result;
        }

        /// <summary>
        /// UTF8 String to bytes
        /// </summary>
        /// <param name="data">data</param>
        /// <returns>bytes</returns>
        private static byte[] stringToBytes(String data)
        {
            return Encoding.UTF8.GetBytes(data);
        }

        /// <summary>
        /// String to base64
        /// </summary>
        /// <param name="data">data</param>
        /// <returns>string base64</returns>
        private static String stringToBase64(String data)
        {
            byte[] dataBytes = stringToBytes(data);
            return bytesToBase64(dataBytes);
        }

        /// <summary>
        /// String to bytes
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>string</returns>
        private static String bytesToString(byte[] bytes)
        {
            return Encoding.UTF8.GetString(bytes);
        }

        /// <summary>
        /// Bytes to hex
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>hex</returns>
        private static String bytesToHex(byte[] bytes)
        {
            StringBuilder hex = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                hex.AppendFormat("{0:x2}", b);
            }
            return hex.ToString();
        }

        /// <summary>
        /// Hex to bytes
        /// </summary>
        /// <param name="hex">hex</param>
        /// <returns>bytes</returns>
        private static byte[] hexToBytes(String hex)
        {
            if (hex.Length % 2 == 1)
            {
                throw new Exception("The binary key cannot have an odd number of digits");
            }
            byte[] data = new byte[hex.Length >> 1];
            for (int i = 0; i < hex.Length >> 1; ++i)
            {
                int a = (int)hex[i << 1];
                int b = (int)hex[(i << 1) + 1];
                data[i] = (byte)(((a - (a < 58 ? 48 : (a < 97 ? 55 : 87))) << 4) + (b - (b < 58 ? 48 : (b < 97 ? 55 : 87))));
            }
            return data;
        }

        /// <summary>
        /// Bytes to base64
        /// </summary>
        /// <param name="bytes">bytes</param>
        /// <returns>string</returns>
        private static String bytesToBase64(byte[] bytes)
        {
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Base64 to bytes
        /// </summary>
        /// <param name="base64">base64</param>
        /// <returns>bytes</returns>
        private static byte[] base64ToBytes(String base64)
        {
            return Convert.FromBase64String(base64);
        }
    }
}