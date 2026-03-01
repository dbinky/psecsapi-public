# CLI Setup Guide (papi)

`papi` is the official PSECS command-line interface. It provides direct access to all game functionality from your terminal, enabling automation, scripting, and bot-driven gameplay.

## Download

Download the latest release for your platform:

| Platform | Download |
|----------|----------|
| macOS (Apple Silicon) | [papi-osx-arm64](https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-osx-arm64) |
| macOS (Intel) | [papi-osx-x64](https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-osx-x64) |
| Linux (x64) | [papi-linux-x64](https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-linux-x64) |
| Windows (x64) | [papi-win-x64.exe](https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-win-x64.exe) |
| Windows (ARM64) | [papi-win-arm64.exe](https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-win-arm64.exe) |

**Important:** Use `curl -L` (follow redirects) when downloading via command line. GitHub release URLs redirect, and without `-L` you'll get an HTML page instead of the binary.

## Install (macOS/Linux)

```bash
# Download (example for macOS Apple Silicon)
curl -L -o papi https://github.com/dbinky/psecsapi-public/releases/latest/download/papi-osx-arm64

# Make executable
chmod +x papi

# Move to PATH
sudo mv papi /usr/local/bin/
```

## Install (Windows)

1. Download the `.exe` file for your architecture
2. Move it to a folder in your PATH (e.g., `C:\Users\YourName\bin\`)
3. Optionally rename to `papi.exe`

## First-Time Setup

```bash
# Authenticate with your PSECS account
papi auth login

# Verify connection
papi status

# See all available commands
papi --help
```

## Common Commands

```bash
papi corp info              # View your corporation details
papi fleet list             # List your fleets
papi fleet scan             # Scan current sector
papi ship list              # List ships in a fleet
papi tokens balance         # Check token balance
papi tokens buy --quantity 5  # Purchase tokens
papi research status        # View research progress
```

## Building from Source

The CLI source code is available at [github.com/dbinky/psecsapi-public](https://github.com/dbinky/psecsapi-public). Build with `dotnet publish` targeting your platform's RID.
