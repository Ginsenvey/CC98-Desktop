using Duende.IdentityModel;
using Duende.IdentityModel.Client;
using Newtonsoft.Json.Serialization;
using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Web;
namespace CC98.Kernel.OpenID;
/// <summary>
/// 用于PKCE的OpenID认证流程生成
/// </summary>
public class OpenID()
{
    public (string url,string veri,string state) GenerateAuthLoop()
    {
        var (verifier, challenge) = GeneratePkce();
        string state = GenerateState();
        var request = new RequestUrl("https://openid.cc98.org/connect/authorize");//终结点
        var url = request.CreateAuthorizeUrl(
            responseType: "code",//授权码模式
            scope: "openid profile cc98-api cc98-card.all offline_access",
            redirectUri: "cc98://callback",//本地应用回环
            nonce: GenerateNonce(),
            state: state,
            responseMode: "query",//回环信息位于查询参数
            clientId: "d47a2448-779f-42f3-164f-08dd8896bbe5",
            codeChallenge:challenge,
            codeChallengeMethod:"S256"     
        );
        return (url,verifier,state);
    }
    static string GenerateNonce()
    {
        // 第一部分：高精度时间戳 (UTC 100ns 精度)
        long timestamp = DateTime.UtcNow.Ticks;

        // 第二部分：随机数 (32字节)
        byte[] randomBytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }
        string randomPart = UrlSafeBase64(randomBytes);

        // 第三部分：数字签名 (HMAC-SHA256)
        using var hmac = new HMACSHA256(randomBytes);
        byte[] signature = hmac.ComputeHash(
            Encoding.UTF8.GetBytes(timestamp + randomPart)
        );

        return $"{timestamp}.{randomPart}.{UrlSafeBase64(signature)}";
    }

    static string GenerateState()
    {
        // 第一部分：随机数 (24字节)
        byte[] randomBytes = new byte[24];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        // 第二部分：时间戳 (Unix 毫秒)
        long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // 第三部分：签名保护
        using var hmac = new HMACSHA256(randomBytes);
        byte[] signature = hmac.ComputeHash(
            BitConverter.GetBytes(timestamp)
        );

        return UrlSafeBase64(
            CombineArrays(randomBytes, BitConverter.GetBytes(timestamp), signature)
        );
    }

    
    static string UrlSafeBase64(byte[] data)
    {
        return Convert.ToBase64String(data)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    static byte[] CombineArrays(params byte[][] arrays)
    {
        int length = 0;
        foreach (var array in arrays) length += array.Length;

        byte[] result = new byte[length];
        int offset = 0;

        foreach (var array in arrays)
        {
            Buffer.BlockCopy(array, 0, result, offset, array.Length);
            offset += array.Length;
        }
        return result;
    }
    private (string Verifier, string Challenge) GeneratePkce()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
        var random = new Random();
        var verifier = new string(Enumerable.Repeat(chars, 128)
            .Select(s => s[random.Next(s.Length)]).ToArray());

        using var sha256 = SHA256.Create();
        var challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(verifier));
        var challenge =UrlSafeBase64(challengeBytes);

        return (verifier, challenge);
    }
    
}

