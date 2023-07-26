SHELL := /bin/bash

DOTNET_ENVIRONMENT ?= "Development"
DOTNET_ARGS := --nologo

# conditionally set SETUP_LAMBDA=true if goals include integration-test
SETUP_LAMBDA ?= $(if $(findstring integration-test,$(MAKECMDGOALS)),true,false)

# conditionally set urls based on if tests are running in a container
IS_RUNNING_IN_CONTAINER := $(findstring true,$(DOTNET_RUNNING_IN_CONTAINER))
AUTH_HOST ?= $(if $(IS_RUNNING_IN_CONTAINER),http://auth_api:7001,http://localhost:5004)
WEB_HOST ?= $(if $(IS_RUNNING_IN_CONTAINER),https://web:443,https://localhost:5001)
FAKE_WEB_HOST ?= $(if $(IS_RUNNING_IN_CONTAINER),https://fake:443,https://localhost:5003)
ENDPOINT_URL ?= $(if $(IS_RUNNING_IN_CONTAINER),http://localstack:4566,http://localhost:4566)

.DEFAULT_GOAL := build

export AWS_PAGER := ""

.PHONY: build-docker setup docker-logs teardown \
	build install setup-localstack \
	run run-docker watch-run run-fake run-worker \
	clean \
	test unit-test api-test ci-api-test \
	publish generate package-lambda

build-docker: publish
	docker compose build

setup: build-docker
	docker compose --ansi never up --force-recreate --always-recreate-deps --wait -d web fake worker zipkin

docker-logs:
	docker compose logs -f

teardown:
	docker compose down $(DOCKER_COMPOSE_TEARDOWN_FLAGS)

build:
	dotnet build $(DOTNET_ARGS)

setup-localstack:
	ENDPOINT_URL=$(ENDPOINT_URL) \
	SETUP_LAMBDA=$(SETUP_LAMBDA) \
	./scripts/setup-localstack.sh

run: setup-localstack
	cd ./src/FaqChatbot.Web/ && \
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	dotnet run $(DOTNET_ARGS)

run-cron:
	cd ./src/FaqChatbot.Cron/ && \
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	dotnet run $(DOTNET_ARGS)

run-docker: publish
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	docker compose up web

watch-run:
	cd ./src/FaqChatbot.Web/ && \
    DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
    dotnet watch run $(DOTNET_ARGS)

run-fake:
	cd ./src/FaqChatbot.Fake/ && \
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	dotnet run $(DOTNET_ARGS)

run-worker:
	cd ./src/FaqChatbot.Worker/ && \
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	dotnet run $(DOTNET_ARGS)

clean:
	dotnet clean $(DOTNET_ARGS)
	rm -rf ./publish

test: unit-test

unit-test:
	DOTNET_ENVIRONMENT="$(DOTNET_ENVIRONMENT)" \
	dotnet test ./tests/UnitTests/UnitTests.csproj $(DOTNET_ARGS) $(TEST_ARGS)

api-test: setup-localstack
	AUTH_HOST=$(AUTH_HOST) \
	WEB_HOST=$(WEB_HOST) \
	ENDPOINT_URL=$(ENDPOINT_URL) \
	dotnet test ./tests/ApiTests/ApiTests.csproj $(DOTNET_ARGS) $(TEST_ARGS)

fake-api-test: setup-localstack
	AUTH_HOST=$(AUTH_HOST) \
	WEB_HOST=$(FAKE_WEB_HOST) \
	ENDPOINT_URL=$(ENDPOINT_URL) \
	dotnet test ./tests/ApiTests/ApiTests.csproj $(DOTNET_ARGS) $(TEST_ARGS)

integration-test: package-lambda setup-localstack
	AUTH_HOST=$(AUTH_HOST) \
	WEB_HOST=$(WEB_HOST) \
	ENDPOINT_URL=$(ENDPOINT_URL) \
	dotnet test ./tests/IntegrationTests/IntegrationTests.csproj $(DOTNET_ARGS) $(TEST_ARGS)

publish:
	dotnet publish ./src/FaqChatbot.Web/FaqChatbot.Web.csproj -c Release -o ./publish/Web $(DOTNET_ARGS)
	dotnet publish ./src/FaqChatbot.Fake/FaqChatbot.Fake.csproj -c Release -o ./publish/Fake $(DOTNET_ARGS)
	dotnet publish ./src/FaqChatbot.Cron/FaqChatbot.Cron.csproj -c Release -o ./publish/Cron $(DOTNET_ARGS)
	dotnet publish ./src/FaqChatbot.Worker/FaqChatbot.Worker.csproj -c Release -o ./publish/Worker $(DOTNET_ARGS)

# Generate service code without fully building the application.
# This is useful if you're making changes and just want the updated
# generated files
generate:
	dotnet msbuild ./src/FaqChatbot.Web/FaqChatbot.Web.csproj /t:GenerateServiceCode /nologo

# lambda
package-lambda:
	./scripts/confirm-dotnet-lambda-tool.sh
	dotnet restore ./src/FaqChatbot.Lambda
	cd ./src/FaqChatbot.Lambda && dotnet lambda package --function-architecture `uname -m`

publish-lambda: package-lambda
	aws s3 cp src/FaqChatbot.Lambda/bin/Release/net6.0/FaqChatbot.Lambda.zip \
	s3://zocdoc-deployment-artifacts/faq-chatbot/FaqChatbot.Lambda.$(TAG).zip

invoke-lambda:
	aws --endpoint-url="${ENDPOINT_URL}" \
		lambda invoke --function-name reservation-lambda \
		--cli-binary-format raw-in-base64-out \
		--payload '{"Records":[]}' \
		/dev/stdout

# outputs localstack data (such as s3 objects & ddb table contents) to the ./localstack_test_artifacts folder
copy-localstack-artifacts:
	./scripts/copy-localstack-artifacts.sh
