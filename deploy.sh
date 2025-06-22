#!/bin/bash

docker pull brawleryura1/telegram-splitter-app:latest
docker stop telegram-splitter-app || true
docker rm telegram-splitter-app || true
docker run -d --name telegram-splitter-app brawleryura1/telegram-splitter-app:latest
