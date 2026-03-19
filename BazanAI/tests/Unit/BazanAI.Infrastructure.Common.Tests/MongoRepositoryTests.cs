
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using BazanAI.Infrastructure.Common.Persistence;
using BazanAI.SharedKernel.Domain;
using FluentAssertions;
using MongoDB.Driver;
using NSubstitute;
using Xunit;

namespace BazanAI.Infrastructure.Common.Tests.Persistence;

public class MongoRepositoryTests
{
    private readonly IMongoDatabase _database;
    private readonly IMongoCollection<TestEntity> _collection;
    private readonly TestMongoRepository _repository;
    private const string CollectionName = "TestCollection";

    public MongoRepositoryTests()
    {
        _database = Substitute.For<IMongoDatabase>();
        _collection = Substitute.For<IMongoCollection<TestEntity>>();
        _database.GetCollection<TestEntity>(CollectionName).Returns(_collection);

        _repository = new TestMongoRepository(_database);
    }

    [Fact]
    public async Task AddAsync_ShouldInsertEntity()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };

        // Act
        await _repository.AddAsync(entity);

        // Assert
        await _collection.Received(1).InsertOneAsync(entity, Arg.Any<InsertOneOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateEntityAndSetUpdatedAt()
    {
        // Arrange
        var entity = new TestEntity { Name = "Test" };
        var originalUpdatedAt = entity.UpdatedAt;

        // Act
        await _repository.UpdateAsync(entity);

        // Assert
        entity.UpdatedAt.Should().NotBeNull();
        entity.UpdatedAt.Should().BeAfter(DateTimeOffset.MinValue);
        await _collection.Received(1).ReplaceOneAsync(Arg.Any<FilterDefinition<TestEntity>>(), entity, Arg.Any<ReplaceOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteEntity()
    {
        // Arrange
        var id = Guid.NewGuid().ToString();

        // Act
        await _repository.DeleteAsync(id);

        // Assert
        await _collection.Received(1).DeleteOneAsync(Arg.Any<FilterDefinition<TestEntity>>(), Arg.Any<CancellationToken>());
    }

    public class TestEntity : Entity
    {
        public string Name { get; set; } = string.Empty;
    }

    public class TestMongoRepository : MongoRepository<TestEntity>
    {
        public TestMongoRepository(IMongoDatabase database) 
            : base(database, CollectionName)
        {
        }
    }
}
