# CLAUDE.md




## Pommel - Semantic Code Search

This sub-project (psecsapi-console) uses Pommel (v0.5.2) for semantic code search with hybrid vector + keyword matching.

**Supported languages:** C#, Dart, Elixir, Go, Java, JavaScript, Kotlin, PHP, Python, Rust, Solidity, Swift, TypeScript

### Code Search Decision Tree

**Use `pm search` FIRST for:**
- Finding specific implementations ("where is X implemented")
- Quick code lookups when you know what you're looking for
- Iterative exploration (multiple related searches)
- Cost/time-sensitive tasks (~18x fewer tokens, 1000x+ faster)

**Fall back to Explorer/Grep when:**
- Verifying something does NOT exist (Pommel may return false positives)
- Understanding architecture or code flow relationships
- Need full context around matches (not just snippets)
- Searching for exact string literals (specific error messages, identifiers)

**Decision rule:** Start with `pm search`. If results seem off-topic or you need to confirm absence, use Explorer.

### When to Use Which Tool

| Use Case                         | Recommended Tool          |
|----------------------------------|---------------------------|
| Quick code lookup                | Pommel                    |
| Understanding architecture       | Explorer                  |
| Finding specific implementations | Pommel                    |
| Verifying if feature exists      | Explorer                  |
| Iterative exploration            | Pommel                    |
| Cost-sensitive workflows         | Pommel (18x fewer tokens) |
| Time-sensitive tasks             | Pommel (1000x+ faster)    |

### Quick Search Examples
```bash
# Search within this sub-project (default when running from here)
pm search "authentication logic"

# Search with JSON output
pm search "error handling" --json

# Search across entire monorepo
pm search "shared utilities" --all

# Show detailed match reasons
pm search "rate limiting" --verbose
```

### Available Commands
- `pm search <query>` - Hybrid search (~18x fewer tokens than grep)
- `pm status` - Check daemon status and index statistics
- `pm subprojects` - List all sub-projects
- `pm start` / `pm stop` - Control the background daemon

### Tips
- **Low scores (< 0.5) suggest weak matches** - consider using Explorer to confirm
- Searches default to this sub-project when you're in this directory
- Use `--all` to search across the entire monorepo
- Use `--verbose` to see why results matched
