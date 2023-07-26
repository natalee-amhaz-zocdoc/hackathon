# Welcome to your CDK TypeScript project!

This project describes the infrastructure for this service using the [CDK project](https://aws.amazon.com/cdk/).

The `cdk.json` file tells the CDK Toolkit how to execute your app.

## Setting up Artifactory

This project requires you to be authenticated to ZD artifactory, see setup instructions [here](https://zocdoc.atlassian.net/wiki/spaces/TECH/pages/39421483/Setting+up+NPM+with+Artifactory)

## AWS Credential Loading

The CDK project is an elaborate way to get your computer to send requests to AWS on your behalf.
To that end, it directly reads and processes your credential files in order to make the requests.
See [the setup documentation](https://zocdoc.atlassian.net/wiki/spaces/TECH/pages/39425287/How+to+set+up+AWS+configuration+on+your+mac) for more information on how to configure your machine correctly.

## Useful commands

### for project setup
* `nvm use`        use the repo specified node version https://github.com/nvm-sh/nvm
* `npm install`    install the current CDK project dependencies
### while making changes
* `npm run build`  compile typescript to js
* `npm run watch`  watch for changes and compile
* `npm run test`*  execute tests against CI account
* `npx cdk deploy`*    deploy this stack to your default AWS account/region
* `npx cdk diff`*      compare deployed stack with current state
* `npx cdk synth`*     emits the synthesized CloudFormation template

the `*` denotes sensitivity to environment variables as noted in the linked setup documentation.