using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;
using System;

namespace Orleans.Benchmarks.Indexing.Scenario02
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
    // --- Player Grain Interface with 1 default index,  ----------------------
    // --- i.e., a single index grain -----------------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 default index
    [Serializable]
    public class IndexedPlayerProperties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerDefaultGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerProperties>
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 per-silo index,  ---------------------
    // --- i.e., one local hash index per silo --------------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 per-silo index
    [Serializable]
    public class IndexedPlayerPerSiloProperties
    {
        public int Score { get; set; }

        [Index(typeof(AHashIndexPartitionedPerSilo<string, IIndexedPlayerPerSiloGrain>))]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerPerSiloGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerPerSiloProperties>
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 1 per-key index,  ----------------------
    // --- i.e., one index bucket grain per key hash --------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 per-key index
    [Serializable]
    public class IndexedPlayerPerKeyProperties
    {
        public int Score { get; set; }

        [Index(typeof(AHashIndexPartitionedPerKey<string, IIndexedPlayerPerKeyGrain>))]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerPerKeyGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerPerKeyProperties>
    {
    }
    #endregion
}
