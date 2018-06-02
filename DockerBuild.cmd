@ECHO OFF

REM no version on the tag so it will build marbletest.net:latest
docker build --rm -f Dockerfile -t marbletest.net .