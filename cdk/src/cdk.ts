#!/usr/bin/env node
import { aws_dynamodb as dynamodb, aws_events } from 'aws-cdk-lib';
import { App, Stack, ecs, gitTag, dynamo, lambda, selectWithCurrentAccount, sns } from '@zocdoc-frontend-common/zd-cdk';
import { StreamViewType } from "aws-cdk-lib/aws-dynamodb";
import { Runtime, StartingPosition } from "aws-cdk-lib/aws-lambda";
import { DynamoEventSource, SqsDlq } from "aws-cdk-lib/aws-lambda-event-sources";

/*
TODO: Complete the setup steps in the README.md (at the root of the project). The steps
outlined in "Setting up your Service" must be completed before you can use CDK
on this stack.
*/

const app = new App({
  owner: 'lexi.jeong@zocdoc.com',
  project: 'faq-chatbot',
});

const dynamodbTableBasename = 'RestaurantsReservations';
const dynamodbTablePrefix = app.stackPrefix;

const dbStack = Stack.with<{ table: dynamodb.Table }>(app, `${app.stackPrefix}-dynamo`, function () {
  this.table = dynamo.makeTable(this, 'db-table', {
    // TODO: if you need a different primary partitionKey replace
    // this with the name and type you need, if not, remove this comment.
    partitionKey: { name: 'PK', type: dynamodb.AttributeType.STRING },
    sortKey: { name: 'SK', type: dynamodb.AttributeType.STRING },
    billingMode: dynamodb.BillingMode.PAY_PER_REQUEST,
    tableName: `${dynamodbTablePrefix}-${dynamodbTableBasename}`,
    stream: StreamViewType.NEW_IMAGE
  });

  this.table.addGlobalSecondaryIndex({
    indexName: 'ReservationIdIndex',
    partitionKey: { name: 'SK', type: dynamodb.AttributeType.STRING },
    projectionType: dynamodb.ProjectionType.ALL,
  });
});

new Stack(app, `${app.stackPrefix}-lambda`, function () {
  const topic = sns.makeSnsTopic(this, `${app.stackPrefix}-sns`, {
    topicName: 'faq-chatbot-alerts'
  });

  const func = lambda.makeDotnetLambda(
    this,
    `${app.stackPrefix}-lambda-function`,
    {
      functionName: 'faq-chatbot-lambda',
      runtime: Runtime.DOTNET_6,
      handler: 'FaqChatbot.Lambda::FaqChatbot.Lambda.EntryPoint::DdbEventStreamHandler',
      code: lambda.codeFromS3(
        this,
        `${app.stackPrefix}-lambda`,
        'arn:aws:s3:::zocdoc-deployment-artifacts',
        `faq-chatbot/FaqChatbot.Lambda.${new gitTag.CurrentGitTag().toString()}.zip`
      ),
      sentryReleaseName: new gitTag.CurrentGitTag().toString(),
      sentryProjectName: 'faq-chatbot',
      environment: {
        SETTINGS__TOPIC_ARN: topic.topicArn,
        DOTNET_ENVIRONMENT: selectWithCurrentAccount(
          {
            ci001: 'Staging',
            pr201: 'Production',
          }
        ),
      }
    });

  func.addEventSource(new DynamoEventSource(dbStack.table, {
    batchSize: 5,
    retryAttempts: 3,
    startingPosition: StartingPosition.LATEST,
    onFailure: new SqsDlq(func.deadLetterQueue!),
    bisectBatchOnError: true,
  }));

  topic.grantPublish(func);
});

new Stack(app, `${app.stackPrefix}-service-stack`, function () {
  const lbHttpSvc = new ecs.LoadBalancedHttpService(this, 'http-service', {
    serviceName: 'faq-chatbot',
    serviceVersion: new gitTag.CurrentGitTag().toString(),
    sentryProjectName: 'faq-chatbot',
    environment: {
      DynamoDBTableNamePrefix: `${dynamodbTablePrefix}-`,
      DOTNET_ENVIRONMENT: selectWithCurrentAccount(
        {
          ci001: 'Staging',
          pr201: 'Production',
        }
      ),
    },

    // Set this value to integrate with Pagerduty, consider only setting this
    // value when deploying in the production environment, you might not want to
    // be alerted in dev or ci.
    //
    // alarmWebhook: "<PAGER_DUTY_URL>"
  });

  dbStack.table.grantReadWriteData(lbHttpSvc.service);
});

new Stack(app, `${app.stackPrefix}-cron`, function name() {
  new ecs.CronService(
    this,
    'cron-task',
    {
      serviceVersion: new gitTag.CurrentGitTag({
        // The transform is needed because this image is differentiated from the
        // core service using a tag prefix. See docker-compose.yml
        transform: (s) => `cron-${s}`
      }).toString(),
      schedule: aws_events.Schedule.cron({
        minute: "0",
      }),
      serviceName: 'faq-chatbot-cron',
      ecrRepositoryName: 'faq-chatbot',
      environment: {
        RELEASE: new gitTag.CurrentGitTag().toString(),
        DOTNET_ENVIRONMENT: selectWithCurrentAccount(
          {
            ci001: 'Staging',
            defaults: 'Production',
          }
        ),
      }
    }
  )
})

new Stack(app, `${app.stackPrefix}-worker`, function () {
  new ecs.WorkerService(
    this,
    'daemon-worker',
    {
      serviceVersion: new gitTag.CurrentGitTag({ transform: (s) => `worker-${s}` }).toString(),
      serviceName: 'faq-chatbot-daemon-worker',
      ecrRepositoryName: 'faq-chatbot',
      environment: {
        DOTNET_ENVIRONMENT: selectWithCurrentAccount(
          {
            ci001: 'Staging',
            defaults: 'Production',
          }
        ),
      },
    }
  );
});
