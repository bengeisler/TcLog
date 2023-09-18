:blue_heart: :+1: Thanks for your time and effort in contributing to this project! :+1: :blue_heart: 

There are several ways to contribute to this project. You can report bugs, suggest enhancements or directly contribute code via pull requests.

### Reporting bugs
 
* Please use the GitHub issue search feature to check if your bug is already reported.
* If it is not reported yet, please provide a detailed description of the problem, including the steps to reproduce it.
* If possible, please provide a minimalistic code example that reproduces the problem.
* If possible, please provide screenshots that illustrate the problem.
  
### Suggesting enhancements

* Please use the GitHub issue search feature to check if your enhancement is already suggested.
* If it is not suggested yet, please provide a detailed description of your proposal, including the use case behind it.
* If possible, please provide a minimalistic code example that illustrates your proposal.

### Pull requests

Please make sure that your pull request 
  * is based on the `main` branch
  * only contains related commits
  * contains a detailed description of the changes
  * contains tests for the changes


### Environment setup

#### TwinCAT and Visual Studio 
The project uses TwinCAT 3.1.4024.29 and Visual Studio 2019 with .NET 7.0. 

Please use the recommended [Zeugwerk IDE settings.](https://doc.zeugwerk.dev/contribute/recom_xae_settings.html)

#### Unit tests

[TcUnit](https://tcunit.org/) is used for unit tests in plc project itself.

These tests are complemented by [XUnit](https://xunit.net/) tests in the .NET project. These tests mainly cover the the file-system related actions, since these are way easier to implement in .NET than in TwinCAT.

#### Code style

The project uses the [TcBlack](https://github.com/Roald87/TcBlack) formatter for TwinCAT code. Please make sure to format your code accordingly before creating a pull request. 

#### Naming conventions

This project uses [Zeugwerk naming conventions](https://doc.zeugwerk.dev/contribute/contribute_code.html#naming-conventions) for the TwinCAT code.

Unfortunately, the Beckhoff PLC Static Analysis does not support checking custom naming conventions yet. Therefore, please make sure to check your code manually before creating a pull request.