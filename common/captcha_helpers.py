
import os

from fastapi import HTTPException, status
from typing import Optional
import httpx

async def _verify_recaptcha(token: str, remote_ip: Optional[str] = None) -> dict:
    payload = {
        "secret": os.getenv("RECAPTCHA_SECRET_KEY"),
        "response": token
    }
    if remote_ip:
        payload["remoteip"] = remote_ip

    try:
        async with httpx.AsyncClient() as client:
            response = await client.post(
                "https://www.google.com/recaptcha/api/siteverify",
                data=payload,
                timeout=10.0
            )
            response.raise_for_status()
            result = response.json()

            if not result.get("success", False):
                error_codes = result.get("error-codes", [])
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST,
                    detail="reCAPTCHA verification failed"
                )

            score = result.get("score", 0)
            if score < float(os.getenv("RECAPTCHA_SCORE_THRESHOLD")):
                raise HTTPException(
                    status_code=status.HTTP_400_BAD_REQUEST,
                    detail="Suspicious activity detected. Please try again."
                )

            return result

    except httpx.RequestError as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="Unable to verify reCAPTCHA"
        )
    except httpx.HTTPStatusError as e:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail="reCAPTCHA verification service error"
        )

