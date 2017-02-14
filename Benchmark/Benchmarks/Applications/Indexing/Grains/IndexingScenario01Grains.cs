using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orleans.Providers;
using Orleans.Indexing;
using Orleans.Runtime;

namespace Orleans.Benchmarks.Indexing.Scenario01
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
    // --- Abstract Indexable Player Grain that implements common methods -----
    // ------------------------------------------------------------------------

    #region Abstract Indexable Player Grain that implements common methods
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
    // --- Player Grain with 1 indexed field ----------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain with 1 indexed field
    [Serializable]
    public class IndexedPlayer1GrainState : IndexedPlayer1Properties, PlayerState
    {
        public string Email { get; set; }
    }

    /// <summary>
    /// A simple grain that represent a player in a game
    /// </summary>
    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayer1Grain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayer1GrainState, IndexedPlayer1Properties>, IIndexedPlayer1Grain
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain with 2 indexed fields ---------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain with 2 indexed fields
    [Serializable]
    public class IndexedPlayer2GrainState : IndexedPlayer2Properties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayer2Grain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayer2GrainState, IndexedPlayer2Properties>, IIndexedPlayer2Grain
    {


        public async Task<bool> SetTwoLocations(string l1, string l2)
        {
            State.Location = l1;
            State.Location2 = l2;

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
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain with 3 indexed fields ---------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain with 3 indexed fields
    [Serializable]
    public class IndexedPlayer3GrainState : IndexedPlayer3Properties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayer3Grain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayer3GrainState, IndexedPlayer3Properties>, IIndexedPlayer3Grain
    {
        private Logger logger;


        public async Task<bool> SetThreeLocations(string l1, string l2, string l3)
        {
            State.Location = l1;
            State.Location2 = l2;
            State.Location3 = l3;

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
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain with 4 indexed fields ---------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain with 4 indexed fields
    [Serializable]
    public class IndexedPlayer4GrainState : IndexedPlayer4Properties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "MemoryStore")]
    public class IndexedPlayer4Grain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayer4GrainState, IndexedPlayer4Properties>, IIndexedPlayer4Grain
    {
        public async Task<bool> SetFourLocations(string l1, string l2, string l3, string l4)
        {
            State.Location = l1;
            State.Location2 = l2;
            State.Location3 = l3;
            State.Location4 = l4;

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
    }
    #endregion
}
