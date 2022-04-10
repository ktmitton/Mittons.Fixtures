# Mittons.Fixtures

Mittons.Fixtures is a framework for standing up testing environments via fixtures, which can be integrated into other testing frameworks.

## Introduction

This project was created to allow developers to declare their environments in a fixture using attributes to define service settings, allowing the testing framework to
spin up the full environment. This saves developers the need to create pre/post test scripts that build/teardown the testing environment, and keeps the environment
definition closer to the actual tests.

## Documentation

Currently, this library uses Docker for spinning up the test environment. To define an environment, you would create a class extending the DockerEnvironmentFixture,
and define all the containers you wish to stand up. When you define a fixture, the following attributes can be applied:

* [Run(string)] - Defines the details for the run, this currently only accepts a string representing an environment variable from which to get an id to apply to all
resources created in the test environment. This allows you to use that identifier for post-test cleanup action in the instance the tests are cancelled prematurely and
normal cleanup behavior does not occur.
* [Network(string)] - Defines a Docker network to create.

Supported container types are:

* Container: This is the base class for all containers, so if an abstraction for your container type doesn't exist, this is where you would start. All containers support
the following attributes:
  * [Image(string)] - Defines the image used to create the container.
  * [Command(string)] - Defines the command Docker runs once the container is started.
  * [HealthCheck(Disabled = boolean, Command = string, Interval = TimeSpan, Timeout = TimeSpan, StartPeriod = TimeSpan, Retries = int)] - Defines the settings
  for Docker to perform health checks on the container.
  * [NetworkAlias(string, string)] - Defines a Docker network to which the container should be attached, and the alias by which other containers on the network can
  communicate with it.
* SftpContainer: Extending Container, this adds extra convenience functionality specific to spinning up and Sftp server. SftpContainers are built using the
  [atmoz/sftp:alpine](https://github.com/atmoz/sftp) image, and since that assumes certain behaviors, [Image] attributes will be ignored for these containers. In addtion
  to the normal Container attributes, SftpContainer supports the following attributes:
  * [SftpUserAccout(string, string)] - Adds an account to the sftp server using the provided username and password.

A good reference on how do define an environment can be found in the [Mittons.Fixtures.Example](https://github.com/ktmitton/Mittons.Fixtures/tree/main/Mittons.Fixtures.Example)
project, which is designed to demo this library.

## Example

**Note:** This example leverages the [SSH.NET library](https://github.com/sshnet/SSH.NET)

```csharp
public class ReportingEnvironmentFixture : DockerEnvironmentFixture
{
    [SftpUserAccount("admin", "securepassword")]
    [SftpUserAccount("tswift", "hatersgonnahate")]
    public SftpContainer SftpContainer { get; set; }
}

public class ConnectionSettingsTests : IClassFixture<ReportingEnvironmentFixture>
{
    private readonly ReportingEnvironmentFixture _reportingEnvironment;

    public ConnectionSettingsTests(ReportingEnvironmentFixture reportingEnvironment)
    {
        _reportingEnvironment = reportingEnvironment;
    }

    [Fact]
    public void Connect_WhenUsingTheAdminCredentials_ExpectASuccessfulConnection()
    {
        // Arrange
        var connectionSettings = _reportingEnvironment.SftpContainer.SftpConnectionSettings.Single(x => x.Key == "admin").Value;

        var connectionInfo = new ConnectionInfo(
                connectionSettings.Host,
                connectionSettings.Port,
                connectionSettings.Username,
                new PasswordAuthenticationMethod(connectionSettings.Username, connectionSettings.Password)
            );

        // Act
        using var client = new SftpClient(connectionInfo);

        client.Connect();

        // Assert
        Assert.True(client.IsConnected);
    }
}
```

## Contributing

### Thanks to all the people who have contributed!

[![contributors](https://contrib.rocks/image?repo=ktmitton/Mittons.Fixtures)](https://github.com/ktmitton/Mittons.Fixtures/graphs/contributors)

Made with [contrib.rocks](https://contrib.rocks).
