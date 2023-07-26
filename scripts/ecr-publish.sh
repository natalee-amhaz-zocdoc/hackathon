#!/bin/bash
set -o errexit
set -o nounset
set -x trace

TAG=${TAG:-local}
echo "Publishing tag ($TAG)"

docker buildx rm builder --keep-state || true
docker buildx create --name builder --driver docker-container --use --bootstrap

docker buildx bake web fake cron worker --push

docker buildx rm builder --keep-state

if [ "${TAG}" != "local" ] ; then
    git tag "$TAG"
    git push origin "$TAG"
fi
