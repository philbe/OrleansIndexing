using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;
using System;

namespace Orleans.Benchmarks.Indexing.Scenario04
{

    // ------------------------------------------------------------------------
    // --- General Player Grain Interface -------------------------------------
    // ------------------------------------------------------------------------

    #region General Player Grain Interface
    public interface PlayerProperties
    {
        int Score { get; set; }

        string Location { get; set; }
    }

    public interface PlayerState : PlayerProperties
    {
        string Email { get; set; }
    }

    public interface IPlayerGrain : IGrainWithIntegerKey
    {
        Task<string> GetEmail();
        Task<string> GetLocation();
        Task<int> GetScore();

        Task<bool> SetEmail(string email);
        Task<bool> SetLocation(string location);
        Task<bool> SetScore(int score);

        Task LogSilo(string mode);
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 A-index ------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 A-index
    [Serializable]
    public class IndexedPlayerAIndexProperties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerAIndexGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerAIndexProperties>
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 I-index ------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 I-index
    [Serializable]
    public class IndexedPlayerIIndexProperties
    {
        public int Score { get; set; }

        [Index(typeof(IHashIndexSingleBucket<string, IIndexedPlayerIIndexGrain>))]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerIIndexGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerIIndexProperties>
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 DSM-index ------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 DSM-index
    [Serializable]
    public class IndexedPlayerDSMIndexProperties
    {
        public int Score { get; set; }

        [DSMIndex]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerDSMIndexGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerDSMIndexProperties>
    {
    }
    #endregion
}
