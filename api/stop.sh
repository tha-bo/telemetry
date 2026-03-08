#!/usr/bin/env bash
# Stops the Telemetry API if it's running on port 5274
PORT=5274
if lsof -ti :$PORT >/dev/null 2>&1; then
  lsof -ti :$PORT | xargs kill -9
  echo "Stopped process on port $PORT"
else
  echo "Nothing running on port $PORT"
fi
