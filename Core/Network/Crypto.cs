using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace CC98.Kernel.Network;

public static class Crypto
{
    /// <summary>
    /// 核心加密实现。
    /// </summary>
    /// <param name="PlainText"></param>
    /// <param name="Key"></param>
    /// <param name="IV"></param>
    /// <returns></returns>
    public static string EncryptStringToHex(string PlainText, string Key, string IV)
    {
        byte[] iv = Encoding.UTF8.GetBytes(IV.PadRight(16, ' ')[..16]);
        byte[] key = Encoding.UTF8.GetBytes(Key.PadRight(16, ' ')[..16]);
        using (Aes aes = Aes.Create())
        {
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CFB;   // CFB 模式
            aes.Padding = PaddingMode.None; // 允许任意长度明文
            aes.FeedbackSize = 128;
            using (ICryptoTransform encryptor = aes.CreateEncryptor())
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                //使用提前填充
                byte[] plainBytes = PadWithZeros(PlainText);
                cs.Write(plainBytes, 0, plainBytes.Length);
                cs.FlushFinalBlock();
                return Convert.ToHexString(ms.ToArray()).ToLower();
            }
        }
    }
    /// <summary>
    /// 将输入字符串按 UTF-8 编码后补足到 16 字节整数倍，不足部分补 0x00。
    /// </summary>
    public static byte[] PadWithZeros(string plainText)
    {
        if (plainText == null) throw new ArgumentNullException(nameof(plainText));

        byte[] raw = Encoding.UTF8.GetBytes(plainText);
        int len = raw.Length;
        int pad = 16 - (len & 15);          // 计算需要补多少字节
        if (pad == 16) pad = 0;             // 刚好 16 的倍数时不补

        byte[] padded = new byte[len + pad];
        Array.Copy(raw, 0, padded, 0, len); // 原始数据
                                            // 剩余部分默认为 0，无需再写
        return padded;
    }
    /// <summary>
    /// 将字符串分别转化为ACSLL码。
    /// </summary>
    /// <param name="Origin"></param>
    /// <returns></returns>
    public static string StringToAscll(string Origin)
    {

        byte[] asciiBytes = Encoding.ASCII.GetBytes(Origin);
        var sb = new StringBuilder(asciiBytes.Length * 2);
        foreach (byte b in asciiBytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}