# Mittons.Fixtures

Mittons.Fixtures is a framework for standing up testing environments via fixtures, which can be integrated into other testing frameworks.

## Introduction

This project was created to allow developers to declare their environments in a fixture using attributes to define service settings, allowing the testing framework to
spin up the full environment. This saves developers the need to create pre/post test scripts that build/teardown the testing environment, and keeps the environment
definition closer to the actual tests. This project is broken down into multiple packages:

* Mittons.Fixtures
* Mittons.Fixtures.FrameworkExtensions.Xunit

### Mittons.Fixtures

The primary package of the project, its goal is to create fixtures that are test framework agnostic, so they can be wired up in any framework. Additionally, we want to restrict dependencies in this project as much as possible, ideally it only depends on .net standard 2.0 so it can be as compatibable as possible.

See the [README](Mittons.Fixtures/README.md) for more details on how to use this package.

### Mittons.Fixtures.FrameworkExtensions.Xunit

The goal of this package is to expose xunit specific classes so the fixtures defined in Mittons.Fixtures can be easily integrated into xunit. This is still under development.

## Contributing

### Thanks to all the people who have contributed!

[![contributors](https://contrib.rocks/image?repo=ktmitton/Mittons.Fixtures)](https://github.com/ktmitton/Mittons.Fixtures/graphs/contributors)

Made with [contrib.rocks](https://contrib.rocks).
