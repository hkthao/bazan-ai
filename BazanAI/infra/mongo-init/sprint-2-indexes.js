// bazan_identity.farmers
db.farmers.createIndex(
  { "Identities.Provider": 1, "Identities.ProviderUserId": 1 },
  { unique: true, sparse: true, name: "idx_identity_provider_unique" }
);
db.farmers.createIndex({ "location.coordinates": "2dsphere" }, { name: "idx_location_geo" });
db.farmers.createIndex({ "CreatedAt": 1 }, { name: "idx_created_at" });

// bazan_identity.farms
db.farms.createIndex({ "FarmerId": 1 }, { name: "idx_farmer_id" });
db.farms.createIndex({ "location.coordinates": "2dsphere" }, { name: "idx_farm_geo" });

// bazan_identity.user_contexts
db.user_contexts.createIndex({ "FarmerId": 1 }, { unique: true, name: "idx_farmer_id_unique" });

// bazan_identity.refresh_tokens
db.refresh_tokens.createIndex({ "HashedToken": 1 }, { name: "idx_hashed_token" });
db.refresh_tokens.createIndex({ "FarmerId": 1, "IsRevoked": 1 }, { name: "idx_farmer_active" });
db.refresh_tokens.createIndex({ "ExpiresAt": 1 }, { expireAfterSeconds: 0, name: "idx_ttl_expire" });
