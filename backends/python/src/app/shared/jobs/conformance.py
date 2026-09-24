"""Deterministic handlers the contract conformance suite drives to prove the
retry and dead-letter paths — the Python twins of the .NET backend's
``ConformanceJobHandlers``. Registered only when the conformance flag is on.
Each is stateless: behaviour is a pure function of the attempt number, so the
outcome cannot depend on state a restart would have dropped.
"""

from __future__ import annotations

from app.shared.jobs.handlers import JobContext, JobPayload


class ConformanceSucceedsJobHandler:
    job_type = "conformance.succeeds"

    async def handle(self, context: JobContext, payload: JobPayload) -> None:
        return None


class ConformanceRetriesJobHandler:
    job_type = "conformance.retries"
    succeed_on_attempt = 3

    async def handle(self, context: JobContext, payload: JobPayload) -> None:
        if context.attempt < self.succeed_on_attempt:
            raise RuntimeError(
                f"conformance.retries fails on attempt {context.attempt} and succeeds on attempt {self.succeed_on_attempt}."
            )


class ConformanceDeadLetterJobHandler:
    job_type = "conformance.dead_letter"

    async def handle(self, context: JobContext, payload: JobPayload) -> None:
        raise RuntimeError("conformance.dead_letter always fails.")


def conformance_handlers() -> list:
    return [ConformanceSucceedsJobHandler(), ConformanceRetriesJobHandler(), ConformanceDeadLetterJobHandler()]
