@echo off

pushd ..\docker
docker-compose -f docker-compose.tests.yml build
docker-compose -f docker-compose.tests.yml up
popd
