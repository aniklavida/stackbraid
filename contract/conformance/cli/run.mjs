#!/usr/bin/env node
// Entry point for the StackBraid Identity conformance suite.
//
// Usage:
//   node cli/run.mjs <baseUrl>
//
// Takes exactly one argument — the base URL of a running backend — and
// nothing else. The suite has no knowledge of which backend, language or
// database provider is behind that URL; it only knows contract/openapi.yaml.

import { runConformanceSuite } from '../src/suite.mjs';

const baseUrl = process.argv[2];

if (!baseUrl || process.argv.length > 3) {
  console.error('Usage: node cli/run.mjs <baseUrl>');
  console.error('Example: node cli/run.mjs http://localhost:8080');
  process.exit(1);
}

const { failureCount } = await runConformanceSuite(baseUrl);
process.exit(failureCount > 0 ? 1 : 0);
