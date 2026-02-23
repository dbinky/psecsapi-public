# PSECSAPI Command Line Interface

A .NET 9 command-line interface for interacting with the PSECSAPI space commerce game backend.

## Quick Start

```bash
# Build the solution
dotnet build psecsapi.sln

# Start the servers (opens new terminal windows, waits for ready)
cd psecsapi.Console/utils
pip3 install -r requirements.txt
python3 start_servers.py

# Login with your Solana wallet
python3 full_login.py

# Run CLI commands (from repository root)
dotnet run --project psecsapi.Console -- user get

# Or create an alias for convenience
alias papi='dotnet run --project psecsapi.Console --'
papi user get
```

## Authentication

The CLI uses Solana wallet-based authentication with Ed25519 signatures.

### Manual Login Flow

```bash
# Step 1: Get a challenge message
papi login --wallet <your-wallet-address>

# Step 2: Sign the message with your Solana wallet
# (Use Phantom, Solflare, or solana-keygen sign)

# Step 3: Complete authentication
papi auth --wallet <wallet> --nonce "<nonce>" --signature "<signature>"

# Step 4: Verify you're logged in
papi user get
```

### Automated Login (Python Utility)

For development and testing, use the full login utility script:

```bash
cd psecsapi.Console/utils

# Install dependencies (use pip3 on macOS)
pip3 install -r requirements.txt

# Login with your Solana keypair
python3 full_login.py --keypair ~/.config/solana/id.json

# Or generate a test wallet and login
python3 full_login.py --generate

# Use a different API URL
python3 full_login.py --keypair ~/wallet.json --api-url http://localhost:5000
```

## Commands

### Authentication

| Command | Description |
|---------|-------------|
| `login -w <wallet>` | Get authentication challenge |
| `auth -w <wallet> -n <nonce> -s <signature>` | Complete authentication |
| `logout` | Logout and clear stored credentials |

### User

| Command | Description |
|---------|-------------|
| `user get` | Get your user profile |
| `user create-corp -n <name>` | Create a new corporation |
| `user map` | Get your user map data |

### Corporation

| Command | Description |
|---------|-------------|
| `corp get [-i <id>]` | Get corporation details |
| `corp fleets [-i <id>]` | List corporation fleets |

### Fleet

| Command | Description |
|---------|-------------|
| `fleet get <fleet-id>` | Get fleet details |
| `fleet scan <sector-id>` | Scan for fleets in a sector |
| `fleet enqueue <fleet-id> -c <conduit-id>` | Queue fleet at a conduit |
| `fleet dequeue <fleet-id>` | Remove fleet from queue |

### Ship

| Command | Description |
|---------|-------------|
| `ship get <ship-id>` | Get ship details |

### Map

| Command | Description |
|---------|-------------|
| `map stats` | Get map statistics |
| `map create [-c <count>]` | Create new sectors |

### Configuration

| Command | Description |
|---------|-------------|
| `config show` | Show current configuration |
| `config set-corp <corp-id>` | Set default corporation |
| `config get-corp` | Get default corporation |

## Configuration

The CLI stores configuration in `~/.psecsapi/config.json`:

```json
{
  "system": {
    "baseUrl": "http://localhost:5130"
  },
  "user": {
    "accessToken": "...",
    "refreshToken": "...",
    "walletAddress": "...",
    "defaultCorpId": null
  }
}
```

### Token Refresh

The CLI automatically refreshes expired access tokens using the stored refresh token. If the refresh token is also expired, you'll be prompted to log in again.

## Project Structure

```
psecsapi.Console/
├── Program.cs                    # Main entry point, command definitions
├── Infrastructure/
│   ├── Configuration/
│   │   ├── CliConfig.cs          # Configuration model
│   │   └── ConfigRepository.cs   # Config file I/O
│   ├── Http/
│   │   └── AuthenticatedHttpClient.cs  # HTTP client with auto-refresh
│   └── Output/
│       └── OutputHelper.cs       # Output formatting utilities
├── utils/
│   ├── full_login.py            # Python full login utility
│   ├── start_servers.py         # Server startup utility
│   └── requirements.txt         # Python dependencies
└── README.md
```

### Key Components

**Program.cs** - Defines all CLI commands using `System.CommandLine`. Commands are organized by domain (user, corp, fleet, ship, map, config) with a thin wrapper over API calls.

**AuthenticatedHttpClient** - Wraps `HttpClient` with automatic token refresh. When a request returns 401 Unauthorized, it attempts to refresh the access token using the refresh token before retrying.

**ConfigRepository** - Handles reading/writing the JSON config file in the user's home directory.

## Development

### Building

```bash
# Build just the CLI
dotnet build psecsapi.Console

# Build entire solution
dotnet build psecsapi.sln
```

### Running Locally

The CLI connects to the API at the URL configured in `~/.psecsapi/config.json`. For local development:

**Option 1: Use the startup script (recommended)**
```bash
cd psecsapi.Console/utils
python3 start_servers.py    # Opens terminal windows, waits for ready
python3 full_login.py
```

**Option 2: Manual startup**
1. Start the Silo: `dotnet run --project psecsapi.Silo`
2. Start the API: `dotnet run --project psecsapi.api`
3. Run CLI commands: `dotnet run --project psecsapi.Console -- <command>`

### Adding New Commands

1. Create a new `Create<Domain>Command()` method in `Program.cs`
2. Add subcommands using `System.CommandLine`
3. Use `AuthenticatedHttpClient` for authenticated API calls
4. Register the command in `Main()` with `rootCommand.AddCommand()`

## Utils Scripts

### start_servers.py

Launches the Silo and API servers in separate terminal windows and waits for them to be ready.

**Features:**
- Opens new macOS Terminal windows for each server
- Displays a spinner while waiting for servers to start
- Auto-detects repository root
- Checks if servers are already running

**Usage:**
```bash
# Start servers with default settings
python3 start_servers.py

# Custom timeout (default: 60 seconds)
python3 start_servers.py --timeout 120

# Specify repository path manually
python3 start_servers.py --repo-path ~/code/psecsapi
```

### full_login.py

A Python utility that automates the complete wallet authentication flow:

1. Loads your Solana keypair (or generates a test one)
2. Requests a challenge from the API
3. Signs the challenge with Ed25519
4. Submits the signature to authenticate
5. Fetches your user profile to verify success
6. Saves tokens to the CLI config file

**Requirements:**
```bash
pip3 install -r requirements.txt
```

**Usage:**
```bash
# Using your Solana CLI keypair
python3 full_login.py

# Using a specific keypair
python3 full_login.py --keypair ~/my-wallet.json

# Generate a test wallet
python3 full_login.py --generate

# Custom API URL
python3 full_login.py --api-url http://localhost:5000

# Skip SSL verification (local dev)
python3 full_login.py --no-verify-ssl
```

## Troubleshooting

### "Session expired. Please login again"

Your refresh token has expired. Run the login flow again:
```bash
papi login --wallet <your-wallet>
```

### SSL Certificate Errors

For local development with self-signed certificates, either:
- Trust the development certificate: `dotnet dev-certs https --trust`
- Use the Python utility with `--no-verify-ssl`

### "No corp ID specified and no default corp ID set"

Set a default corporation:
```bash
papi config set-corp <your-corp-id>
```

Or specify the corp ID with each command:
```bash
papi corp get -i <corp-id>
```
