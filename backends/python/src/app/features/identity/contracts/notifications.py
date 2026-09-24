from __future__ import annotations

from pydantic import BaseModel, ConfigDict, Field


class RegisterDeviceTokenRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)
    token: str = Field(min_length=1, max_length=4096)
    platform: str = Field(pattern="^(ios|android|web)$")
    app_version: str | None = Field(default=None, max_length=50, alias="appVersion")


class RefreshDeviceTokenRequest(BaseModel):
    token: str = Field(min_length=1, max_length=4096)
