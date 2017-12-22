# Welcome to Serverless Image Resizer for C# Lambda

## What's in the package

1. README.md - This file
2. app-sam.yaml - Serverless Application Model template

## Here are some steps to follow from Visual Studio:

To deploy your Serverless application, right click the project in Solution Explorer and select *Publish to AWS Lambda*.

To view your deployed application open the Stack View window by double-clicking the stack name shown beneath the AWS CloudFormation node in the AWS Explorer tree. The Stack View also displays the root URL to your published application.

## Here are some steps to follow to get started from the command line:

Once you have edited your template and code you can use the following command lines to deploy your application from the command line (these examples assume the project name is *EmptyServerless*):

Restore dependencies
```
    cd "ServerlessImageResizing"
    dotnet restore
```

Execute unit tests
```
    cd "ServerlessImageResizing/test/ServerlessImageResizing.Tests"
    dotnet test
```

Deploy application
```
    cd "ServerlessImageResizing/src/ServerlessImageResizing"
    dotnet lambda deploy-serverless
```
