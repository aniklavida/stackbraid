"""Combines every Identity route group into one router `host/main.py`
mounts — the Python equivalent of .NET's ``MapIdentityEndpoints``.
"""

from __future__ import annotations

from fastapi import APIRouter

from app.features.identity.endpoints.auth import router as auth_router
from app.features.identity.endpoints.roles import router as roles_router
from app.features.identity.endpoints.users import router as users_router

router = APIRouter()
router.include_router(auth_router)
router.include_router(users_router)
router.include_router(roles_router)
