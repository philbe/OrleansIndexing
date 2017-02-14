using Microsoft.Azure.Documents;
using Orleans.Indexing;
using Orleans.Providers;
using Orleans.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Orleans.Benchmarks.Indexing.Scenario04
{

    // ------------------------------------------------------------------------
    // --- Baseline (persisted) Player Grain without index --------------------
    // ------------------------------------------------------------------------

    #region Baseline (persisted) Player Grain without index
    [Serializable]
    public class PlayerGrainState
    {
        public string Email { get; set; }

        public int Score { get; set; }

        public string Location { get; set; }
    }

    [StorageProvider(ProviderName = "BenchmarkStore")]
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
            // try... catch because sometimes AzureTable chokes on etag violations
            // returning false will cause the client to re-issue the update
            try
            {
                await base.WriteStateAsync();
                return true;
            }
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
    // --- Abstract Indexed Player Grain that implements common methods -------
    // ------------------------------------------------------------------------

    #region Abstract Player Grain that implements common methods
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
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
            catch (DocumentClientException de)
            {
                if ((int)de.StatusCode == 429 || (int)de.StatusCode == 449)
                {
                    await Task.Delay(de.RetryAfter);
                }
                await base.ReadStateAsync();
                return false;
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
    // --- (persisted) Player Grain with 1 A-index ------------------------------
    // ------------------------------------------------------------------------

    #region (persisted) Player Grain with 1 A-index

    [Serializable]
    public class IndexedPlayerAIndexGrainState : IndexedPlayerAIndexProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "BenchmarkStore")]
    public class IndexedPlayerAIndexGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerAIndexGrainState, IndexedPlayerAIndexProperties>, IIndexedPlayerAIndexGrain
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- (persisted) Player Grain with 1 I-index ----------------------------
    // ------------------------------------------------------------------------

    #region (persisted) Player Grain with 1 I-index

    [Serializable]
    public class IndexedPlayerIIndexGrainState : IndexedPlayerIIndexProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "BenchmarkStore")]
    public class IndexedPlayerIIndexGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerIIndexGrainState, IndexedPlayerIIndexProperties>, IIndexedPlayerIIndexGrain
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- (persisted) Player Grain with 1 DSM-index ----------------------------
    // ------------------------------------------------------------------------

    #region (persisted) Player Grain with 1 DSM-index

    [Serializable]
    public class IndexedPlayerDSMIndexGrainState : IndexedPlayerDSMIndexProperties, PlayerState
    {
        public string Email { get; set; }
    }

    [StorageProvider(ProviderName = "DocumentDBStore")]
    public class IndexedPlayerDSMIndexGrain : AbstractIndexedPlayerGrainNonFaultTolerant<IndexedPlayerDSMIndexGrainState, IndexedPlayerDSMIndexProperties>, IIndexedPlayerDSMIndexGrain
    {
    }
    #endregion

}
