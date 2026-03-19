
from fastapi import FastAPI
from contextlib import asynccontextmanager
import structlog

logger = structlog.get_logger()

@asynccontextmanager
async def lifespan(app: FastAPI):
    logger.info("Starting up Knowledge RAG Service...")
    yield
    logger.info("Shutting down...")

app = FastAPI(title="Bazan Knowledge RAG", lifespan=lifespan)

@app.get("/health")
async def health():
    return {"status": "ok"}
