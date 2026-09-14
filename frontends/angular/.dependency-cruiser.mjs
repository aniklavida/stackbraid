/**
 * Enforces the same Clean Architecture rule the backends carry, adapted to
 * a frontend with no dependency-injection container of its own beyond
 * Angular's: `domain/` depends on nothing, `data/` may depend on `domain/`
 * and the generated client, `application/` orchestrates `domain/` and
 * `data/`, and `presentation/` only ever reaches `application/` and
 * `domain/` — never `data/` directly, so a screen can never bypass a use
 * case and call the API on its own.
 *
 * `shared/` carries no business meaning and must never import a feature.
 * A feature's `domain/`, `application/`, `data/` and `presentation/`
 * folders are internal; only that feature's own `index.ts` may be imported
 * from outside it — the same "only through Contracts" rule the backends
 * enforce between features, restated for a frontend where a feature's
 * barrel file is its `Contracts`.
 *
 * Every rule below was seen to genuinely fail against a deliberate
 * violation before this file reached its current, passing shape.
 */
const FEATURES = [
  { app: "web", name: "auth" },
  { app: "web", name: "home" },
  { app: "web", name: "profile" },
  { app: "admin", name: "users" },
  { app: "admin", name: "roles" },
];

const crossFeatureRules = FEATURES.map(({ app, name }) => ({
  name: `feature-${app}-${name}-internal-only-via-barrel`,
  comment: `Only src/${app}/features/${name}/index.ts may be imported from outside that feature — everything else under it is internal.`,
  severity: "error",
  from: {
    pathNot: `^src/${app}/features/${name}/`,
  },
  to: {
    path: `^src/${app}/features/${name}/(?!index\\.ts$).+`,
  },
}));

const domainPurityRules = FEATURES.map(({ app, name }) => ({
  name: `feature-${app}-${name}-domain-depends-on-nothing`,
  comment: "domain/ models the feature's own rules and types — it must not depend on application/, data/, presentation/, shared/, or the generated client.",
  severity: "error",
  from: {
    path: `^src/${app}/features/${name}/domain/`,
  },
  to: {
    path: `^src/${app}/features/${name}/(application|data|presentation)/|^src/shared/|^@stackbraid/client-typescript`,
  },
}));

const presentationCannotReachDataRules = FEATURES.map(({ app, name }) => ({
  name: `feature-${app}-${name}-presentation-cannot-reach-data`,
  comment: "presentation/ calls application/'s use cases, never data/'s repository directly — that would let a screen bypass validation and orchestration.",
  severity: "error",
  from: {
    path: `^src/${app}/features/${name}/presentation/`,
  },
  to: {
    path: `^src/${app}/features/${name}/data/`,
  },
}));

const applicationCannotReachPresentationRules = FEATURES.map(({ app, name }) => ({
  name: `feature-${app}-${name}-application-cannot-reach-presentation`,
  comment: "application/ orchestrates domain/ and data/; it must not depend on presentation/ — a use case is not allowed to know about a component.",
  severity: "error",
  from: {
    path: `^src/${app}/features/${name}/application/`,
  },
  to: {
    path: `^src/${app}/features/${name}/presentation/`,
  },
}));

const dependencyCruiserConfig = {
  forbidden: [
    {
      name: "shared-never-imports-a-feature",
      comment: "shared/ carries no business meaning — the moment it imports a feature, it stops being shared.",
      severity: "error",
      from: { path: "^src/shared/" },
      to: { path: "^src/(web|admin)/features/" },
    },
    ...crossFeatureRules,
    ...domainPurityRules,
    ...presentationCannotReachDataRules,
    ...applicationCannotReachPresentationRules,
    {
      name: "no-circular",
      comment: "A dependency cycle between modules is a design smell in any of these layers.",
      severity: "error",
      from: {},
      to: { circular: true },
    },
    {
      name: "no-orphans",
      comment: "A file nothing imports and that imports nothing itself is almost always dead — Angular's own bootstrap entry points and generated/config files are excluded below.",
      severity: "warn",
      from: {
        orphan: true,
        pathNot: ["\\.d\\.ts$", "^src/main\\.ts$", "^src/app/app\\.(config|routes)\\.ts$"],
      },
      to: {},
    },
  ],
  options: {
    tsPreCompilationDeps: true,
    tsConfig: {
      fileName: "tsconfig.json",
    },
    // node_modules (this includes the generated TypeScript client, resolved
    // through the tsconfig path alias to a sibling directory rather than a
    // real node_modules folder) is a dependency leaf, not something whose
    // own internals this ruleset has any opinion about.
    doNotFollow: {
      path: "node_modules|clients/typescript",
    },
  },
};

export default dependencyCruiserConfig;
