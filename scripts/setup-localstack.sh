#!/bin/bash
set -x trace

ENDPOINT_URL="${ENDPOINT_URL:-http://localhost:4566}"
SETUP_LAMBDA="${SETUP_LAMBDA:-false}"

echo "Creating dynamodb table"
ddb_table_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
  --region=us-east-1 \
  --endpoint-url="$ENDPOINT_URL" \
  dynamodb \
  create-table \
  --cli-input-json \
  file://scripts/tables/reservation-table-dev.json 2>&1)

if [ $? -ne 0 ] && [[ ! (($ddb_table_output =~ "Cannot create preexisting table")) ]]; then
  echo 'dynamo: non success exit code, and output did not match allowed error'
  echo "$ddb_table_output"
  exit 1
fi

if [ "$SETUP_LAMBDA" = true ]; then
  echo "Creating lambda function"
  lambda_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --region=us-east-1 \
    --endpoint-url="$ENDPOINT_URL" \
    lambda \
    create-function \
    --function-name reservation-lambda \
    --runtime dotnet6 \
    --zip-file fileb://src/FaqChatbot.Lambda/bin/Release/net6.0/FaqChatbot.Lambda.zip \
    --handler FaqChatbot.Lambda::FaqChatbot.Lambda.EntryPoint::DdbEventStreamHandler \
    --environment "Variables={DOTNET_ENVIRONMENT=IntegrationTests}" \
    --role local-role 2>&1)

  if [ $? -ne 0 ]; then
      if [[ (($lambda_output =~ "Function already exist")) ]]; then
          echo "Updating lambda function code"
          lambda_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
              --region=us-east-1 \
              --endpoint-url="$ENDPOINT_URL" \
              lambda \
              update-function-code \
              --function-name reservation-lambda \
              --zip-file fileb://src/FaqChatbot.Lambda/bin/Release/net6.0/FaqChatbot.Lambda.zip 2>&1)
      else
          echo 'lambda: non success exit code, and output did not match allowed error'
          echo "$lambda_output"
          exit 1
      fi
  fi

  echo "Creating sns topic"
  sns_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --region=us-east-1 \
    --endpoint-url="$ENDPOINT_URL" \
    sns \
    create-topic \
    --name reservation-alerts 2>&1)

  if [ $? -ne 0 ]; then
      echo 'sns: non success exit code'
      echo "$sns_output"
      exit 1
  fi

  echo "Creating dynamo event source mapping"
  ddb_stream_arn=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --region=us-east-1 \
    --endpoint-url="$ENDPOINT_URL" \
    dynamodbstreams \
    list-streams \
    --table-name dev-RestaurantsReservations \
    --query "Streams[0].StreamArn" \
    --output text 2>&1)

  ddb_stream_mapping_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --endpoint-url="$ENDPOINT_URL" \
    --region=us-east-1 \
    lambda \
    create-event-source-mapping \
    --function-name reservation-lambda \
    --event-source $ddb_stream_arn \
    --batch-size 1 \
    --bisect-batch-on-function-error \
    --starting-position TRIM_HORIZON 2>&1)

  if [ $? -ne 0 ]; then
      echo 'dynamodb streams: non success exit code'
      echo $ddb_stream_arn
      echo $ddb_stream_mapping_output
      exit 1
  fi

  echo "Creating sqs queue"
  sqs_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --endpoint-url="$ENDPOINT_URL" \
    --region=us-east-1 \
    sqs \
    create-queue \
    --queue-name reservation-alerts-received 2>&1)

  if [ $? -ne 0 ]; then
      echo 'sqs: non success exit code'
      echo $sqs_output
      exit 1
  fi

  echo "Subscribing sqs to sns"
  sqs_subscription_output=$(AWS_ACCESS_KEY_ID=localstack AWS_SECRET_ACCESS_KEY=localstack aws \
    --endpoint-url="$ENDPOINT_URL" \
    --region=us-east-1 \
    sns \
    subscribe \
    --topic-arn arn:aws:sns:us-east-1:000000000000:reservation-alerts \
    --protocol sqs \
    --notification-endpoint $ENDPOINT_URL/000000000000/reservation-alerts-received 2>&1)

  if [ $? -ne 0 ]; then
      echo 'sqs subscription: non success exit code'
      echo $sqs_subscription_output
      exit 1
  fi
fi

echo "Successfully setup localstack"
