using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using ExcelBotCs.Data;
using ExcelBotCs.Models.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;

namespace ExcelBotCs.Services;

public class RsaKeyService
{
    private readonly IMongoCollection<RsaKeyDocument> _collection;

    public RsaKeyService(IOptions<DatabaseOptions> databaseConfig)
    {
        var client = new MongoClient(databaseConfig.Value.ConnectionString);
        var db = client.GetDatabase(databaseConfig.Value.DatabaseName);
        _collection = db.GetCollection<RsaKeyDocument>("SecurityKeys");
    }

    public string GenerateJwt(JwtOptions jwtOptions, List<Claim> claims)
    {
        var tokenHandler = new JwtSecurityTokenHandler();

        using var rsa = GetPrivateRsa();
        var signingCredentials = new SigningCredentials(
            new RsaSecurityKey(rsa),
            SecurityAlgorithms.RsaSha256
        );

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = jwtOptions.Issuer,
            Audience = jwtOptions.Audience,
            SigningCredentials = signingCredentials
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    public void EnsureRsaKeysPresent(JwtOptions jwtOptions, string contentRoot)
    {
        // Ensure an active RSA keypair is present in Mongo. Ignore filesystem locations.
        var existing = _collection.Find(x => x.Type == "RSA" && x.Active).FirstOrDefault();
        if (existing != null) return;

        using var rsa = RSA.Create(2048);
        var privatePem = ExportPrivateKeyPem(rsa);
        var publicPem = ExportPublicKeyPem(rsa);

        var doc = new RsaKeyDocument
        {
            Id = ObjectId.GenerateNewId(),
            Type = "RSA",
            Active = true,
            PrivatePem = privatePem,
            PublicPem = publicPem,
            CreatedAt = DateTime.UtcNow
        };

        // Try insert; if another replica raced and inserted, this will simply add another doc.
        // As a simple guard, if multiple active docs exist, first is used by getters below.
        _collection.InsertOne(doc);
    }

    public RSA GetPublicRsa()
    {
        var doc = _collection.Find(x => x.Type == "RSA" && x.Active)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (doc == null || string.IsNullOrWhiteSpace(doc.PublicPem))
            throw new InvalidOperationException("RSA public key not found in database.");
        var rsa = RSA.Create();
        rsa.ImportFromPem(doc.PublicPem.ToCharArray());
        return rsa;
    }

    public RSA GetPrivateRsa()
    {
        var doc = _collection.Find(x => x.Type == "RSA" && x.Active)
            .SortByDescending(x => x.CreatedAt)
            .FirstOrDefault();
        if (doc == null || string.IsNullOrWhiteSpace(doc.PrivatePem))
            throw new InvalidOperationException("RSA private key not found in database.");
        var rsa = RSA.Create();
        rsa.ImportFromPem(doc.PrivatePem.ToCharArray());
        return rsa;
    }

    private static string ExportPrivateKeyPem(RSA rsa)
    {
        var pkcs8 = rsa.ExportPkcs8PrivateKey();
        var pem = PemEncoding.Write("PRIVATE KEY", pkcs8);
        return new string(pem);
    }

    private static string ExportPublicKeyPem(RSA rsa)
    {
        var spki = rsa.ExportSubjectPublicKeyInfo();
        var pem = PemEncoding.Write("PUBLIC KEY", spki);
        return new string(pem);
    }

    private class RsaKeyDocument
    {
        [BsonId] public ObjectId Id { get; set; }
        public string Type { get; set; } = string.Empty; // e.g., "RSA"
        public bool Active { get; set; }
        public string PublicPem { get; set; } = string.Empty;
        public string PrivatePem { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}