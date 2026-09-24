"""The status query a job's owner can call, plus the optional conformance
surface. The auth dependency is passed in by the composition root rather than
imported here, so ``shared`` never reaches into a feature's internals.
"""

from __future__ import annotations

from typing import Callable
from uuid import UUID

from fastapi import APIRouter, Depends, Request
from fastapi.responses import JSONResponse
from pydantic import BaseModel

from app.shared.jobs.models import JobRequest
from app.shared.web.errors import AppError
from app.shared.web.problem import problem_response

_SCENARIOS: dict[str, tuple[str, int]] = {
    "succeeds": ("conformance.succeeds", 3),
    "retries": ("conformance.retries", 3),
    "dead-letter": ("conformance.dead_letter", 2),
}


class ConformanceScenarioRequest(BaseModel):
    scenario: str


def create_jobs_router(current_user_id: Callable, *, conformance_enabled: bool) -> APIRouter:
    router = APIRouter(prefix="/v1/jobs", tags=["Jobs"])

    @router.get("/{job_id}")
    async def get_job_status(job_id: UUID, request: Request, user_id: UUID = Depends(current_user_id)):
        status = await request.app.state.job_scheduler.status(job_id)
        if status is None or status.owner_id != str(user_id):
            error = AppError.not_found("JOBS.NOT_FOUND", "jobs.not_found")
            return problem_response(error, request.app.state.localizer, request)
        return JSONResponse(content=status.to_response())

    if conformance_enabled:

        @router.post("/scenarios", status_code=202)
        async def enqueue_scenario(body: ConformanceScenarioRequest, request: Request, user_id: UUID = Depends(current_user_id)):
            if body.scenario not in _SCENARIOS:
                error = AppError.validation("JOBS.UNKNOWN_SCENARIO", "jobs.unknown_scenario", {})
                return problem_response(error, request.app.state.localizer, request)
            job_type, max_attempts = _SCENARIOS[body.scenario]
            job_id = await request.app.state.job_scheduler.enqueue_request(
                JobRequest(job_type, "{}", str(user_id), max_attempts)
            )
            return JSONResponse(status_code=202, content={"jobId": str(job_id)})

    return router
