using Xunit;
using Moq;
using Mittons.Fixtures.Docker.Gateways;
using Mittons.Fixtures.Docker.Containers;
using System.Linq;
using Mittons.Fixtures.Models;
using System.Collections.Generic;
using System;

namespace Mittons.Fixtures.Tests.Unit.Docker.Containers
{
    public class SftpContainerTests
    {
        private readonly string sftpImageName = "atmoz/sftp";

        [Fact]
        public void Ctor_WhenCalled_ExpectTheContainerToUseTheAtmozImage()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new SftpContainer(gatewayMock.Object, Enumerable.Empty<Attribute>());

            // Assert
            gatewayMock.Verify(x => x.Run(sftpImageName, It.IsAny<string>()), Times.Once);
        }

        [Fact]
        public void Ctor_InitializedWithNoCredentials_ExpectTheCommandToSetupTheGuestAccount()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new SftpContainer(gatewayMock.Object, Enumerable.Empty<Attribute>());

            // Assert
            gatewayMock.Verify(x => x.Run(sftpImageName, "guest:guest"), Times.Once);
        }

        [Theory]
        [InlineData("testuser1", "testpassword1")]
        [InlineData("testuser2", "testpassword2")]
        [InlineData("guest", "guest")]
        public void Ctor_InitializedWithOneSetOfCredentials_ExpectTheCommandToSetupTheCredentials(string username, string password)
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new SftpContainer(
                    gatewayMock.Object,
                    new SftpUserAccount[]
                    {
                        new SftpUserAccount { Username = username, Password = password }
                    }
                );

            // Assert
            gatewayMock.Verify(x => x.Run(sftpImageName, $"{username}:{password}"), Times.Once);
        }

        [Fact]
        public void Ctor_InitializedWithMultipleSetOfCredentials_ExpectTheCommandToSetupTheCredentials()
        {
            // Arrange
            var gatewayMock = new Mock<IDockerGateway>();

            // Act
            using var container = new SftpContainer(
                    gatewayMock.Object,
                    new SftpUserAccount[]
                    {
                        new SftpUserAccount { Username = "testuser1", Password = "testpassword1" },
                        new SftpUserAccount { Username = "testuser2", Password = "testpassword2" },
                        new SftpUserAccount { Username = "guest", Password = "guest" }
                    }
                );

            // Assert
            gatewayMock.Verify(x => x.Run(sftpImageName, $"testuser1:testpassword1 testuser2:testpassword2 guest:guest"), Times.Once);
        }
    }
}