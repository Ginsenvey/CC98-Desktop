using System;
using System.Collections.Generic;
using Windows.Security.Credentials;
using System.Linq;
namespace CC98.Services;

/// <summary>
/// 本地化密码管理
/// </summary>
public static class PasswordManager
{
    /// <summary>
    /// 保存密码到保险库
    /// </summary>
    /// <param name="username">用户名/标识</param>
    /// <param name="password">密码</param>
    /// 用户名均使用"CC98".
    public static void SavePassword(string password, string resource, string username = "CC98")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空");

        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("密码不能为空");
        if (string.IsNullOrEmpty(resource))
            throw new ArgumentException("资源名不能为空");
        var vault = new PasswordVault();

        // 如果凭据已存在则先删除
        RemovePassword(username, resource);

        // 创建并添加新凭据
        var cred = new PasswordCredential(resource, username, password);
        vault.Add(cred);
    }

    /// <summary>
    /// 从保险库检索密码
    /// </summary>
    /// <param name="username">用户名/标识</param>

    /// <returns>检索到的密码，未找到返回 null</returns>
    public static string RetrievePassword(string resource, string username = "CC98")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空");
        var vault = new PasswordVault();
        try
        {
            // 尝试检索凭据
            var cred = vault.Retrieve(resource, username);
            cred.RetrievePassword(); // 显式获取密码值
            return cred.Password;
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
        {
            return null;
        }
    }

    /// <summary>
    /// 从保险库删除密码
    /// </summary>
    /// <param name="username">用户名/标识</param>

    public static void RemovePassword(string resource, string username = "CC98")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空");

        var vault = new PasswordVault();


        try
        {
            // 尝试查找并删除凭据
            var cred = vault.Retrieve(resource, username);
            vault.Remove(cred);
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
        {
            // 凭据不存在，无需操作
        }
    }

    /// <summary>
    /// 检查是否存在指定凭据
    /// </summary>
    /// <param name="username">用户名/标识</param>

    /// <returns>凭据是否存在</returns>
    public static bool PasswordExists(string resource, string username = "CC98")
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("用户名不能为空");

        var vault = new PasswordVault();
        try
        {
            // 尝试检索凭据
            vault.Retrieve(resource, username);
            return true;
        }
        catch (Exception ex) when (ex.HResult == unchecked((int)0x80070490)) // 元素未找到
        {
            return false;
        }
    }

    /// <summary>
    /// 获取所有保存的凭据（当前资源）
    /// </summary>

    /// <returns>凭据列表</returns>
    public static IReadOnlyList<PasswordCredential> GetAllCredentials(string resource)
    {
        var vault = new PasswordVault();
        try
        {
            // 检索所有凭据并过滤当前资源的
            return vault.RetrieveAll()
                .Where(c => c.Resource == resource)
                .ToList()
                .AsReadOnly();
        }
        catch
        {
            return new List<PasswordCredential>().AsReadOnly();
        }
    }

    /// <summary>
    /// 清除当前应用的所有凭据
    /// </summary>

    public static void ClearAllPasswords(string resource)
    {
        var vault = new PasswordVault();
        try
        {
            // 检索并删除所有当前资源的凭据
            var credentials = vault.RetrieveAll()
                .Where(c => c.Resource == resource)
                .ToList();

            foreach (var cred in credentials)
            {
                vault.Remove(cred);
            }
        }
        catch
        {
            // 忽略错误
        }
    }
    public static void Logout()
    {
        PasswordManager.ClearAllPasswords("Access");
        PasswordManager.ClearAllPasswords("Refresh");
        PasswordManager.ClearAllPasswords("Ticket");
        PasswordManager.ClearAllPasswords("Route");
        PasswordManager.ClearAllPasswords("VpnUserName");
        PasswordManager.ClearAllPasswords("VpnPassWord");
    }
}