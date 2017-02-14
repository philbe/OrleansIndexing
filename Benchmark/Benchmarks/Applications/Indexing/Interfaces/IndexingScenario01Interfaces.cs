using System.Threading.Tasks;
using Orleans;
using Orleans.Indexing;
using System;

namespace Orleans.Benchmarks.Indexing.Scenario01
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
    // --- Player Grain Interface with 1 indexed field ------------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 1 indexed field
    [Serializable]
    public class IndexedPlayer1Properties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }
    }

    public interface IIndexedPlayer1Grain : IPlayerGrain, IIndexableGrain<IndexedPlayer1Properties>
    {
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 2 indexed fields -----------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 2 indexed fields
    [Serializable]
    public class IndexedPlayer2Properties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }

        [Index]
        public string Location2 { get; set; }

    }

    public interface IIndexedPlayer2Grain : IPlayerGrain, IIndexableGrain<IndexedPlayer2Properties>
    {
        Task<bool> SetTwoLocations(string l1, string l2);
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 3 indexed fields -----------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 3 indexed fields
    [Serializable]
    public class IndexedPlayer3Properties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }

        [Index]
        public string Location2 { get; set; }

        [Index]
        public string Location3 { get; set; }

    }

    public interface IIndexedPlayer3Grain : IPlayerGrain, IIndexableGrain<IndexedPlayer3Properties>
    {
        Task<bool> SetThreeLocations(string l1, string l2, string l3);
    }
    #endregion

    // ------------------------------------------------------------------------
    // --- Player Grain Interface with 4 indexed fields -----------------------
    // ------------------------------------------------------------------------

    #region Player Grain Interface with 4 indexed fields
    [Serializable]
    public class IndexedPlayer4Properties
    {
        public int Score { get; set; }

        [Index]
        public string Location { get; set; }

        [Index]
        public string Location2 { get; set; }

        [Index]
        public string Location3 { get; set; }

        [Index]
        public string Location4 { get; set; }
    }

    public interface IIndexedPlayer4Grain : IPlayerGrain, IIndexableGrain<IndexedPlayer4Properties>
    {
        Task<bool> SetFourLocations(string l1, string l2, string l3, string l4);
    }
    #endregion
}
