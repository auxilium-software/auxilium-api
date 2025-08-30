import argparse
import sys
import time

from dotenv import load_dotenv
from fastapi import FastAPI, Request
from fastapi.middleware.cors import CORSMiddleware  # Use FastAPI's built-in CORS
from fastapi.middleware.gzip import GZipMiddleware
from fastapi.middleware.trustedhost import TrustedHostMiddleware
from fastapi.responses import JSONResponse

from common.utilities.configuration import Configuration, load_configuration, get_configuration
from common.databases.couchdb_interactions import get_couchdb_connection
from common.logging_helpers import LOGGER

from routers.authentication_router import router as authentication_router

parser=argparse.ArgumentParser()
parser.add_argument("--config", help="The location of the config file")
args=parser.parse_args()
load_configuration(args.config)
CONFIGURATION = get_configuration()

def create_app() -> FastAPI:
    app = FastAPI(
        title="Auxilium API",
        description="The core data IO for Auxilium 3",
        version="3.0.0-alpha",
        swagger_ui_parameters={
            "syntaxHighlight": {
                "theme": "obsidian",
            },
            "defaultModelsExpandDepth": 2,
            "defaultModelExpandDepth": 2,
            "displayRequestDuration": True,
        },
        contact={
            "name": CONFIGURATION.get_string('Instance', 'Contacts', 'Maintainer', 'Name'),
            "email": CONFIGURATION.get_string('Instance', 'Contacts', 'Maintainer', 'EmailAddress'),
        },
        license_info={
            "name": "Apache 2.0",
            "identifier": "Apache-2.0",
        },
    )

    app.add_middleware(
        CORSMiddleware,
        allow_origins=CONFIGURATION.get_object('API', 'AllowedOrigins'),
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
        ],
        expose_headers=[
            "X-Process-Time",
            "X-Total-Count",
            "X-Page-Count",
        ]
    )

    app.add_middleware(
        TrustedHostMiddleware,
        allowed_hosts=CONFIGURATION.get_object('API', 'AllowedHosts'),
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


app = create_app()

if __name__ == "__main__":
    import uvicorn

    uvicorn.run(
        "main:app",
        host=CONFIGURATION.get_string('API', 'Host'),
        port=CONFIGURATION.get_int('API', 'Port'),
        reload=True,
        # log_level="info",
        log_level="debug",
        access_log=True,
    )
