using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CC98.Kernel.Authorize;
/// <summary>
/// 提供基于 VPN 服务的 HTTP 请求转发工具。
/// </summary>
public partial class TokenHandler(ITokenService tokenService) : DelegatingHandler
{
    private readonly SemaphoreSlim refreshLock = new(1, 1);
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        
        //添加Bearer Token
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenService.AccessToken);

        var response = await base.SendAsync(request, cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            var clonedRequest = await CloneHttpRequestMessageAsync(request, cancellationToken);
            await refreshLock.WaitAsync(cancellationToken);
            try
            {
                //检查令牌是否已经被其他线程刷新成功（即不再过期）        
                if (tokenService.IsTokenExpired())
                {
                    // 令牌仍过期，需要当前线程负责刷新            
                    var tokenResponse = await tokenService.GetNewTokenAsync(cancellationToken);
                    if (tokenResponse == null || tokenResponse.IsError)
                        throw new UnauthorizedAccessException($"令牌刷新失败: {tokenResponse?.Error}", tokenResponse?.Exception);
                }
                //使用当前令牌设置 Authorization 头（已被刷新或由其他线程刷新）
                clonedRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenService.AccessToken);
                //重新发送请求
                response = await base.SendAsync(clonedRequest, cancellationToken);
            }
            //不要捕捉我们自己抛出的 UnauthorizedAccessException
            catch (Exception ex) when (ex is not UnauthorizedAccessException)
            {
                throw;
            }
            finally
            {
                refreshLock.Release();
            }
        }

        return response;
    }

    private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request, CancellationToken cancellationToken = default)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri);

        // 复制 Headers
        foreach (var header in request.Headers)
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

        // 复制 Content
        if (request.Content != null)
        {
            var contentStream = await request.Content.ReadAsStreamAsync();
            var memoryStream = new MemoryStream();
            await contentStream.CopyToAsync(memoryStream);
            memoryStream.Position = 0;

            clone.Content = new StreamContent(memoryStream);
            foreach (var header in request.Content.Headers)
                clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        return clone;
    }

}
