
FROM python:3.12-slim

WORKDIR /app

RUN useradd -m -u 1000 appuser

COPY src/Python/bazan-knowledge-rag/requirements.txt .
RUN pip install --no-cache-dir -r requirements.txt

COPY src/Python/bazan-knowledge-rag/ .

USER appuser

CMD ["uvicorn", "app.main:app", "--host", "0.0.0.0", "--port", "8000"]
