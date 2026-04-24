using System;
using Npgsql;

namespace MidgardAddressBook.DAL.Configuration;

/// <summary>
/// Helpers that translate Render-style URLs (e.g. <c>postgres://user:pass@host:port/db</c>) into
/// the formats expected by Npgsql and StackExchange.Redis.
/// </summary>
public static class ConnectionStringTranslator
{
    /// <summary>
    /// Converts a <c>postgres://</c> or <c>postgresql://</c> URL into an Npgsql connection string.
    /// If the input is already a key/value Npgsql connection string, it is returned unchanged.
    /// </summary>
    /// <param name="value">URL or Npgsql connection string.</param>
    /// <returns>An Npgsql-compatible connection string, or <c>null</c> if <paramref name="value"/> is null/empty.</returns>
    public static string? ToNpgsqlConnectionString(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
            && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            // Assume it is already a key/value Npgsql connection string.
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var username = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : string.Empty;
        var database = uri.AbsolutePath.TrimStart('/');

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort ? 5432 : uri.Port,
            Username = username,
            Password = password,
            Database = database,
            SslMode = SslMode.Prefer,
        };

        return builder.ConnectionString;
    }

    /// <summary>
    /// Converts a <c>redis://</c> or <c>rediss://</c> URL into a StackExchange.Redis configuration string.
    /// If the input is already a host:port style config, it is returned unchanged.
    /// </summary>
    /// <param name="value">URL or StackExchange.Redis config string.</param>
    /// <returns>A StackExchange.Redis compatible configuration string, or <c>null</c> if <paramref name="value"/> is null/empty.</returns>
    public static string? ToRedisConfiguration(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var isRediss = value.StartsWith("rediss://", StringComparison.OrdinalIgnoreCase);
        var isRedis = value.StartsWith("redis://", StringComparison.OrdinalIgnoreCase);

        if (!isRedis && !isRediss)
        {
            return value;
        }

        var uri = new Uri(value);
        var host = uri.Host;
        var port = uri.IsDefaultPort ? 6379 : uri.Port;
        var config = $"{host}:{port}";

        if (!string.IsNullOrEmpty(uri.UserInfo))
        {
            var parts = uri.UserInfo.Split(':', 2);
            var password = parts.Length > 1 ? Uri.UnescapeDataString(parts[1]) : Uri.UnescapeDataString(parts[0]);
            if (!string.IsNullOrEmpty(password))
            {
                config += $",password={password}";
            }
        }

        if (isRediss)
        {
            config += ",ssl=true";
        }

        return config;
    }
}
