using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;
using System;

namespace Orleans.Benchmarks.Indexing.Scenario03
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
    // --- Player Grain Interface with 1 default index ------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 default index
    [Serializable]
    public class IndexedPlayerProperties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IIndexedPlayerGrain : IPlayerGrain, IIndexableGrain<IndexedPlayerProperties>
    {
    }
    #endregion

    
}
