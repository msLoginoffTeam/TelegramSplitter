#!/bin/bash

docker pull brawleryura1/telegram-splitter-app:latest
docker stop telegram-splitter-app || true
docker rm telegram-splitter-app || true
APP_VERSION=latest docker compose up telegram-splitter-app -d
