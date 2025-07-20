#!/bin/bash

set -e

docker network inspect my-network >/dev/null 2>&1 || \
  docker network create my-network

docker-compose stop telegram-splitter-app || true
docker-compose rm -f telegram-splitter-app || true

docker-compose pull telegram-splitter-app
docker-compose up -d telegram-splitter-app

