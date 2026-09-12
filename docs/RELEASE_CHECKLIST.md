# v1.0 release checklist

## Truth

- [ ] Every public claim has working evidence or is clearly labelled planned.
- [ ] The specification, the documentation and the implementation agree.

## Product

- [ ] `create` produces a working project for every combination, containing only the chosen pieces.
- [ ] `docker compose up` brings up the full system with no manual steps.
- [ ] Register, log in and manage users and roles work in web, admin and mobile against both backends.
- [ ] A queued export produces a file and its duration appears on a dashboard.
- [ ] A realtime notification reaches every surface from both backends.
- [ ] Every string is localized in at least two locales, backend errors included.

## Engineering

- [ ] Conformance passes for every backend on every shipped provider, in CI.
- [ ] Generated clients are byte-identical to a fresh regeneration.
- [ ] Architecture tests pass in both backends.
- [ ] All five test suites run green from a clean checkout.
- [ ] Every route and generated client carries `/v1/`.

## Repository

- [ ] Every shipped dependency is listed with its licence and passes the audit.
- [ ] README, specification, architecture, structure and troubleshooting are complete.
- [ ] Security policy, code of conduct and contributor instructions are complete.
- [ ] Repository description, topics and homepage are set.
- [ ] CI and release workflows pass.
- [ ] Working tree is clean and local HEAD matches the remote.

## Launch

- [ ] A 90-second demo proves pick, run, and add a feature.
- [ ] Release notes and changelog are accurate.
- [ ] The tag is created only after every box above is ticked.
