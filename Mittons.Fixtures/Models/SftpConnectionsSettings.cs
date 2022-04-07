namespace Mittons.Fixtures.Models
{
    public class SftpConnectionSettings
    {
        public string Host { get; set; }

        public int Port { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }

        public Fingerprint RsaFingerprint { get; set; }

        public Fingerprint Ed25519Fingerprint { get; set; }
    }
}