using System;

namespace Mittons.Fixtures.Docker.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class SftpUserAccount : Attribute
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public SftpUserAccount()
        {
        }

        public SftpUserAccount(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}