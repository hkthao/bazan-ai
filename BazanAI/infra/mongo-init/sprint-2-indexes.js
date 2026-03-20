// bazan_identity.farmers
// Chú ý: Cần đồng bộ PascalCase/camelCase với Domain model và MongoDB driver configuration.
// Theo ADR-001 và yêu cầu issue #34, chúng ta sử dụng camelCase cho fields trong MongoDB.

db.farmers.createIndex(
  { "identities.provider": 1, "identities.providerUserId": 1 },
  { unique: true, sparse: true, name: "idx_identity_provider_unique" }
);
db.farmers.createIndex({ "location.coordinates": "2dsphere" }, { name: "idx_location_geo" });
db.farmers.createIndex({ "createdAt": 1 }, { name: "idx_created_at" });

// bazan_identity.farms
db.farms.createIndex({ "farmerId": 1 }, { name: "idx_farmer_id" });
db.farms.createIndex({ "location.coordinates": "2dsphere" }, { name: "idx_farm_geo" });

// bazan_identity.user_contexts
db.user_contexts.createIndex({ "farmerId": 1 }, { unique: true, name: "idx_farmer_id_unique" });

// bazan_identity.refresh_tokens
db.refresh_tokens.createIndex({ "hashedToken": 1 }, { name: "idx_hashed_token" });
db.refresh_tokens.createIndex({ "farmerId": 1, "isRevoked": 1 }, { name: "idx_farmer_active" });
db.refresh_tokens.createIndex({ "expiresAt": 1 }, { expireAfterSeconds: 0, name: "idx_ttl_expire" });
