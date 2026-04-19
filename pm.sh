#!/bin/bash
# CLI wrapper for life-orchestration
export PM_API_URL="${PM_API_URL:-http://192.168.1.194:3080}"
dotnet ~/projects/life-orchestration/publish/cli/LifeOrchestration.Cli.dll "$@"
