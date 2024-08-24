using MongoDB.Bson;
using MongoDB.Driver;

namespace DonkeyDog.Models
{
    public class ServiziService
    {
        private readonly IMongoCollection<Servizi> _serviziCollection;

        public ServiziService(IMongoDatabase database)
        {
            var collectionSettings = new MongoCollectionSettings { GuidRepresentation = GuidRepresentation.Standard };
            _serviziCollection = database.GetCollection<Servizi>("Servizi", collectionSettings);
        }

        public async Task<Servizi> GetByIdAsync(Guid id)
        {
            var filter = Builders<Servizi>.Filter.Eq(s => s.Id, id);
            return await _serviziCollection.Find(filter).FirstOrDefaultAsync();
        }

        public async Task CreateAsync(Servizi servizio)
        {
            servizio.Id = Guid.NewGuid(); // Genera un nuovo GUID per il documento
            await _serviziCollection.InsertOneAsync(servizio);
        }

        public async Task UpdateAsync(Guid id, Servizi servizio)
        {
            var filter = Builders<Servizi>.Filter.Eq(s => s.Id, id);
            await _serviziCollection.ReplaceOneAsync(filter, servizio);
        }

        public async Task DeleteAsync(Guid id)
        {
            var filter = Builders<Servizi>.Filter.Eq(s => s.Id, id);
            await _serviziCollection.DeleteOneAsync(filter);
        }

        public async Task<List<Servizi>> GetAllAsync()
        {
            return await _serviziCollection.Find(_ => true).ToListAsync();
        }
    }
}
