namespace Reviews.Infrastructure.Persistence;

using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

public static class MongoMappings
{
    public static void Register()
    {
        ConventionRegistry.Register("camelCase", new ConventionPack
        {
            new CamelCaseElementNameConvention(),
            new IgnoreExtraElementsConvention(true)
        }, _ => true);

        BsonClassMap.RegisterClassMap<Review>(cm =>
        {
            cm.AutoMap();
            cm.MapField("_comments").SetElementName("comments");
        });

        BsonClassMap.RegisterClassMap<Author>(cm =>
        {
            cm.MapProperty(a => a.UserId);
            cm.MapProperty(a => a.DisplayName);
            cm.MapCreator(a => new Author(a.UserId, a.DisplayName));
        });
    }
}