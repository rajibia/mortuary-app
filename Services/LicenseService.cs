using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace MortuaryApp.Services;

public static class LicenseService
{
    private const string Secret = "M0rtuary@pp!2026#Secure$Key";

    public static string GetMachineFingerprint()
    {
        var raw = GetMacAddress() + "-" + Environment.MachineName;
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        var hex = Convert.ToHexString(hash).ToUpper();
        return "MACH-" + string.Join("-", Enumerable.Range(0, 4).Select(i => hex.Substring(i * 4, 4)));
    }

    public static string GenerateLicenseKey(string machineFingerprint)
    {
        var normalized = machineFingerprint.Trim().ToUpper();
        var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(Secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        var hex = Convert.ToHexString(hash).ToUpper();
        return "LIC-" + string.Join("-", Enumerable.Range(0, 5).Select(i => hex.Substring(i * 4, 4)));
    }

    public static bool ValidateLicense(string licenseKey)
    {
        var expected = GenerateLicenseKey(GetMachineFingerprint());
        return string.Equals(licenseKey?.Trim().ToUpper(), expected, StringComparison.Ordinal);
    }

    private static string GetMacAddress()
    {
        try
        {
            var nic = NetworkInterface.GetAllNetworkInterfaces()
                .FirstOrDefault(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);
            var mac = nic?.GetPhysicalAddress().ToString();
            return !string.IsNullOrEmpty(mac) ? mac : "NOMAC";
        }
        catch { return "NOMAC"; }
    }
}
