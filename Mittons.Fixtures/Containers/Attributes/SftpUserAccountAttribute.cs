using System;

namespace Mittons.Fixtures.Containers.Attributes
{
    [AttributeUsage(System.AttributeTargets.Property, AllowMultiple = true)]
    public class SftpUserAccountAttribute : Attribute
    {
        public string Username { get; set; }

        public string Password { get; set; }

        public SftpUserAccountAttribute()
        {
        }

        public SftpUserAccountAttribute(string username, string password)
        {
            Username = username;
            Password = password;
        }
    }
}