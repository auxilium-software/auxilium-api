
import time

from dotenv import load_dotenv
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware  # Use FastAPI's built-in CORS
from fastapi.middleware.gzip import GZipMiddleware
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from fastapi.openapi.docs import get_swagger_ui_html
from fastapi.responses import RedirectResponse, JSONResponse
from starlette.exceptions import HTTPException as StarletteHTTPException

from common.couchdb_interactions import build_couchdb_database, get_couchdb_connection
from common.logging_helpers import LOGGER

from routers.authentication_router import router as authentication_router

# Configure logging

load_dotenv()


def create_app() -> FastAPI:
    """Application factory pattern for better testability"""

    app = FastAPI(
        title="Auxilium API",
        description="The core data IO for Auxilium 3",
        version="3.0.0-alpha",
        docs_url=None,
        redoc_url=None,
        swagger_ui_parameters={
            "syntaxHighlight": {
                "theme": "obsidian",
            },
            "defaultModelsExpandDepth": 2,
            "defaultModelExpandDepth": 2,
            "displayRequestDuration": True,
        },
    )

    # Add CORS middleware FIRST (before other middleware)
    app.add_middleware(
        CORSMiddleware,
        allow_origins=[
            "https://go.fflyd.com",
            "http://localhost",
            "http://127.0.0.1",
            "http://localhost:1234",
            "http://127.0.0.1:1234",
            "http://localhost:1938",
            "http://127.0.0.1:1938",
        ],
        allow_credentials=True,
        allow_methods=["GET", "POST", "PUT", "DELETE", "OPTIONS", "PATCH"],
        allow_headers=[
            "Authorization",
            "Content-Type",
            "Accept",
            "Origin",
            "User-Agent",
            "DNT",
            "Cache-Control",
            "X-Mx-ReqToken",
            "Keep-Alive",
            "X-Requested-With",
            "X-CSRF-Token",
            "X-API-Key",
            "X-Tenant-ID"
        ],
        expose_headers=[
            "X-Process-Time",
            "X-Total-Count",
            "X-Page-Count",
        ]
    )

    app.add_middleware(
        TrustedHostMiddleware,
        allowed_hosts=[
            "localhost",
            "127.0.0.1",
            "localhost:1234",
            "127.0.0.1:1234",
            "localhost:1938",
            "127.0.0.1:1938",
            "api.fflyd.com",
            "go.fflyd.com",
        ]
    )

    app.add_middleware(GZipMiddleware, minimum_size=1000)

    @app.middleware("http")
    async def add_process_time_header(request: Request, call_next):
        start_time = time.time()

        if request.method == "OPTIONS":
            response = JSONResponse(content={}, status_code=200)
            response.headers["Access-Control-Allow-Origin"] = request.headers.get("origin", "*")
            response.headers["Access-Control-Allow-Methods"] = "GET, POST, PUT, DELETE, OPTIONS, PATCH"
            response.headers["Access-Control-Allow-Headers"] = "Authorization, Content-Type, Accept, Origin, User-Agent, DNT, Cache-Control, X-Mx-ReqToken, Keep-Alive, X-Requested-With, X-CSRF-Token, X-API-Key, X-Tenant-ID"
            response.headers["Access-Control-Allow-Credentials"] = "true"
            return response

        response = await call_next(request)
        process_time = time.time() - start_time
        response.headers["X-Process-Time"] = str(process_time)

        if process_time > 1.0:
            LOGGER.warning(f"Slow request: {request.method} {request.url} took {process_time:.2f}s")

        return response


    all_routers = [
        (authentication_router,     '/api/v3/authentication'),
    ]

    for router, description in all_routers:
        app.include_router(router)
        LOGGER.info(f"Registered router: {description}")

    return app


# Create the app instance
app = create_app()

if __name__ == "__main__":
    couchdb = get_couchdb_connection()
    build_couchdb_database()

    import uvicorn

    uvicorn.run(
        "main:app",
        host="0.0.0.0",
        port=1938,
        reload=True,
        # log_level="info",
        log_level="debug",
        access_log=True,
    )
