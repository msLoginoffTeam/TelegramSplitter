#!/bin/bash

set -e

docker-compose stop telegram-splitter-app || true
docker-compose rm -f telegram-splitter-app || true

docker-compose pull telegram-splitter-app
docker-compose up -d telegram-splitter-app

