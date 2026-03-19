
up:
	docker compose -f infra/docker-compose.yml up -d

down:
	docker compose -f infra/docker-compose.yml down

test:
	dotnet test
	pytest src/Python/bazan-knowledge-rag

migrate:
	docker compose -f infra/docker-compose.yml exec mongo1 mongosh /mongo-init/init-replica.js

logs:
	docker compose -f infra/docker-compose.yml logs -f

build:
	docker compose -f infra/docker-compose.yml build --no-cache
