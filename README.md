# faq chat bot

## Setting up your Service

1.  Setting up your repository

    1.  If you have not already created your repository, run
        `plinth project publish`. This will create a GitHub repository under the
        Zocdoc org, based on the name of the project.

1.  Setting up ECR

    1.  Create a branch on https://github.com/Zocdoc/ecr
    2.  Add an entry for your service to [cdk/src/services.ts](https://github.com/Zocdoc/ecr/blob/master/cdk/src/services.ts)
    3.  Put up a PR and merge it in once it is approved.

 1. Setting up sentry
    1.  Ensure your AWS credentials [are configured](https://zocdoc.atlassian.net/wiki/spaces/TECH/pages/39425287/How+to+set+up+AWS+configuration+on+your+mac)
    1.  Run `plinth sentry --repo faq-chatbot --project faq-chatbot  --team [team name, as listed on https://sentry.io/settings/zocdoc/teams/)]`
    1.  More detailed information on Sentry and setup instructions can be found in [Confluence](https://zocdoc.atlassian.net/wiki/spaces/TECH/pages/39430726/Sentry)

1.  Adding Basic Alerting

    1. Get a PagerDuty webhook; most teams will already have this.
    1. The PagerDuty url is associated with your service by setting
       `alarmWebhook` on the [`LoadBalancerProps`][lb-props] object.

1.  Setting up TeamCity

    1. Go to the [TeamCity admin screen] and click on the "Create Project" button.
       ![Admin Screen](https://github.com/zocdoc/plinth/raw/master/docs/images/tc_00_admin_screen.png?raw=true)

       > If you do not see the "Create Project" button, try looking for "New Subproject" in the appropriate parent project context. If you are unable to create a new project, verify your permissions with someone on your team who has administrative permissions, or, ask the Platform team for help.

    1. Select a root project that is associated with the appropriate team.
       For example, if you are adding a project the Platform team would own, you
       would select _Platform_ from the root project dropdown list.
    1. Select the _From a repository URL_ tab.
    1. Add the git url to your repository: `git@github.com:Zocdoc/faq-chatbot.git`
       ![create project screen](https://github.com/zocdoc/plinth/raw/master/docs/images/tc_01_create_project_screen.png?raw=true)
    1. Click "Proceed" (no need to fill in _username_ or _Password / access token_) <!-- is this true in general? or does this vary by project -->
    1. If all is well so far you should see: "✓ The connection to the VCS repository has been verified" at the top of the page.
       ![create project](https://github.com/zocdoc/plinth/raw/master/docs/images/tc_02_create_project_from_url_screen.png?raw=true)
    1. Select the radio button for "Import settings from .teamcity/settings.kts and enable synchronization with the VCS repository"
    1. Correct the project name if needed, confirm the default brach is what you
       expect (generally the default should read `refs/heads/main`).
    1. Update the branch specification to include tags, making the field:

    ```
    refs/heads/(*)
    refs/tags/(*)
    ```

    1. Click "Proceed", and wait for team city to bake a new project (might take a minute or so).
    1. Navigate to the _VCS roots_ and then select edit on the single VCS root in the list.
    1. Check _Use tags as branches_ and save.
    1. If all went well you should have a working team city project. If something is
       not working please reach out in #platform (on slack).

1.  Set up teamcity for PR201
    1. Make sure your new project can go through teamcity CI/Staging successfully.
    1. You should have access to [pr201 teamcity](https://pr201teamcity.east.zocdoccloud.com/), but if you dont, ask SRE to give you access, and make sure you have the permission/role to view projects. 
    1. Set up sentry for pr201
       1. Make sure you have a `pr201` profile in your `.aws/config` file, and you have permission to assume that role.
       1. Run `plinth sentry --project interop-platform-api --phi --team [team name, as listed on https://sentry.io/settings/zocdoc/teams/)]`.
       1. From aws console, switch to a phi/pr201 role, and make sure you can find a SSM in [parameter store](https://us-east-1.console.aws.amazon.com/systems-manager/parameters/?region=us-east-1&tab=Table#list_parameter_filters=Name:Contains:%2Fsentry%2Fprojects%2F) like this: `/sentry/projects/[your-project-name]/dsn_public`.
    1. In your CI deployment step, [add this step](https://github.com/Zocdoc/zopentable/blob/main/.teamcity/settings.kts#L164-L166).
    1. Make sure your service has a `pr201` entry for DOTNET_ENVIRONMENT env variable ([Example](https://github.com/Zocdoc/zopentable/blob/main/cdk/src/cdk.ts#L86C10-L86C10)).
    1. Add your project to [teamcity-kotlin-pr201 repo](https://github.com/Zocdoc/teamcity-kotlin-pr201/tree/main/.teamcity)
       1. Add to your team's project list ([Example](https://github.com/Zocdoc/teamcity-kotlin-pr201/blob/main/.teamcity/infrastructure/platform/PlatformProject.kt#L10)).
       1. Add details for your new project to be deployed to pr201. ([Example](https://github.com/Zocdoc/teamcity-kotlin-pr201/blob/main/.teamcity/infrastructure/platform/Zopentable.kt)).
       1. The pr201 project should run automatically after each successful main CI run. 
       
3.  Search for TODO in this document and fill them in:

    1. Background
    1. TeamCity links

1.  Delete this section; you won't need it ever again.

If you have trouble setting up your service or working with your new C# service please ask questions in the #optometrists slack channel.


[lb-props]: https://github.com/Zocdoc/frontend-common/blob/09290064e6c78389fda01eedbe4b688499b6153e/packages/utils/zd-cdk/src/ecs/loadBalancer.ts#L27
[Go to TeamCity]: https://teamcity.east.zocdoccloud.net/admin/admin.html?item=projects

## Table of Contents

1.  [Background](#background)
1.  [Architecture](#architecture)
1.  [Development](#development)
    1.  [Setting up DEV certificates](#setting-up-DEV-certificates)
    1.  [Prerequisites](#prerequisites)
    1.  [Commands](#commands)
    1.  [Adding A Route](#adding-a-route)
    1.  [Dependency Injection](#dependency-injection)
    1.  [Fakes](#fakes)
    1.  [Writing Tests](#writing-tests)
        1. [Unit Tests](#unit-tests)
        1. [API Tests](#api-tests)
    1.  [Adding AWS Dependencies](#adding-aws-dependencies)
        1. [Local Development](#local-development)
        1. [CI and Production](#ci-and-production)
1.  [CI](#ci)
    1.  [My CI run broke](#my-ci-run-broke)
    1.  [Published Artifacts](#published-artifacts)
1.  [Deployment](#deployment)
1.  [Alerts](#alerts)

## Background

TODO: What is the business value of this service? What should people know about to be successful?

### Architecture

TODO: Add a high level diagram of your service.

<p align="center">
    <img width="700" src="https://github.com/zocdoc/approved-reviews-service/raw/master/images/architecture-diagram.png?raw=true">
    <br/>
    <a href="https://docs.google.com/presentation/d/1j_MZhra6JWVDXT1StyTjMLn8K0SsIoTU8k8NL73gFIo/edit?usp=sharing" style="text-align: center; font-size: 10px">
      Edit diagram here
    </a>
</p>

## Development

### Prerequisites

This solution uses a handful of technologies:

- [dotnet core](https://dotnet.microsoft.com/download)
- [docker](https://docs.docker.com/docker-for-mac/install/)
- [plinth](https://github.com/Zocdoc/plinth)
- [Nuget Setup](https://zocdoc.atlassian.net/wiki/spaces/TECH/pages/39421132/Setting+up+Nuget+for+Artifactory)
  - Note: You need to be on VPN to use artifactory

You can develop using:

- [Visual Studio Code](https://code.visualstudio.com/) with the [C# for Visual Studio Code](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.csharp) extension
- [Rider](https://www.jetbrains.com/rider/)

### Commands

All the common commands are available in your `Makefile`:

- `make build`: Generate your API and compile the solution
  - Note: You need to be on VPN to restore dependencies from artifactory
- `make setup`: Run any dependency setup (i.e. a database or localstack)
- `make unit-test`: Run any unit tests. If you have SQL tests this will require `make setup` to be run before it.
- `make run`: Run your service. It will be available on `http://localhost:5000 and https://localhost:5001`
- `make run-fake`: Run your fake. It will be available on `http://localhost:5000 and https://localhost:5001`
- `make api-test`: Run API tests against your running service.
- `make fake-api-test`: Run API tests against your running fake service.

### Adding a route

The routing for your API is controlled by the swagger file `service.yaml`. To add a new route:

1. Define the route in the swagger file.
1. Run `make build` to re-run code generation for the API
   1. If you get swagger generation errors, double check that your new swagger file is valid
   2. You will get build errors; you will need to add new methods to `FaqChatbotImpl.cs` to handle
      your new routes.
1. Write API tests that hit your new route on your live service and fake

For details on swagger syntax go [here](https://swagger.io/docs/specification/about/)

### Dependency Injection

Our C# services are similar to the monolith, in that they provide attributes that you can add to classes so that
they are automatically dependency injected at run time:

```
    interface IMyService {
        Task DoIt()
    }

    [RegisterService]
    public class MyService : IMyService {
        public MyService(IOtherService otherService) { ... }
        public async Task DoIt() { ... }
    }
```

See the [standards](https://github.com/Zocdoc/standards/tree/master/CSharp) documentation for more details and examples.

### Fakes

This service automatically builds a fake version of your service meant to be used by your consumers in their testing.

- What so it makes a mock? Don't my consumers do that for me?

  - While similar to a mock, a fake uses the same code as your service to respond to your consumers tests. This means your consumers
    will be testing against the exact same contract and return values as your production service, so we can be more confident
    that the tests actually reflect the reality of the service.

- Isn't that just my service?

  - The key difference is that your fake cannot have any other dependencies; so if your service has a SQL database than
    your fake needs to have a fake version of that dependency?

- How do I provide a fake version?
  - Normally when we do dependency injection we use the `[RegisterService]` attribute so that an implementation of an interface is automatically injected into other classes. For a class that interacts with a dependency, you should instead use
    `[RegisterFakeableService]` and also in `FaqChatbot.Fake` provide a fake implementation of the service with the attribute
    `[RegisterFakeService()]`. In the case of a data store a simple fake could be an in memory dictionary.

See the [standards]() documentation for more details and examples.

### Writing Tests

There are two tests solutions in this repository: UnitTests and ApiTests.

#### Unit Tests

In the `UnitTest` project you should write unit tests for the business logic of individual classes. This project should also contain integration tests with dependencies like a database or several classes working in concert.

There are some utilities available for writing your integration tests:

- Inherit your test class from `TestFixture` or `ServiceTestFixture`
- Use [Moq](https://github.com/Moq/moq4/wiki/Quickstart) to mock out dependencies. Hooks for this are provided on `TestFixture`.
- Use [FluentAssertions](https://fluentassertions.com/) to assert test state.

See the [standards](https://github.com/Zocdoc/standards/tree/master/CSharp) documentation for more details and examples.

#### API Tests

In the `ApiTests` project tests you should write tests that make HTTP requests to endpoints on your service. Your test dependencies should be defined in `docker-compose.yml` and any initialization of your dependencies should be performed in the `Makefile` under the `setup` command.

There are some utilities available for writing your integration tests:

- `ZocDoc.ApiTests.ApiTestHelpers`: Functions to make HTTP requests that log the request information into the console.

### Adding AWS Dependencies

#### Local Development

For development, we do not use real AWS resources; we instead use `docker compose` to launch versions of the resources on your machine. Most resources are available through [localstack](https://github.com/localstack/localstack). For example, here this configures localstack to setup SNS and SQS and set an environment variable for the service to read:

```
services:
  web:
    ...
    environment:
      ...
      - SQS_SERVICE_URL=http://localstack:4576
    depends_on:
      - localstack

  localstack:
    image: localstack/localstack
    ports:
      - "4575:4575" # local sns port
      - "4576:4576" # local sqs port
    environment:
      - SERVICES=sns,sqs
```

Other databases and caches like ElasticSearch, PostGRES, or Redis are available as standalone docker images.

Any initialization of your resources required for testing should be done in the `Makefile` within the `setup` command. To set up AWS resources on localstack you can use the AWS CLI while specifying the `--endpoint-url`.

For example, to hook up SNS to SQS you could do:

```
make setup:
	@aws sqs create-queue --queue-name testqueue --endpoint-url http://localhost:4576
	@aws --endpoint-url=http://localhost:4575 sns create-topic --name my-topic
	@aws sns subscribe --protocol sqs --topic-arn "arn:aws:sns:us-east-1:123456789012:my-topic" --endpoint-url http://localhost:4575 --notification-endpoint "http://localhost:4576/queue/testqueue"
```

For more examples of using localstack and databases with C# check out the [standards](https://github.com/Zocdoc/standards/tree/master/CSharp) documentation.

#### CI and Production

We use cdk and Cloudformation to provision AWS resources.

## CI

CI can be found [in Teamcity](TODO:ADD_LINK_TO_TEAMCITY) and is generated based on the kotlin in `.teamcity\settings.kts`.

CI uses a fully containerized version of the application to run three sets of tests:

1. Unit tests
2. API tests against the full service
3. API tests against the fake service

### My CI run broke

In most circumstances, you can replicate the error by just running the application and test suite directly on your machine. However, there might be occasional differences when running the tests against a container. To do this locally:

1. Make sure you have no other containers lying around your machine: `kill $(docker ps -q) && docker rm $(docker ps -aq)`
1. Go to the `settings.kts` file and run each bash command in order for your build step.
1. Inspect your database, application state, or docker logs (i.e. `docker compose logs web`) to figure out whats going on.

### Published Artifacts

A successful run against the `master` branch will add a tag on the git repository and emit two containers tagged in ECR
in the format `version_DATE`:

- The application
- The fake

The fake can be consumed by other services by including it in their `docker-compose.yml` files:

```
faq-chatbotfake:
  container_name: abfake
  image: 038405485655.dkr.ecr.us-east-1.amazonaws.com/faq-chatbot:fake_VERSION_TAG
  ports:
    - "5000:443"
  expose:
    - "443"
  environment:
    - ASPNETCORE_ENVIRONMENT=Fake # NOTE: don't forget this variable, otherwise the fake won't initialize correctly
```

## Deployment

Deployment to production is performed manually [in Teamcity](TODO:ADD_LINK_TO_TEAMCITY) by selecting a version tag
and running it in the TeamCity UI.

Production deployment is dependent on the same steps successfully running in our staging environment which happens
automatically on each merge into master that successfully passes CI.
