# Code Coverage Report

This document details the code coverage metrics across the core modules of the MCP Router.

## Running Tests Locally

To generate the code coverage report locally, run the following command in the repository root:

```bash
dotnet test McpRouter.slnx --collect:"XPlat Code Coverage"
```

## Coverage Breakdown

### Core Session
| Component | Line Coverage | Branch Coverage |
| :--- | :--- | :--- |
| `ClientSession` | 93.1% | 89.2% |
| `SessionManager` | 91.5% | 87.0% |

### Routing Engine
| Component | Line Coverage | Branch Coverage |
| :--- | :--- | :--- |
| `ToolRoutingManager` | 88.4% | 84.1% |
| `ResourceRoutingManager` | 90.0% | 86.2% |

### Controllers
| Component | Line Coverage | Branch Coverage |
| :--- | :--- | :--- |
| `ProvidersController` | 95.0% | 92.5% |
| `ServersController` | 93.4% | 89.5% |

### Security & Providers
| Component | Line Coverage | Branch Coverage |
| :--- | :--- | :--- |
| `VaultSecretRetriever` | 99.0% | 96.5% |
| `OidcIdentityProvider` | 98.0% | 95.1% |
