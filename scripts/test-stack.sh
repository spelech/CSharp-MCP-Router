#!/usr/bin/env bash
# ==============================================================================
# Integration Test Stack Lifecycle Helper (Model Context Gateway (MCG))
# ==============================================================================
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

COMPOSE_FILE="docker-compose.test.yml"

usage() {
    echo "Usage: $0 [command] [options]"
    echo ""
    echo "Commands:"
    echo "  up          Start the core integration test stack (Vault, OpenLDAP, MySQL, Mock MCP)"
    echo "  up-full     Start full integration stack including MSSQL Server 2022"
    echo "  down        Stop all test stack containers"
    echo "  status      Check health and status of all test stack services"
    echo "  logs        Follow logs for all test stack containers"
    echo "  seed        Re-run Vault and database seeding"
    echo "  clean       Stop and remove containers, networks, and test volumes"
    echo "  help        Show this help message"
    echo ""
}

CMD="${1:-up}"

case "${CMD}" in
    up)
        echo "🚀 Starting core integration test stack (Vault, OpenLDAP, MySQL, Mock MCP)..."
        docker compose -f "${COMPOSE_FILE}" up -d --wait vault-test ldap-test mysql-test mock-mcp-server
        echo "✅ Core test stack is healthy and ready."
        ;;
    up-full)
        echo "🚀 Starting FULL integration stack (including MSSQL)..."
        docker compose -f "${COMPOSE_FILE}" --profile mssql up -d --wait
        echo "✅ Full test stack is healthy and ready."
        ;;
    down)
        echo "🛑 Stopping integration test stack..."
        docker compose -f "${COMPOSE_FILE}" --profile mssql down
        echo "✅ Test stack stopped."
        ;;
    status)
        docker compose -f "${COMPOSE_FILE}" --profile mssql ps
        ;;
    logs)
        docker compose -f "${COMPOSE_FILE}" --profile mssql logs -f "${@:2}"
        ;;
    seed)
        echo "🌱 Re-running Vault & seed initialization..."
        docker compose -f "${COMPOSE_FILE}" up vault-init --force-recreate
        echo "✅ Seeding completed."
        ;;
    clean)
        echo "🧹 Cleaning up test stack containers and volumes..."
        docker compose -f "${COMPOSE_FILE}" --profile mssql down -v --remove-orphans
        echo "✅ Test environment cleaned."
        ;;
    help|-h|--help)
        usage
        ;;
    *)
        echo "Unknown command: ${CMD}"
        usage
        exit 1
        ;;
esac
