using Orleans.Indexing;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Indexing.Scenario02
{

    // ------------------------------------------------------------------------
    // --- Baseline Player Grain without index --------------------------------
    // ------------------------------------------------------------------------

    #region Baseline Player Grain without index
    [Serializable]
    public class PlayerGrainState
    {
        public string Email { get; set; }

        public int Score { get; set; }

        public string Location { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class PlayerGrain : Grain<PlayerGrainState>, IPlayerGrain
    {
        private Logger logger;

        public string Email { get { return State.Email; } }
        public string Location { get { return State.Location; } }
        public int Score { get { return State.Score; } }

        public override Task OnActivateAsync()
        {
            logger = GetLogger("PlayerGrain-" + IdentityString);
            return base.OnActivateAsync();
        }

        public Task<string> GetLocation()
        {
            return Task.FromResult(Location);
        }

        public async Task<bool> SetLocation(string location)
        {
            State.Location = location;
            //return TaskDone.Done;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }

        public Task<int> GetScore()
        {
            return Task.FromResult(Score);
        }

        public async Task<bool> SetScore(int score)
        {
            State.Score = score;
            //return TaskDone.Done;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }

        public Task<string> GetEmail()
        {
            return Task.FromResult(Email);
        }

        public async Task<bool> SetEmail(string email)
        {
            State.Email = email;
            //return TaskDone.Done;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }
        public Task LogSilo(string mode)
        {
            Logger log = GetLogger();
            log.TrackTrace("IndexBenchmark: PlayerGrain: mode = " + mode + "; silo = " + base.RuntimeIdentity, Severity.Info);

            return TaskDone.Done;
        }

    }

    #endregion

    // ------------------------------------------------------------------------
    // --- Abstract Player Grain that implements common methods ---------------
    // ------------------------------------------------------------------------

    #region Abstract Player Grain that implements common methods
    [StorageProvider(ProviderName = "MemoryStore")]
    public abstract class AbstractIndexedPlayerGrainNonFaultTolerant<TState, TProps> : IndexableGrainNonFaultTolerant<TState, TProps>, IPlayerGrain where TState : PlayerState where TProps : new()
    {
        private Logger logger;

        public string Email { get { return State.Email; } }
        public string Location { get { return State.Location; } }
        public int Score { get { return State.Score; } }

        public override Task OnActivateAsync()
        {
            logger = GetLogger("PlayerGrain-" + IdentityString);
            return base.OnActivateAsync();
        }

        public Task<string> GetLocation()
        {
            return Task.FromResult(Location);
        }

        public async Task<bool> SetLocation(string location)
        {
            State.Location = location;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }

        public Task<int> GetScore()
        {
            return Task.FromResult(Score);
        }

        public async Task<bool> SetScore(int score)
        {
            State.Score = score;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }

        public Task<string> GetEmail()
        {
            return Task.FromResult(Email);
        }

        public async Task<bool> SetEmail(string email)
        {
            State.Email = email;

            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (Exception)
            {
                await base.ReadStateAsync();
                return false;
            }
        }
        public Task LogSilo(string mode)
        {
            Logger log = GetLogger();
            log.TrackTrace("IndexBenchmark: PlayerGrain: mode = " + mode + "; silo = " + base.RuntimeIdentity, Severity.Info);

            return TaskDone.Done;
        }

    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain with 1 default index,  --------------------------------
    // --- i.e., a single index grain -----------------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain with 1 default index

    [Serializable]
    public class IndexedPlayerDefaultGrainState : IndexedPlayerProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayerDefaultGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerDefaultGrainState, IndexedPlayerProperties>, IIndexedPlayerDefaultGrain
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 per-silo index,  ---------------------
    // --- i.e., one local hash index per silo --------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 per-silo index
    [Serializable]
    public class IndexedPlayerPerSiloGrainState : IndexedPlayerPerSiloProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayerPerSiloGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerPerSiloGrainState, IndexedPlayerPerSiloProperties>, IIndexedPlayerPerSiloGrain
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 per-key index,  ----------------------
    // --- i.e., one index bucket grain per key hash --------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 per-key index
    [Serializable]
    public class IndexedPlayerPerKeyGrainState : IndexedPlayerPerKeyProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayerPerKeyGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerPerKeyGrainState, IndexedPlayerPerKeyProperties>, IIndexedPlayerPerKeyGrain
    {
    }
    #endregion
}
