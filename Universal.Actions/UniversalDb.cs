using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Universal.Actions.Models;
using Model = Universal.Actions.Models;

namespace Universal.Actions
{
    public class UniversalDb : IDisposable
    {
        private string _endpointUri;
        private string _primaryKey;

        // Cosmos client 
        private CosmosClient _cosmosClient;
        private Lazy<Database> _database;
        private Lazy<Container> _container;

        private readonly string DatabaseId = "UniversalUsers";
        private readonly string ContainerId = "Approvals";
        private readonly string PartitionKeyValue = "AllUsers";

        public UniversalDb(IConfiguration configuration)
        {
            Configuration = configuration;

            this._endpointUri = Configuration.GetSection("CosmosEndpointUri")?.Value;
            this._primaryKey = Configuration.GetSection("CosmosKey")?.Value;

            this._cosmosClient = new CosmosClient(_endpointUri, _primaryKey);
            this._database = new Lazy<Database>(() =>
            {
                var task = this._cosmosClient.CreateDatabaseIfNotExistsAsync(DatabaseId);
                task.Wait();
                return task.Result;
            }, isThreadSafe: true);

            this._container = new Lazy<Container>(() =>
            {
                var task = this._database.Value.CreateContainerIfNotExistsAsync(ContainerId, "/partitionKey");
                task.Wait();
                return task.Result;
            }, isThreadSafe: true);
        }

        public IConfiguration Configuration { get; set; }

        public async Task<Asset> GetAssetAsync(string assetId)
        {
            try
            {
                var asset = await _container.Value.ReadItemAsync<Asset>(assetId, new PartitionKey(PartitionKeyValue));

                return asset;
            }
            catch (CosmosException ex)
            {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    return null;
                }
                else
                {
                    throw;
                }
            }

            
        }

        public async Task<Asset> UpsertAssetAsync(Asset asset)
        {

            asset.PartitionKey = PartitionKeyValue;

            ItemResponse<Asset> response = await _container.Value.UpsertItemAsync(asset, new PartitionKey(PartitionKeyValue));
            return response.Resource;
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _cosmosClient.Dispose();
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~CateringDb()
        // {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
